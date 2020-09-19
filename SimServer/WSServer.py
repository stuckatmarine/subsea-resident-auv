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
        recvMsg = self.data
        print(f"< {recvMsg}")

        #  test response object
        obj = {
                "source" : "server",
                "msgNum" : 1,
                "msgType" : "command",
                "timestamp" : time.strftime("%Y-%m-%d %H:%M.%S"),
                "latThrust" : [6.0,7.0,8.0,9.0],
                "vertThrust" : [10.0, 11.0]
        }

        resp = json.dumps(obj)
        self.sendMessage(resp)
        print(f"> {resp}")

    def handleConnected(self):
        print(self.address, 'connected')

    def handleClose(self):
        print(self.address, 'closed')

server = SimpleWebSocketServer('', 8000, SimpleEcho)
server.serveforever()