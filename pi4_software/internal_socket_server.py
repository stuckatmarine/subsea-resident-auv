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

import socket
import sys
import threading
import time
import queue
import json
from datetime import datetime

import logger

class LocalSocketThread(threading.Thread):
    def __init__(self, address, tel, cmd, tel_recv, cmd_recv):
        threading.Thread.__init__(self)
        self.kill_received = False
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.sock.bind(address)
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
        self.logger = logger.setup_logger("internal_socket_server", log_filename)
        self.logger.info(f"Local socket thread started at {address}")
        print(f"Local socket thread started at {address}")


    def run(self):
        while not self.kill_received:
            try:
                # blocks until data recieved
                data, address = self.sock.recvfrom(4096)
                self.logger.info(f"< addr:{address} data:{data}")
                data_dict = json.loads(data.decode("utf-8"))

                if data_dict["msg_type"] == "telemetry":
                    self.tel_recv = data_dict

                    # update cmd_bytes if not most current
                    if self.cmd["msg_num"] > self.last_cmd_sent:
                        self.cmd_bytes = json.dumps(self.cmd).encode("utf-8")
                    
                    self.sock.sendto(self.cmd_bytes, address)
                    self.last_cmd_sent = self.cmd["msg_num"]
                    self.logger.info(f"> addr:{address} data:{self.cmd_bytes}")

                elif data_dict["msg_type"] == "command":
                    for k in data_dict:
                        if k == "msg_num":
                            continue
                        self.cmd_recv[k] = data_dict[k]
                        print(f"keys:{k}")
                    # update last as trigger of copy completed
                    self.cmd_recv["msg_num"] = data_dict["msg_num"]

                    # update tel_bytes if not most current
                    if self.tel["msg_num"] > self.last_tel_sent:
                        self.tel_bytes = json.dumps(self.tel).encode("utf-8")

                    # immediatly log kill recvd in case msg is missed
                    if data_dict["force_state"] == "kill":
                        self.cmd_with_kill_recvd = True
                        self.logger.warining(f"Kill cmd received")
                    
                    self.sock.sendto(self.tel_bytes, address)
                    self.last_tel_sent = self.tel["msg_num"]
                    self.logger.info(f"> addr:{address} data:{self.tel_bytes}")

                elif data_dict["msg_type"] == "distance":
                    # update cmd_bytes if not most current
                    sensor_idx = data_dict["sensor_idx"]
                    self.dist_values[sensor_idx] = data_dict["sensor_value"]
                    
                    self.sock.sendto(self.default_response, address)
                    print(f"Recvd sensor_idx:{sensor_idx} distance:{self.dist_values[sensor_idx]}")

                # respond with srauv's default response
                else:
                    self.sock.sendto(self.default_response, address)
                    self.logger.warning(f"< unknonw msg_type received by internal socket, {data_dict['msg_type']}")
                    
            except KeyboardInterrupt:
                self.logger.warning("Exiting via interrupt")
                break

            except socket.timeout as e:
                self.logger.warning(f"Exiting via socket timeout e:{e}")
                break

            except Exception as ex:
                self.logger.warning(f"General internal socket exception ex:{ex}")
                break

            time.sleep(0.001)