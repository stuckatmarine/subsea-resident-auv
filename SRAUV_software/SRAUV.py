#!/usr/bin/env python
# SRAUV

import asyncio
import json
import sys
import socket
import time
import logging
import threading
import signal
from datetime import datetime
from datetime import timedelta
from twisted.internet import task, reactor

from inputs import get_gamepad
from SRAUV_settings import SETTINGS
import DistanceSensor
import Timestamp
import CommandMsg
import TelemetryMsg

###################  Globals Variables  ###################
UPDATE_INTERVAL_MS = SETTINGS["update_interval_ms"]
TEL_TX_INTERVAL_MS = SETTINGS["tel_tx_interval_ms"]
srauv_address = (SETTINGS["srauv_ip"], SETTINGS["srauv_port"])
source = "vehicle"
state = "idle"
txCmds = True
hasCmd = False
supress_img = True
sim_echo = False
threads = []
dist_sensor_data = [0.0, 0.0, 0.0, 0.0, 0.0, 0.0]

cmd = CommandMsg.make(source, "sim")
tel = TelemetryMsg.make(source, "sim")

log_filename = str(f'Logs/{datetime.now().strftime("%m-%d-%Y_%H-%M-%S")}.log')
logging.basicConfig(filename=log_filename, filemode='w', format='%(asctime)s - %(message)s',level=logging.INFO)
logging.getLogger().addHandler(logging.StreamHandler(sys.stdout))

# Try opening a socket for communication
try:
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # internet, udp
    sock.settimeout(3)
except socket.error:
    print("Failed To Create Socket")
    sys.exit()

###################  Update Loop  ###################


# def update_loop(tel):
#     delta_us_start = datetime.utcnow().microsecond

#     if tel["state"] == "idle":
#         tel["state"] = "running"

#     elif tel["state"] == "running":
#         tel["state"] = "running"
#         # read sensore
#         # read inputs
#         try:
#             print("checking inputs")
#         except KeyboardInterrupt:
#             print("Exiting via interrupt")
#             sys.exit()
#         # get nav data
#         # do thrust

#     send_telemetry()

#     update_loop_delta_us = datetime.utcnow().microsecond - delta_us_start
#     if update_loop_delta_us < 0:
#         update_loop_delta_us += 1000000
#     print(f'timestamp:{Timestamp.make()} state:{tel["state"]}, update_loop_delta_us:{update_loop_delta_us}')

# ###################  Socket Server  ###################
# # print recvMsg, strip image data if required
# def print_msg_dict(msg, replace_image):
#     if replace_image == True and 'imgStr' in msg.keys():
#         msg["imgStr"] = "replaced str"

#     print(f"< {msg}")

# # send message to SIM over ws
# def sim_forward(recvMsg):
#     self.sendMessage(self.data)
#     print(f"> forwarded msg to SIM, {recvMsg['source']} -> {recvMsg['dest']}")

# # server respond to SIM with placeholder msgs
# def send_sim_test_msgs():
#     cmd["timestamp"] = Timestamp.make()
#     resp = json.dumps(cmd)
#     self.sendMessage(resp)
#     # print(f"> {resp}")

#     tel["timestamp"] = Timestamp.make()
#     resp = json.dumps(tel)
#     self.sendMessage(resp)
#     # print(f"> {resp}")

# act on gamepad input
# def gamepad_actions(recvMsg):
#     if 'buttons' in recvMsg.keys():
#         print(f"gamepad buttons pressed: {recvMsg['buttons']}")
#         # logic to do actions based on which buttons are used
#     else:
#         print(f"no gamepad buttons pressed")
#         # logic to null cmd

# def handleMessage(data):
#     recvMsg = json.loads(data)
#     print_msg_dict(recvMsg, supress_img)

#     if recvMsg["source"] == "sim" and sim_echo == True:
#         send_sim_test_msgs()

#     elif recvMsg["source"] == "gamepad":
#         gamepad_actions(recvMsg)

#     elif recvMsg["source"] == "vehicle":
#         if recvMsg["dest"] == "sim":
#             sim_forward(recvMsg)

def update_telemetry():
    tel["fwdDist"] = dist_sensor_data[0]
    tel["rightDist"] = dist_sensor_data[0]
    tel["rearDist"] = dist_sensor_data[0]
    tel["leftDist"] = dist_sensor_data[0]
    tel["depth"] = dist_sensor_data[0]
    tel["alt"] = dist_sensor_data[0]

def send_telemetry():
    sock.sendto(str.encode(json.dumps(tel)), srauv_address)
    tel["msgNum"] += 1

    data, server = sock.recvfrom(4096)
    data = data.decode("utf-8")

    if data != '':
        print(f"recv {data} from {server}")

def setup_distance_module(ds_config, data_arr):
    for id in range(ds_config["num_sensors"]):
        data_arr[id] = float(id)
        threads.append(DistanceSensor.ds_thread(ds_config,id, data_arr))

    for t in threads:
        t.start()

    logging.info(f'state:{tel["state"]} MSG:Distance sensors started')

def has_live_threads(threads):
    return True in [t.is_alive() for t in threads]

def stop_threads():
    logging.info("Trying to stop threads...")
    while has_live_threads(threads):
        try:
            for t in threads:
                t.kill_received = True  
        except Exception as e:
            logging.error(f"Thread stopping err:{e}")
            
    logging.info("Stopped threads")

def main():
    last_update_ms = 0
    last_tel_tx_ms = 0
    tel["state"] = "idle"
    logging.info(f'state:{tel["state"]} MSG:SRAUV main starting')

    setup_distance_module(SETTINGS["dist_sensor_config"], dist_sensor_data)

    while True:
        try:
            # update system
            time_now = int(round(time.time() * 1000))
            if time_now - last_update_ms >= UPDATE_INTERVAL_MS:
                delta_us_start = datetime.utcnow().microsecond

                # read sensore
                # calc nav data

                if tel["state"] == "idle":
                    tel["state"] = "running"

                elif tel["state"] == "running":
                    tel["state"] = "running"
                    # read inputs
                    # do thrust

                logging.info(f"dist_sensor_data:{dist_sensor_data}")

                last_update_ms = int(round(time.time() * 1000))
                ul_delta_us = datetime.utcnow().microsecond - delta_us_start
                if ul_delta_us < 0:
                    ul_delta_us += 1000000
                logging.info(f'state:{tel["state"]} ul_delta_us:{ul_delta_us}')
            
            # tel tx
            if time_now - last_tel_tx_ms >= TEL_TX_INTERVAL_MS:
                delta_us_start = datetime.utcnow().microsecond

                update_telemetry()
                send_telemetry()
                last_tel_tx_ms = int(round(time.time() * 1000))

                tel_delta_us = datetime.utcnow().microsecond - delta_us_start
                if tel_delta_us < 0:
                    tel_delta_us += 1000000
                logging.info(f'state:{tel["state"]} tel_delta_us:{tel_delta_us}')
            
            # else:
            #     time.sleep(0.001)

        except KeyboardInterrupt:
            logging.info("Keyboad Interrup caught, closing gracefully")
            stop_threads()
            logging.info("sys.exit()")
            sys.exit()

if __name__ == "__main__":
    main()
