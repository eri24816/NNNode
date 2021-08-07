'''
Websocket server for NNNode

API:
    /lobby/{env_name}  Lobby
        Usage:
            1. Connect
            Send commands to server :
                - stt <env name>
                    1. Wait for a respond message from server
        
    /env/{env_name}  Interact with an environment
        Usage:
            1. Connect
            2. Wait for a respond message from server
            3. 
            Send commands to server :
                - new : create a node or a flow
                - cod : modify code in a CodeNode
                - act : activate a node
                - mov : move a node
            Recieve messages form server : 
                



prifix:
    client -> server:
    - cod
    - exc

    server -> client
    - msg
    - wrn
    - err

'''



from asyncio.events import get_running_loop
from asyncio.queues import Queue
import websockets
import asyncio
import threading
import Environment
import json

envs=dict()

import websockets_routes

router = websockets_routes.Router()

message_sender_started = False

@router.route("/lobby") #* lobby
async def lobby(websocket, path):
    global message_sender_started
    if not message_sender_started:
        asyncio.create_task(direct_message_sender())
        asyncio.create_task(buffered_message_sender())
        message_sender_started = True
    async for message in websocket:
        command=message[:3]
        message=message[4:]
        if command == "stt": # start a env
            env_name=message
            if env_name in envs:
                await websocket.send("msg env %s is already running" % env_name)
                return
            else:
                await websocket.send("msg env %s has started" % env_name)
            new_env=Environment.Env(name=env_name)
            new_thread=threading.Thread(target=new_env.run,name=env_name)
            new_thread.setDaemon(True)
            new_env.thread=new_thread
            envs.update({env_name:new_env})
            new_thread.start()

            new_env.Add_direct_message = lambda content: messages_to_client.put_nowait((new_env.ws_clients, content))

# The main loop that handle an ws client connected to an env
@router.route("/env/{env_name}")
async def env_ws(websocket, path):
    env : Environment.Env = None
    env_name=path.params["env_name"]
    if env_name in envs:
        env=envs[env_name]
        await websocket.send("msg connected to env %s" % env_name)
    else:
        await websocket.send("err no such env %s" % env_name)
        websocket.close()
        return

    env.ws_clients.append(websocket)
    

    #TODO: client load entire env

    env.update_demo_nodes()

    async for message in websocket:
        m=json.loads(message) # message is in Json
        command=m['command']

        if command == 'upd':
            continue # TODO
        print('-- client:\t',m)
        if command == "new":
            '''
            create a new node or an new edge
                {command:"new",
                    info:{id,...}
                }
            '''
            env.Create(m['info'])
            await websocket.send("msg %s %s created" % (m['info']['type'],m['info']['id']))

        elif command == "rmv":
            '''
            remove a node or an edge
                {command:"rmv",
                    id
                }
            '''
            
            env.Remove({"id" : m['id']})
            await websocket.send("smsg %s removed" % m['id'])
            

        elif command == "udo":
            '''
            undo  
            {command:"udo",id}
            if id=='', apply undo on env
            '''
            id= m['id']
            if id=='':
                if env.Undo():
                    await websocket.send("msg env undone" )
                else:
                    await websocket.send("msg  noting to undo" )
            else:
                if id in env.nodes:
                    with Environment.History_lock(env.nodes[id]):
                        if env.nodes[id].Undo():
                            await websocket.send("msg node %s undone" % id)
                        else:
                            await websocket.send("msg node %s noting to undo" % id)
                else:
                    await websocket.send("err no such node %s" % id)
                
        elif command == "rdo":
            '''
            redo
            if id=='', apply redo on env
                {command:"rdo",id}
            '''
            id= m['id']
            if id=='':
                if env.Redo():
                    await websocket.send("msg env redone" )
                else:
                    await websocket.send("msg  noting to redo" )
            else:
                if id in env.nodes:
                    with Environment.History_lock(env.nodes[id]):
                        if env.nodes[id].Redo():
                            await websocket.send("msg node %s redone" % id)
                        else:
                            await websocket.send("msg node %s noting to redo" % id)
                else:
                    await websocket.send("err no such node %s" % id)

        elif command == "gid":
            '''
            give client an unused id
            '''
            await websocket.send(json.dumps({'command':"gid",'id':env.id_iter.next()}))
        #TODO
        elif command == "sav":
            '''
            save the graph to disk
            '''
       
        elif command == "lod":
            '''
            load the graph from disk
            '''

        else:
            '''
            Other commands are sent to the node directly
            '''
            id=m['id']
            if id in env.nodes:
                env.nodes[id].recive_command(m)

async def direct_message_sender():
    # put message in the queue to send them to client
    global messages_to_client
    messages_to_client = asyncio.Queue()
    while True:
        ws_list,message = await messages_to_client.get()
        for ws in ws_list:
            await ws.send(json.dumps(message))

async def buffered_message_sender():

    while(True):
        for env in envs.values():
            # Each env has a message buffer
            if env.running_node:
                env.running_node.flush_output()
            for key, value in sorted(env.message_buffer.copy().items()):
                command_, id = key[1:4], key[5:].split('/')[0]
                if command_ == "atr":
                    messages_to_client.put_nowait((env.ws_clients,{'command': command_, 'id': id, 'name':value,'value':env.nodes[id].attributes[value].value}))
                else:
                    messages_to_client.put_nowait((env.ws_clients,{'command': command_, 'id': id, 'info': value}))
                #print({'command': command_, 'id': id, 'value': value})
            env.message_buffer.clear()
        await asyncio.sleep(0.1)


start_server = websockets.serve(router, "localhost", 1000)

asyncio.get_event_loop().run_until_complete(start_server)
try:
    asyncio.get_event_loop().run_forever()
except KeyboardInterrupt:
    print('KeyboardInterrupt')

