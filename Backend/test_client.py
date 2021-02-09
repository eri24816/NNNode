import asyncio
import websockets
import time

async def start(i):
    uri = "ws://localhost:1000/lobby"
    async with websockets.connect(uri) as websocket:
        command = "stt my_env"
        print('>>>' + command)
        await websocket.send(command)
        print(await websocket.recv())

async def env(i):
    uri = "ws://localhost:1000/env/my_env"
    async with websockets.connect(uri) as websocket:
        print(await websocket.recv())
        command = "cod \"0\" np=456456"
        print('>>>' + command)
        await websocket.send(command)
        command = "exc 0"
        print('>>>' + command)
        await websocket.send(command)
        command = "exc 1"
        print('>>>' + command)
        await websocket.send(command)
            
task = []
task.append(start(''))
task.append(env('exc 0'))

asyncio.get_event_loop().run_until_complete(asyncio.gather(*task))