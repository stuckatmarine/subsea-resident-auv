#!/usr/bin/env python
#   internal_socket_server.py
#   Starts a socket server thread for internal communication. It listens for incoming
#     messages and responds with appropriate outgoing messages for the source.
#
#   Used to communicate to the srauv_main by other processes such as:
#     -  external_websocket process with incoming commands or telemetry (SIM) 
#     -  computer_vision process with updates vision target locations
#
#   Socket transmit binary "utf-8" encoded data.

import socket
import sys
import threading
import time
import queue
import json
from datetime import datetime

import logger

log_filename = str(f'Logs/{datetime.now().strftime("IS--%m-%d-%Y_%H-%M-%S")}.log')
logger = logger.setup_logger("internal_socket_server", log_filename)

class LocalSocketThread(threading.Thread):
    def __init__(self, address, tel, cmd):
        threading.Thread.__init__(self)
        self.kill_received = False
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.bind(address)
        self.tel = tel
        self.cmd = cmd
        self.default_response = str('dflt response').encode('utf-8') # for testing, '' also acceptable
        logger.info("LocalSocketThread started")


    def run(self):
        while not self.kill_received:
            try:
                data, address = self.sock.recvfrom(4096)
            
                logger.info(f"addr:{address} data:{data}")
                # if is telemetry, do soemthing
                # if is command, do soemthing

                # respond with srauv's current telemetry as placeholder
                tel_bytes = json.dumps(self.tel).encode("utf-8")
                self.sock.sendto(tel_bytes, address)
                    

            except KeyboardInterrupt:
                logger.warning("Exiting via interrupt")
                break

            except socket.timeout as e:
                logger.warning(f"Exiting via socket timeout e:{e}")
                break

            time.sleep(0.001)