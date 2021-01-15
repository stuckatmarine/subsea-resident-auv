#!/usr/bin/env python
# WS client example to test server

import asyncio
import websockets
import json
import time

async def hello():
    uri = "ws://localhost:8001"
    async with websockets.connect(uri) as websocket:
        inp = input("Input msg number? ")

        obj = {
            "source" : "sim",
            "msgNum" : inp,
            "msgType" : "telemetry",
            "timestamp" : time.strftime("%Y-%m-%d %H:%M.%S"),
            "cardDist" : [6.0,7.0,8.0,9.0],
            "depth" : 10.0,
            "alt" : 11.0,
            "assetDistances" : 
            {
                "cage" : 12.0,
                "tree1" : 13.0,
                "tree2" : 14.0
            }
        }

        msg = json.dumps(obj)
        await websocket.send(msg)
        print(f"> {msg}")

        resp = await websocket.recv()
        print(f"< {resp}")

asyncio.get_event_loop().run_until_complete(hello())