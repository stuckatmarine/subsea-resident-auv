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

source = "external_ws_server"
srauv_address = (SETTINGS["internal_ip"], SETTINGS["main_msg_port"])

log_filename = str(f'Logs/{datetime.now().strftime("ES--%m-%d-%Y_%H-%M-%S")}.log')
logger = logger.setup_logger("external_ws_logger", log_filename)

try:
    srauv_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # internet, udp
except socket.error:
    logger.info("Failed To Create main_msg_socket in external_ws_server")
    sys.exit()


class GetSrauvResponse(WebSocket):
    def handleMessage(self):
        try:
            # Received ws msg from external network. Log it
            logger.info(f"sending to {srauv_address}  data:{self.data}")
            print(f"sending to {srauv_address}  data:{self.data}")

            # Forward msg to srauv_main's local socket
            srauv_socket.sendto(self.data.encode("utf-8"), srauv_address)

            # Get response from srauv_main. Log it
            data, addr = srauv_socket.recvfrom(4096)
            srauv_response = data.decode("utf-8")
            logger.info(f"recv {srauv_response} from {addr}")

            # Forward response back over external network
            self.sendMessage(srauv_response)
            logger.info(f"> responding to topsides, msg:{srauv_response}")

        except socket.error:
            logger.info(f"Failed to send over socket, address:{srauv_address}")


    def handleConnected(self):
        logger.info(f'connected {self.address}')

    def handleClose(self):
        logger.info(f'closed {self.address}')

logger.info(f"starting external server process")
server = SimpleWebSocketServer(SETTINGS["external_ip"], SETTINGS["external_port"], GetSrauvResponse)
server.serveforever()
logger.info(f"stopping external server process")