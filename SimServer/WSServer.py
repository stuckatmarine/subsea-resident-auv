#!/usr/bin/env python

# WS server example

import asyncio
import websockets
import json
import time

async def hello(websocket, path):
    recvMsg = await websocket.recv()
    
    print(f"< {recvMsg}")

    obj = {
            "source" : "sim",
            "msgNum" : 1,
            "msgType" : "telemetry",
            "timestamp" : time.strftime("%Y-%m-%d %H:%M.%S"),
            "latThrust" : [6.0,7.0,8.0,9.0],
            "vertThrust" : [10.0, 11.0]
    }

    resp = json.dumps(obj)
    await websocket.send(resp)
    print(f"> {resp}")

start_server = websockets.serve(hello, "localhost", 8765)

asyncio.get_event_loop().run_until_complete(start_server)
asyncio.get_event_loop().run_forever()