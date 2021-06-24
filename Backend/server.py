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



import websockets
import asyncio
import threading
import Environment
import json

envs=dict()

import websockets_routes

router = websockets_routes.Router()



@router.route("/env/{env_name}") #* interact with an env
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

    env_history_client=env.latest_history # which env history is client on
    env_history_client_version = 0
    update_message_buffer = {}
    env.update_message_buffers.append(update_message_buffer) # register to env so the buffer will be updated

    #TODO: client load entire env

    env.update_demo_nodes()
    print(env.update_message_buffers[0])

    async for message in websocket:
        m=json.loads(message) # message is in Json
        command=m['command']

        if command != 'upd':
            print(m)
        if command == "new":
            '''
            create a new node or an edge
                {command:"new",
                    info:{id,...}
                }
            '''
            env.Create(m['info'])
            await websocket.send("msg %s %s created" % (m['info']['type'],m['info']['id']))

        elif command == "rmv":
            '''
            remove a new node or an edge
                {command:"rmv",
                    id
                }
            '''
            
            env.Remove({"id" : m['id']})
            await websocket.send("msg %s removed" % m['id'])
            
        elif command == "mov":
            '''
            move a node to pos
                {command:"mov",id,pos}
            '''
            
            env.Move(m['id'],m['pos'])
            await websocket.send("msg node %s moved to %s" % (m['id'],m['pos']))

        elif command == "upd":
            '''
            if there are changes after last upd command, send those changes to client
                {command:"upd",(int)max_steps}
            '''
            max_steps=int(m['max_steps'])if 'max_steps' in m else 100
            for _ in range(max_steps):# update env
                #TODO: if too far, reload entire env
                if env_history_client.head_direction==0:
                    if env_history_client_version == env_history_client.version:
                        break
                    else:
                        await Update_client(websocket,1,env_history_client)
                        env_history_client_version = env_history_client.version
                elif env_history_client.head_direction==1:
                    env_history_client=env_history_client.next # move forward
                    await Update_client(websocket,1,env_history_client)
                    env_history_client_version = env_history_client.version
                elif env_history_client.head_direction==-1:
                    await Update_client(websocket,-1,env_history_client)
                    env_history_client=env_history_client.last # move backward
                    env_history_client_version = env_history_client.version

            if env.running_node:
                env.running_node.flush_output()
            for key, value in sorted(update_message_buffer.copy().items()):
                command_, id = key[:3], key[4:]
                if command_ == "cod":
                    value=env.nodes[id].code
                await websocket.send(json.dumps({'command': command_, 'id': id, 'info': value}))
                #print({'command': command_, 'id': id, 'value': value})
            update_message_buffer.clear()

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


        
async def Update_client(ws,direction,history_item):
    '''
    for client that isn't on head,
    if direction == 1, do/redo the action in history_item
    if direction == -1, undo the action in history_item
    '''
    print('Update client: '+history_item.type+', '+str(history_item.content))
    # history_item can't be type "stt"
    if direction==1:
        if history_item.type=="mov":# move node to new position
            await ws.send(json.dumps({'command':"mov",'id':history_item.content['id'],'pos':history_item.content['new']}))
        if history_item.type=="new":# add node
            await ws.send(json.dumps({'command':"new",'info':history_item.content})) # info
        if history_item.type=="rmv":# remove node
            await ws.send(json.dumps({'command':"rmv",'id':history_item.content['id']}))
    else:
        if history_item.type=="mov":# move node back to old position
            await ws.send(json.dumps({'command':"mov",'id':history_item.content['id'],'pos':history_item.content['old']}))
        if history_item.type=="new":# remove node
            await ws.send(json.dumps({'command':"rmv",'id':history_item.content['id']}))
        if history_item.type=="rmv":# add node
            await ws.send(json.dumps({'command':"new",'info':history_item.content})) # info


        
@router.route("/lobby") #* lobby
async def lobby(websocket, path):

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
            new_env.thread=new_thread
            envs.update({env_name:new_env})
            new_thread.start()

start_server = websockets.serve(router, "localhost", 1000)
asyncio.get_event_loop().run_until_complete(start_server)
asyncio.get_event_loop().run_forever()

