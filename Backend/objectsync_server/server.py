'''
Websocket server for NNobj

API:
    /lobby/{space_name}  Lobby
        Usage:
            1. Connect
            Send commands to server :
                - stt <space name>
                    1. Wait for a respond message from server
        
    /space/{space_name}  Interact with an spaceironment
        Usage:
            1. Connect
            2. Wait for a respond message from server
            3. 
            Send commands to server :
                - new : create a obj or a flow
                - cod : modify code in a Codeobj
                - act : activate a obj
                - mov : move a obj
            Recieve messages form server : 
                



prefix:
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
from typing import Dict, Tuple, Optional
import typing
from typing import TYPE_CHECKING
import websockets
import asyncio
import threading
if True:
    from .space import Space
    from .object import Object
import json

space_class : Optional[type] = None

if TYPE_CHECKING:
    spaces: typing.Dict[str,Space]=dict()
else:
    spaces=dict()

obj_classes = Dict[str, type]

import websockets_routes

router = websockets_routes.Router()

message_sender_started = False

def set_space_class(space_class_):
    global space_class
    space_class = space_class_

@router.route("/lobby") #* lobby
async def lobby(websocket : websockets.legacy.server.WebSocketServerProtocol, path):
    print( isinstance(space_class,Space))
    global message_sender_started
    if not message_sender_started:
        asyncio.create_task(direct_message_sender())
        #asyncio.create_task(buffered_message_sender())
        message_sender_started = True
    async for message in websocket:
        command=message[:3]
        message=message[4:]
        if command == "stt": # start a space
            space_name=message
            if space_name in spaces:
                await websocket.send("msg space %s is already running" % space_name)
                return
            else:
                await websocket.send("msg space %s has started" % space_name)
            
            new_space=space_class(name=space_name,obj_classes=obj_classes,base_obj_class = Object)
            new_thread=threading.Thread(target=new_space.main_loop,name=space_name)
            new_thread.setDaemon(True)
            new_space.thread=new_thread
            spaces.update({space_name:new_space})
            new_thread.start()

            new_space.send_direct_message = lambda content: messages_to_client.put_nowait((new_space.ws_clients, content))

            print(f'space {space_name} created')

# The main loop that handle an ws client connected to an space
@router.route("/space/{space_name}")
async def space_ws(websocket : websockets.legacy.server.WebSocketServerProtocol, path):
    space_name=path.params["space_name"]
    if space_name in spaces:
        space=spaces[space_name]
        await websocket.send("msg connected to space %s" % space_name)
    else:
        await websocket.send("err no such space %s" % space_name)
        websocket.close()
        return

    space.ws_clients.append(websocket)

    #TODO: client load entire space

    space.update_demo_objs()

    async for message in websocket:
        space.recieve_message(message,websocket)

messages_to_client: Optional[asyncio.Queue[Tuple[list[websockets.legacy.server.WebSocketServerProtocol],Dict]]] = None
async def direct_message_sender():
    # put message in the queue to send them to client
    global messages_to_client
    messages_to_client = asyncio.Queue() 
    while True:
        ws_list,message = await messages_to_client.get()
        for ws in ws_list:
            if not ws.open:
                continue
            await ws.send(json.dumps(message))

'''
async def buffered_message_sender():
    while(True):
        for space in spaces.values():
            # Each space has a message buffer
            if space.running_obj:
                space.running_obj.flush_output()
            for key, value in sorted(space.message_buffer.copy().items()):
                command_, id = key[1:4], key[5:].split('/')[0]
                if command_ == "atr":
                    messages_to_client.put_nowait((space.ws_clients,{'command': command_, 'id': id, 'name':value,'value':space.objs[id].attributes[value].value}))
                else:
                    messages_to_client.put_nowait((space.ws_clients,{'command': command_, 'id': id, 'info': value}))
                #print({'command': command_, 'id': id, 'value': value})
            space.message_buffer.clear()
        await asyncio.sleep(0.1)
'''

def start(space_class_, obj_classes_):
    global space_class, obj_classes
    
    space_class = space_class_

    if space_class == None:   
        raise Exception("Please assign objectsync_server.server.space_class for intantiating spaces")  
    if not issubclass(space_class,Space):
        raise Exception("objectsync_server.server.space_class should inherit objectsync_server.server.spaceironment.space.")

    obj_classes = obj_classes_
    
    for c in obj_classes:
        if not issubclass(c,Object):
            raise Exception(f"{type(c)} in obj_classes should inherit objectsync_server.Object.")
    
    start_server = websockets.serve(router, "localhost", 1000)

    asyncio.get_event_loop().run_until_complete(start_server)

    print('ObjectSync server started')
    print('Space class:')
    print(f'\t{space_class.__name__}')
    print('Object classes:')
    for c in obj_classes:
        print(f'\t{c.__name__}')

    try:
        asyncio.get_event_loop().run_forever()
    except KeyboardInterrupt:
        print('KeyboardInterrupt')

