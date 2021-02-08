import websockets
import asyncio

import traceback

import contextlib
import ast




commands=[]



async def server(websocket, path):
    async for message in websocket:
        

start_server = websockets.serve(server, "localhost", 1000)
asyncio.get_event_loop().run_until_complete(start_server)
asyncio.get_event_loop().run_forever()

