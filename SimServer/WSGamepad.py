#!/usr/bin/env python
# WS gamepad

from inputs import get_gamepad
import asyncio
import json
# import websockets
import websocket
import time


def main():
    uri = "ws://localhost:8000"
    ws = websocket.create_connection(uri)

    """Parse gamepad inputs and send to server"""
    interval_ms = 500
    last_msg_time_ms = 0
    tx_num = 0
    cmd = {
        "source" : "gamepad",
        "msgNum" : tx_num,
        "msgType" : "command",
        "timestamp" : time.strftime("%Y-%m-%d %H:%M.%S"),
        "thrustFwd" : 0.0,
        "thrustRight" : 0.0,
        "thrustRear" : 0.0,
        "thrustLeft" : 0.0,
        "vertA" : 0.0,
        "vertB" : 0.0
    }

    #  initial tx
    ws.send(json.dumps(cmd))
    last_msg_time_ms = int(round(time.time() * 1000))
    tx_num += 1

    while 1:

        #  update cmd based on inputs
        events = get_gamepad()
        for event in events:
            print(event.ev_type, event.code, event.state)
            if event.code == "ABS_HAT0X":
                if event.state > 0:
                    cmd["thrustRight"] = 1.0
                    cmd["thrustLeft"] = 0.0
                elif event.state < 0:
                    cmd["thrustRight"] = 0.0
                    cmd["thrustLeft"] = 1.0
                else:
                    cmd["thrustRight"] = 0.0
                    cmd["thrustLeft"] = 0.0
            elif event.code == "ABS_HAT0Y":
                if event.state > 0:
                    cmd["thrustFwd"] = 1.0
                    cmd["thrustRear"] = 0.0
                elif event.state < 0:
                    cmd["thrustFwd"] = 0.0
                    cmd["thrustRear"] = 1.0
                else:
                    cmd["thrustFwd"] = 0.0
                    cmd["thrustRear"] = 0.0

        time_now = int(round(time.time() * 1000))
        if time_now - last_msg_time_ms >= interval_ms:
            ws.send(json.dumps(cmd))
            last_msg_time_ms = int(round(time.time() * 1000))
            tx_num += 1

    ws.close()

if __name__ == "__main__":
    main()