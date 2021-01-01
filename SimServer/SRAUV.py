#!/usr/bin/env python
# SRAUV

from inputs import get_gamepad
import asyncio
import json
import websocket
import time

def main():
    uri = "ws://localhost:8000"
    ws = websocket.create_connection(uri)

    interval_ms = 100 # update loop timer
    last_update_ms = 0
    state = "idle"
    txCmds = True

    tx_num = 0
    hasCmd = False
    cmd = {
        "source" : "vehicle",
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

    tel = {
        "source" : "vehicle",
        "msgNum" : 1,
        "state" : "none",
        "msgType" : "telemetry",
        "timestamp" : time.strftime("%Y-%m-%d %H:%M.%S"),
        "fwdDist" : 6.1, 
        "rightDist" : 7.1,
        "rearDist" : 8.1,
        "leftDist" : 0.1,
        "depth" : 10.1,
        "alt" : 11.1,
        "posX" : 11.1,
        "posY" : 12.1,
        "posZ" : 11.1,
        "heading" : 315.1,
        "assetDistances" : 
        {
            "cage" : 12.1,
            "tree1" : 13.1,
            "tree2" : 14.1
        }
    }

    while 1:
        # use update interval
        time_now = int(round(time.time() * 1000))
        if time_now - last_update_ms >= interval_ms:

            if state == "idle":
                state = "running"
                tel["state"] = state

            elif state == "running":
                tel["state"] = state
                # read sensore
                # update cmd

            if hasCmd:  
                if txCmds:
                    print(f" send vehicle trust cmd")
                    ws.send(json.dumps(cmd))
                else:
                    # apply thrust
                    print(f" vehicle trust local")


            ws.send(json.dumps(tel))
            last_update_ms = int(round(time.time() * 1000))
            tx_num += 1

    ws.close()

if __name__ == "__main__":
    main()