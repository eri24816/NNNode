import asyncio
async def ws_sender():
    while True:
        await a()

async def b():
    pass

def a():
    print('sync')
async def a():
    await b()
    print('async')

asyncio.run(ws_sender())
#asyncio.get_event_loop().run_until_complete(start_server)
try:
    asyncio.get_event_loop().run_forever()
except KeyboardInterrupt:
    print('KeyboardInterrupt')
