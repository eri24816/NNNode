from typing import Dict, List, Tuple, Optional, Type
from typing import TYPE_CHECKING
import websockets
import asyncio
import threading
from .space import Space
from .object import Object
import json

space_class : Optional[type] = None

if TYPE_CHECKING:
    spaces: Dict[str,Space]=dict()
else:
    spaces=dict()

obj_classes = Dict[str, type]

import websockets_routes

router = websockets_routes.Router()

message_sender_started = False

@router.route("/lobby") #* lobby
async def lobby(websocket : websockets.legacy.server.WebSocketServerProtocol, path):
    global message_sender_started
    if not message_sender_started:
        asyncio.create_task(message_sender_loop())
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
            
            new_space : Space = space_class(name=space_name,obj_classes=obj_classes,root_obj_class = root_obj_class)
            new_thread=threading.Thread(target=new_space.main_loop,name=space_name)
            new_thread.setDaemon(True)
            new_space.thread=new_thread
            spaces.update({space_name:new_space})
            new_thread.start()

            new_space.send_message = lambda content, ws=None, exclude_ws = []: messages_to_client.put_nowait(([w for w in new_space.ws_clients if w != exclude_ws] if ws == None else [ws], content))

            print(f'space {space_name} created')

# The main loop that handle an ws client connected to an space
@router.route("/space/{space_name}")
async def space_ws(websocket : websockets.legacy.server.WebSocketServerProtocol, path):
    '''
    Connect the client to the space
    '''
    space_name=path.params["space_name"]
    if space_name in spaces:
        space=spaces[space_name]
        await websocket.send("msg connected to space %s" % space_name)
    else:
        await websocket.send("err no such space %s" % space_name)
        websocket.close()
        return

    space.OnClientConnection(websocket)
    print(f"Client connected to {space_name}")

    '''
    Session loop
    '''
    try:
        lock = asyncio.Lock()
        async for message in websocket:
            async with lock:
                space.recieve_message(message,websocket)

    except websockets.exceptions.ConnectionClosed:
        print(f"Client disconnected from {space_name}")

messages_to_client: Optional[asyncio.Queue[Tuple[list[websockets.legacy.server.WebSocketServerProtocol],Dict]]] = None
async def message_sender_loop():
    '''
    The async loop that reads messages_to_client then sends messages to clients
    '''
    global messages_to_client
    messages_to_client = asyncio.Queue() 
    while True:
        ws_list,message = await messages_to_client.get()
        for ws in ws_list:
            if not ws.open:
                continue
            await ws.send(json.dumps(message, indent=4))

def start(space_class_:Type[Space], obj_classes_ : Dict[str,Type[Object]], root_obj_class_ : Type[Object],host = 'localhost', port = 1000):
    """
    The entry point of the ObjectSync server

    Args:
        space_class_ (Type[Space]): Specify an implementation of Space

        obj_classes_ (Dict[str,Type[Object]]): A dictionary of Object classes that can be created in the space

        root_obj_class_ (Type[Object]): The class of the root object of the space. It will be created automatically when the space is started.
    """

    global space_class, obj_classes, root_obj_class
    
    space_class = space_class_

    if not issubclass(space_class,Space):
        raise Exception("space_class should inherit objectsync_server.Space.")

    obj_classes = obj_classes_
    root_obj_class = root_obj_class_
    
    for c in obj_classes.values():
        if not issubclass(c,Object):
            raise Exception(f"{type(c)} in obj_classes doesn't inherit objectsync_server.Object.")
    
    start_server = websockets.serve(router, host, port)
    asyncio.get_event_loop().run_until_complete(start_server)

    print("="*50)
    print(f'ObjectSync server started on {host}:{port}\n')
    print('Space class:')
    print(f'\t{space_class}')
    print('Object classes:')
    for c in obj_classes.values():
        print(f'\t{c}')

    print("="*50)

    try:
        asyncio.get_event_loop().run_forever()
    except KeyboardInterrupt:
        print('KeyboardInterrupt')
        exit()

