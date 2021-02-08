import asyncio
import websockets
import time
async def hello(i):
    uri = "ws://localhost:1000"
    async with websockets.connect(uri) as websocket:
        command = i
        print('>>>' + command)
        output = ''
        await websocket.send(command)
        print('5+'+str(i)+'='+await websocket.recv())
        time.sleep(0)
            
task = []
task.append(hello('i=1\nwhile 1:\n    i+=1'))
for i in range(10):
    task.append(hello('5+'+str(i)))
asyncio.get_event_loop().run_until_complete(asyncio.gather(*task))