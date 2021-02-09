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
                - cod "<node name>" <code> : Set code of a node
                    1. Wait for a respond message from server
                - exc <node name>: Run a node
                - mov : Move a node
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
    env=None
    env_name=path.params["env_name"]
    if env_name in envs:
        env=envs[env_name]
        await websocket.send("msg connected to env %s" % env_name)
    else:
        await websocket.send("err no such env %s" % env_name)
        websocket.close()
        return

    node_history_client={} # which node history is client on
    env_history_client=env.latest_history # which env history is client on
    env_history_client_version=0

    #TODO: client load entire env

    async for message in websocket:
        m=json.loads(message) # message is in Json
        if m['command']!='upd':
            print(m)
        if m['command'] == "new":
            '''
            create a new node
                {command:"new",
                    node:{name,pos:[,,]}
                }
            '''
            node=m['node']
            if node['name'] in env.nodes:
                await websocket.send("err no such node %s" % node['name'])
            else:
                env.nodes.update({node['name']:Environment.Node(node,env)})
                await websocket.send("msg node %s created" % node['name'])
            
        elif m['command'] == "mov":
            '''
            move a node to pos
                {command:"mov",node_name,pos}
            '''
            node_name=m['node_name']
            if node_name in env.nodes:
                env.nodes[node_name].move(m['pos'])
                await websocket.send("msg node %s moved to %s" % (node_name,m['pos']))
            else:
                await websocket.send("err no such node %s" % node_name)

        elif m['command'] == "cod":
            '''
            set code of a node
                {command:"cod",node_name,code}
            '''
            node_name=m['node_name']
            if node_name in env.nodes:
                env.nodes[node_name].set_code(m['code'])
                await websocket.send("msg changed node %s's code" % node_name)
            else:
                await websocket.send("err no such node %s" % node_name)

        elif m['command'] == "exc":
            '''
            run a node 
                {command:"exc",node_name}
            '''
            env.tasks.put(m['node_name'],block=False)

        elif m['command'] == "upd":
            '''
            if there are changes after last upd command, send those changes to client
                {command:"upd",(int)max_steps}
            '''
            for name,node in env.nodes.items():# directly update code in nodes
                if name in node_history_client:
                    if node_history_client[name]!=node.latest_history:
                        await websocket.send("cod "+json.dumps({'name':name,'code':node.code}))
                        node_history_client[name]=node.latest_history
                else:
                    await websocket.send("cod "+json.dumps({'name':name,'code':node.code}))
                    node_history_client[name]=node.latest_history
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

        
        elif m['command'] == "udo":
            '''
            undo  
            {command:"udo",name}
            if node_name=='', apply undo on env
                {command:"udo",node_name}
            '''
            node_name= m['node_name']
            if node_name=='':
                if env.Undo():
                    await websocket.send("msg env undone" )
                else:
                    await websocket.send("msg  noting to undo" )
            else:
                if node_name in env.nodes:
                    if env.nodes[node_name].Undo():
                        await websocket.send("msg node %s undone" % node_name)
                    else:
                        await websocket.send("msg node %s noting to undo" % node_name)
                else:
                    await websocket.send("err no such node %s" % node_name)
                

        elif m['command'] == "rdo":
            '''
            redo
            if node_name=='', apply redo on env
                {command:"rdo",node_name}
            '''
            node_name= m['node_name']
            if node_name=='':
                if env.Redo():
                    await websocket.send("msg env redone" )
                else:
                    await websocket.send("msg  noting to redo" )
            else:
                if node_name in env.nodes:
                    if env.nodes[node_name].Redo():
                        await websocket.send("msg node %s redone" % node_name)
                    else:
                        await websocket.send("msg node %s noting to redo" % node_name)
                else:
                    await websocket.send("err no such node %s" % node_name)


async def Update_client(ws,direction,history_item):
    '''
    for client that aren't on head,
    if direction == 1, do/redo the action in history_item
    if direction == -1, undo the action in history_item
    '''
    # history_item can't be type "stt"
    if direction==1:
        if history_item.type=="mov":# move node to new position
            await ws.send("mov "+json.dumps({'node_name':history_item.content['name'],'pos':history_item.content['new']}))
        if history_item.type=="new":# add new node
            await ws.send("new "+json.dumps({'node_name':history_item.content['name'],'pos':history_item.content['pos']}))
    else:
        if history_item.type=="mov":# move node back to old position
            await ws.send("mov "+json.dumps({'node_name':history_item.content['name'],'pos':history_item.content['old']}))
        if history_item.type=="new":# remove node
            await ws.send("rmv "+json.dumps({'node_name':history_item.content['name']}))
    
        
@router.route("/lobby") #* lobby
async def lobby(websocket, path):

    async for message in websocket:
        prefix=message[:3]
        message=message[4:]
        if prefix == "stt": # start a env
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

