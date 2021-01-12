#!/usr/bin/env python
# SRAUV

from inputs import get_gamepad
import asyncio
import json
import sys
import websocket
import time
from datetime import datetime

from SRAUV_settings import SETTINGS
import Timestamp
import CommandMsg
import TelemetryMsg

def main():
    # try:
    uri = "ws://" + SETTINGS["ip_server"] + ":" + str(SETTINGS["port_server"])
    ws = websocket.create_connection(uri)
    # except ws.error:
    #     print("Failed To Create Socket")
    #     sys.exit()

    update_interval_ms = SETTINGS["tel_tx_interval_ms"] # update loop timer
    last_update_ms = 0
    source = "vehicle"
    state = "idle"
    txCmds = True
    tx_num = 0
    hasCmd = False

    cmd = CommandMsg.make(source, "sim")
    tel = TelemetryMsg.make(source, "sim")

    print(f'SRAUV up, timestamp:{Timestamp.make()} state:{tel["state"]}')

    while 1:
        # use update interval
        time_now = int(round(time.time() * 1000))
        if time_now - last_update_ms >= update_interval_ms:
            delta_us_start = datetime.utcnow().microsecond

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

            delta_us = datetime.utcnow().microsecond - delta_us_start
            if delta_us < 0:
                delta_us += 1000000
            print(f'timestamp:{Timestamp.make()} state:{tel["state"]}, delta_us:{delta_us}')
        # else:
        #     time.sleep(.001) # 1 ms, accurate on pc without this, might be needed on PI

    ws.close()

if __name__ == "__main__":
    main()