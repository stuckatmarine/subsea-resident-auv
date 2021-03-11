#!/usr/bin/env python
#   internal_socket_server.py
#   Starts a socket server thread for internal communication. It listens for incoming
#     messages and responds with appropriate outgoing messages for the source.
#
#   Used to communicate to the srauv_main by other processes such as:
#     -  external_websocket process with incoming commands or telemetry (SIM) 
#     -  computer_vision process with updates vision target locations
# 
#   Receives tel_msg -> cmd_msg response
#   Receives cmd_msg -> tel_msg response
#
#   Socket transmit binary "utf-8" encoded data.

import asyncio
import websockets
import socket
import sys
import threading
import time
import queue
import json
from datetime import datetime
from SimpleWebSocketServer import SimpleWebSocketServer, WebSocket

from srauv_settings import SETTINGS


class SrauvExternalWSS(WebSocket):
    print(f"SrauvExternalWSS_start at {SETTINGS['internal_ip']}:{SETTINGS['internal_port']}")

    def handleMessage(self):
        try:
            # # Received ws msg from external network. Log it
            # # self.logger.info(f"sending to {self.srauv_address}  data:{self.data}")

            # # Forward msg to srauv_main's local socket
            # self.srauv_socket.sendto(self.data.encode("utf-8"), self.srauv_address)

            # # Get response from srauv_main. Log it
            # data, addr = self.srauv_socket.recvfrom(4096)
            # srauv_response = data.decode("utf-8")

            # # Forward response back over external network
            # self.sendMessage(srauv_response)
            # # self.logger.info(f"> responding to topsides, msg:{srauv_response}")


            # # blocks until data recieved, send 'stop' to break
            # client, address = self.socket.accept()
            # self.logger.info(f"< addr:{address} client:{client}")
            # data = client.recvfrom(4096)[0].decode("utf-8")
            print.info(f"< data:{data}")
            data_dict = json.loads(data)

            if data_dict["msg_type"] == "telemetry":
                self.tel_recv = data_dict

                # update cmd_bytes if not most current
                if self.cmd["msg_num"] > self.last_cmd_sent:
                    self.cmd_bytes = json.dumps(self.cmd).encode("utf-8")
                
                client.sendto(self.cmd_bytes, address)
                self.last_cmd_sent = self.cmd["msg_num"]
                # self.logger.info(f"> addr:{address} data:{self.cmd_bytes}")

            elif data_dict["msg_type"] == "command":
                for k in data_dict:
                    if k == "msg_num":
                        continue
                    self.cmd_recv[k] = data_dict[k]
                # print(f"self.cmd_recv::{self.cmd_recv}")
                # update last as trigger of copy completed
                self.cmd_recv["msg_num"] = data_dict["msg_num"]

                # update tel_bytes if not most current
                if self.tel["msg_num"] > self.last_tel_sent:
                    self.tel_bytes = json.dumps(self.tel).encode("utf-8")

                # immediatly log kill recvd in case msg is missed
                if data_dict["force_state"] == "kill":
                    self.cmd_with_kill_recvd = True
                    print(f"Kill cmd received")
                
                client.sendto(self.tel_bytes, address)
                self.last_tel_sent = self.tel["msg_num"]
                # self.logger.info(f"> addr:{address} data:{self.tel_bytes}")

            elif data_dict["msg_type"] == "distance":
                # update cmd_bytes if not most current
                sensor_idx = data_dict["sensor_idx"]
                self.dist_values[sensor_idx] = data_dict["sensor_value"]
                
                client.sendto(self.default_response, address)
                print(f"Recvd sensor_idx:{sensor_idx} distance:{self.dist_values[sensor_idx]}")

            # respond with srauv's default response
            else:
                print(f"< unknonw msg_type received by internal socket, {data_dict['msg_type']}")
                client.sendto(self.default_response, address)
                    
        except KeyboardInterrupt:
            print("Exiting via interrupt")
            break

        except Exception as ex:
            print(f"General internal socket exception ex:{ex}")
            break

    def handleConnected(self):
        print(f'connected {self.address}')

    def handleClose(self):
        print(f'closed {self.address}')

def SrauvExternalWSS_start():
    print(f"SrauvExternalWSS_start at {SETTINGS['external_ip']}:{SETTINGS['external_port']}")
    server = SimpleWebSocketServer(SETTINGS["external_ip"], SETTINGS["external_port"], SrauvExternalWSS)
    try:
        server.serveforever()
    except KeyboardInterrupt:
        print("Keyboad Interrup caught, closing external server")
        sys.exit()

class LocalSocketThread(threading.Thread):
    def __init__(self, address, tel, cmd, tel_recv, cmd_recv):
        threading.Thread.__init__(self)
        self.kill_received = False
        self.server = SimpleWebSocketServer(SETTINGS["external_ip"], SETTINGS["external_port"], SrauvExternalWSS)
        self.tel = tel
        self.tel_recv = tel_recv
        self.tel_bytes = json.dumps(self.tel).encode("utf-8")
        self.last_tel_sent = -1
        self.cmd = cmd
        self.cmd_recv = cmd_recv
        self.cmd_bytes = json.dumps(self.cmd).encode("utf-8")
        self.last_cmd_sent = -1
        self.cmd_with_kill_recvd = False
        self.default_response = str('dflt response').encode('utf-8') # for testing, '' also acceptable
        self.dist_values = tel["dist_values"]

        log_filename = str(f'Logs/{datetime.now().strftime("IS--%m-%d-%Y_%H-%M-%S")}.log')
        print(f"Local socket thread started at {address}")

    def run(self):
        while not self.kill_received:
            try:
                server.serveforever()
        except KeyboardInterrupt:
            print("Keyboad Interrup caught, closing websocket server")
            sys.exit()

            time.sleep(0.001)