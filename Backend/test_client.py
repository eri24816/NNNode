import asyncio
import app_objects

async def ws_server(ws):

    # send message to client
    await ws.send('server started') #* OK

    # create a node object
    app_objects.node(ws)

asyncio.get_event_loop().run_until_complete(ws_server)

