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
from timeloop import Timeloop

from inputs import get_gamepad
from SRAUV_settings import SETTINGS
import Timestamp
import CommandMsg
import TelemetryMsg


###################  Globals Variables  ###################
UPDATE_INTERVAL_MS = SETTINGS["tel_tx_interval_ms"] # update loop timer 
srauv_address = (SETTINGS["srauv_ip"], SETTINGS["srauv_port"])
topside_address = (SETTINGS["topside_ip"], SETTINGS["topside_port"])
last_update_ms = 0
source = "vehicle"
state = "idle"
txCmds = True
hasCmd = False
supress_img = True
sim_echo = False

cmd = CommandMsg.make(source, "sim")
tel = TelemetryMsg.make(source, "sim")


###################  Global Setup ###################
tl = Timeloop()

# logging.basicConfig(filename='SRAUV.log', filemode='w', format='%(name)s - %(levelname)s - %(message)s')
log_filename = str(f"Logs/{str(time.time())}.log")
logging.basicConfig(filename=log_filename, filemode='w')




###################  Update Loop  ###################
def send_telemetry():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # (internet, UDP)

    try:
        sock.sendto(str.encode(json.dumps(tel)), ("localhost", 7001))
        tel["msgNum"] += 1

    finally:
        sock.close()

@tl.job(interval=timedelta(seconds=UPDATE_INTERVAL_MS/1000), data=tel)
def update_loop(tel):
    delta_us_start = datetime.utcnow().microsecond

    if tel["state"] == "idle":
        tel["state"] = "running"

    elif tel["state"] == "running":
        tel["state"] = "running"
        # read sensore
        # read inputs
        try:
            print("checking inputs")
        except KeyboardInterrupt:
            print("Exiting via interrupt")
            sys.exit()
        # get nav data
        # do thrust

    send_telemetry()

    update_loop_delta_us = datetime.utcnow().microsecond - delta_us_start
    if update_loop_delta_us < 0:
        update_loop_delta_us += 1000000
    print(f'timestamp:{Timestamp.make()} state:{tel["state"]}, update_loop_delta_us:{update_loop_delta_us}')

###################  Socket Server  ###################
# print recvMsg, strip image data if required
def print_msg_dict(msg, replace_image):
    if replace_image == True and 'imgStr' in msg.keys():
        msg["imgStr"] = "replaced str"

    print(f"< {msg}")

# send message to SIM over ws
def sim_forward(recvMsg):
    self.sendMessage(self.data)
    print(f"> forwarded msg to SIM, {recvMsg['source']} -> {recvMsg['dest']}")

# server respond to SIM with placeholder msgs
def send_sim_test_msgs():
    cmd["timestamp"] = Timestamp.make()
    resp = json.dumps(cmd)
    self.sendMessage(resp)
    # print(f"> {resp}")

    tel["timestamp"] = Timestamp.make()
    resp = json.dumps(tel)
    self.sendMessage(resp)
    # print(f"> {resp}")

# act on gamepad input
def gamepad_actions(recvMsg):
    if 'buttons' in recvMsg.keys():
        print(f"gamepad buttons pressed: {recvMsg['buttons']}")
        # logic to do actions based on which buttons are used
    else:
        print(f"no gamepad buttons pressed")
        # logic to null cmd

def handleMessage(data):
    recvMsg = json.loads(data)
    print_msg_dict(recvMsg, supress_img)

    if recvMsg["source"] == "sim" and sim_echo == True:
        send_sim_test_msgs()

    elif recvMsg["source"] == "gamepad":
        gamepad_actions(recvMsg)

    elif recvMsg["source"] == "vehicle":
        if recvMsg["dest"] == "sim":
            sim_forward(recvMsg)

def sock_server():
    # Create a UDP socket
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    # Bind the socket to the port
    logging.info(f"starting server on {srauv_address}")
    sock.bind(srauv_address)
    print('SRAUV socket server up, timestamp:%s state:%s', Timestamp.make(), tel["state"])

    while True:
        try:
            data, addr = sock.recvfrom(1024) # buffer size is 1024 bytes
            handleMessage(data)
            time.sleep(.1)
        except KeyboardInterrupt:
            print("Exiting via interrupt")
            sys.exit()

if __name__ == "__main__":
    tl.start(block=False)
    # sock_server()
