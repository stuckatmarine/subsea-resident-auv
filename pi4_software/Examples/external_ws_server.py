#!/usr/bin/env python
#   external_ws_server.py
#   Starts a websocket server for external communication.
#   Should remain idle during flight if autonmous/ untethered,
#     reduct networking and logging load on main process if
#     communicating externally topside.

import asyncio
import websockets
import socket
import json
import time
import sys
from datetime import datetime
from SimpleWebSocketServer import SimpleWebSocketServer, WebSocket

import timestamp
import logger
from srauv_settings import SETTINGS

class SrauvExternalWSS(WebSocket):
    # Setup
    log_filename = str(f'Logs/{datetime.now().strftime("ES--%m-%d-%Y_%H-%M-%S")}.log')
    logger = logger.setup_logger("external_ws_logger", log_filename)

    srauv_address = (SETTINGS["internal_ip"], SETTINGS["main_msg_port"])
    srauv_address = srauv_address
    try:
        srauv_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # internet, udp
        print(f"SrauvExternalWSS forwarding to internal socket {srauv_address}")
        logger.info(f"SrauvExternalWSS forwarding to internal socket {srauv_address}")
    except socket.error:
        logger.info("Failed To Create main_msg_socket in external_ws_server")
        sys.exit()

    def handleMessage(self):
        try:
            # Received ws msg from external network. Log it
            self.logger.info(f"sending to {self.srauv_address}  data:{self.data}")

            # Forward msg to srauv_main's local socket
            self.srauv_socket.sendto(self.data.encode("utf-8"), self.srauv_address)

            # Get response from srauv_main. Log it
            data, addr = self.srauv_socket.recvfrom(4096)
            srauv_response = data.decode("utf-8")

            # Forward response back over external network
            self.sendMessage(srauv_response)
            self.logger.info(f"> responding to topsides, msg:{srauv_response}")

        except socket.error:
            self.logger.info(f"Failed to send over socket, address:{self.srauv_address}")


    def handleConnected(self):
        self.logger.info(f'connected {self.address}')

    def handleClose(self):
        self.logger.info(f'closed {self.address}')

def SrauvExternalWSS_start():
    print(f"SrauvExternalWSS_start at {SETTINGS['external_ip']}:{SETTINGS['external_port']}")
    server = SimpleWebSocketServer(SETTINGS["external_ip"], SETTINGS["external_port"], SrauvExternalWSS)
    try:
        server.serveforever()
    except KeyboardInterrupt:
        print("Keyboad Interrup caught, closing external server")
        sys.exit()