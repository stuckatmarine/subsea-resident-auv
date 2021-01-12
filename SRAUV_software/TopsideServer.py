#!/usr/bin/env python
# Topside Server

import sys
import json
import time
from datetime import datetime
from SimpleWebSocketServer import SimpleWebSocketServer, WebSocket

from SRAUV_settings import SETTINGS
import Timestamp
import CommandMsg
import TelemetryMsg

def main():
    # Create a TCP/IP socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    # Bind the socket to the port
    server_address = ('localhost', 8001)
    print(f"{sys.stderr}, 'starting up on %s port %s' - {server_address}")
    sock.bind(server_address)

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

    print(f"{sys.stderr}, '\nwaiting to receive message' - {server_address}")
    data, address = sock.recvfrom(4096)
    
    print(f"{sys.stderr}, 'received %s bytes from %s' - {server_address}")
    print(f"{sys.stderr}, 'datas' - {data}")
    
    if data:
        sent = sock.sendto(data, address)
        print(f"{sys.stderr}, 'sent %s bytes back to %s' - {server_address}")

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