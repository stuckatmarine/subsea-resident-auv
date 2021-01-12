#!/usr/bin/env python
# WS server example

import asyncio
import websockets
import json
import time
from datetime import datetime
from SimpleWebSocketServer import SimpleWebSocketServer, WebSocket

import Timestamp
import CommandMsg
import TelemetryMsg

source = "server"
cmd = CommandMsg.make(source)
tel = TelemetryMsg.make(source)

sim_echo = True
supress_img = True

class SimpleForward(WebSocket):

    # print recvMsg, strip image data if required
    def print_msg(self, recvMsg, replace_image):
        if replace_image == True and 'imgStr' in recvMsg.keys():
            recvMsg["imgStr"] = "replaced str"

        print(f"< {recvMsg}")

    # send message to SIM over ws
    def sim_forward(self, recvMsg):
        self.sendMessage(self.data)
        print(f"> forwarded msg to SIM, {recvMsg['source']} -> {recvMsg['dest']}")

    # server respond to SIM with placeholder msgs
    def send_sim_test_msgs(self):
        cmd["timestamp"] = Timestamp.make()
        resp = json.dumps(cmd)
        self.sendMessage(resp)
        # print(f"> {resp}")

        tel["timestamp"] = Timestamp.make()
        resp = json.dumps(tel)
        self.sendMessage(resp)
        # print(f"> {resp}")

    # act on gamepad input
    def gamepad_actions(self, recvMsg):
        if 'buttons' in recvMsg.keys():
            print(f"gamepad buttons pressed: {recvMsg['buttons']}")
            # logic to do actions based on which buttons are used
        else:
            print(f"no gamepad buttons pressed")
            # logic to null cmd

    # msg parsing
    def handleMessage(self):
        recvMsg = json.loads(self.data)
        self.print_msg(recvMsg, supress_img)

        if recvMsg["source"] == "sim" and sim_echo == True:
            self.send_sim_test_msgs()

        elif recvMsg["source"] == "gamepad":
            self.gamepad_actions(recvMsg)

        elif recvMsg["source"] == "vehicle":
            if recvMsg["dest"] == "sim":
                self.sim_forward(recvMsg)

    def handleConnected(self):
        print(self.address, 'connected')

    def handleClose(self):
        print(self.address, 'closed')

server = SimpleWebSocketServer('', 8001, SimpleForward)
print('server up')
server.serveforever()