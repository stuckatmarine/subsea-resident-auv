#!/usr/bin/env python
# WS server example

import asyncio
import websockets
import json
import time

from SimpleWebSocketServer import SimpleWebSocketServer, WebSocket

class SimpleEcho(WebSocket):

    def handleMessage(self):
        # echo message back to client
        # self.sendMessage(self.data)
        recvMsg = json.loads(self.data)

        if recvMsg["source"] == "sim":

            imgStr = ""

            # remove imgStr from printing
            # try:
            if 'imgStr' in recvMsg.keys():
                recvMsg["imgStr"] = "replaced str"
                # imgStr = recvMsg["imgStr"]
            # except:
            #     print(f" no img str")

            print(f"< {recvMsg}")

            # if true: # server respond with placeholder msg
            #  test response object
            cmd = {
                    "source" : "server",
                    "msgNum" : 1,
                    "msgType" : "command",
                    "timestamp" : time.strftime("%Y-%m-%d %H:%M.%S"),
                    "thrustFwd" : -1.0,
                    "thrustRight" : 0.0,
                    "thrustRear" : 0.0,
                    "thrustLeft" : 0.0,
                    "vertA" : 1.0,
                    "vertB" : 0.0,
            }

            resp = json.dumps(cmd)
            self.sendMessage(resp)
            print(f"> {resp}")

            tel = {
                    "source" : "server",
                    "msgNum" : 1,
                    "msgType" : "telemetry",
                    "timestamp" : time.strftime("%Y-%m-%d %H:%M.%S"),
                    "fwdDist" : 6.0, 
                    "rightDist" : 7.0,
                    "rearDist" : 8.0,
                    "leftDist" : 0.2,
                    "depth" : 10.0,
                    "alt" : 11.0,
                    "posX" : 11.0,
                    "posY" : 12.0,
                    "posZ" : 11.11,
                    "heading" : 315.2,
                    "assetDistances" : 
                    {
                        "cage" : 12.0,
                        "tree1" : 13.0,
                        "tree2" : 14.0
                    }


            }

            resp = json.dumps(tel)
            self.sendMessage(resp)
            print(f"> {resp}")

        elif recvMsg["source"] == "gamepad":
            # forward gamepad input to sim
            self.sendMessage(self.data)
            print(f"> {self.data}")


    def handleConnected(self):
        print(self.address, 'connected')

    def handleClose(self):
        print(self.address, 'closed')

server = SimpleWebSocketServer('', 8000, SimpleEcho)
server.serveforever()