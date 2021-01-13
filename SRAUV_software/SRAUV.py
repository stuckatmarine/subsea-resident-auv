#!/usr/bin/env python
# SRAUV.py

import json
import sys
import socket
import time
import logging
import threading
from datetime import datetime
from time import perf_counter

from SRAUV_settings import SETTINGS
import DistanceSensor
import ThrusterController
import Timestamp
import CommandMsg
import TelemetryMsg

###################  Globals  ###################
UPDATE_INTERVAL_MS = SETTINGS["update_interval_ms"]
TEL_TX_INTERVAL_MS = SETTINGS["tel_tx_interval_ms"]
srauv_address = (SETTINGS["srauv_ip"], SETTINGS["srauv_port"])
source = "vehicle"
starting_state = "idle"
threads = []
dist_sensor_values = [0.0, 0.0, 0.0, 0.0, 0.0]
ds_threads = []
thrust_values = [0, 0, 0, 0, 0, 0] # int, -100 to 100 as percent max thrust
th_threads = []

cmd = CommandMsg.make(source, "sim")
tel = TelemetryMsg.make(source, "sim")

log_filename = str(f'Logs/{datetime.now().strftime("%m-%d-%Y_%H-%M-%S")}.log')
logging.basicConfig(
                    filename=log_filename,
                    filemode='w',
                    format='%(asctime)s - %(levelname)s - %(message)s',
                    level=logging.INFO) 
logging.getLogger().addHandler(logging.StreamHandler(sys.stdout))

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
    tel["fwdDist"] = dist_sensor_values[0]
    tel["rightDist"] = dist_sensor_values[0]
    tel["rearDist"] = dist_sensor_values[0]
    tel["leftDist"] = dist_sensor_values[0]
    tel["alt"] = dist_sensor_values[0]


def send_telemetry():
    try:
        sock.sendto(str.encode(json.dumps(tel)), srauv_address)
        tel["msgNum"] += 1

        data, server = sock.recvfrom(4096)
        data = data.decode("utf-8")

        if data != '':
            print(f"recv {data} from {server}")

    except socket.error:
        logging.warning("Failed to send over socket, srauv_address:%s", srauv_address)


def setup_distance_threads(ds_config, data_arr):
    logging.info(f'state:{tel["state"]} MSG:Creating distance sensor threads')

    ds_threads = []
    for id in range(ds_config["num_sensors"]):
        ds_threads.append(DistanceSensor.ds_thread(ds_config, id, data_arr))

    for t in ds_threads:
        t.start()

    threads.extend(ds_threads)


def setup_esc_threads(th_config, data_arr):
    logging.info(f'state:{tel["state"]} MSG:Creating thruster threads')

    for id in range(th_config["num_thrusters"]):
        th_threads.append(ThrusterController.thruster_thread(th_config, id, data_arr))

    for t in th_threads:
        t.start()

    threads.extend(th_threads)


def apply_thrust(enable):
    for t in th_threads:
        t.do_thrust(enable)

def has_live_threads(threads):
    return True in [t.is_alive() for t in threads]


def start_threads():
    try:
        setup_distance_threads(SETTINGS["dist_sensor_config"], dist_sensor_values)
        setup_esc_threads(SETTINGS["esc_config"], thrust_values)
    except Exception as e:
        logging.error(f"Thread creation err:{e}")
    logging.info(f'state:{tel["state"]} MSG:All threads should be started. num threads:{len(threads)}')


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
    tel["state"] = starting_state
    logging.info(f'state:{tel["state"]} MSG:SRAUV main starting')

    start_threads()

    #  state change test
    test_counter = 0
    test_running_cap = 20

    while True:
        try:
            # update system
            time_now = int(round(time.time() * 1000))
            if time_now - last_update_ms >= UPDATE_INTERVAL_MS:
                ul_perf_timer_start = perf_counter() 

                # read sensore
                # calc nav data

                if tel["state"] == "idle": 
                    apply_thrust(False)
                    if test_counter < test_running_cap: # stat change tesing if
                        tel["state"] = "running" 

                elif tel["state"] == "running":
                    tel["state"] = "running"
                    # read inputs
                    apply_thrust(True)
                    

                    # run for test_running_cap loops before going to idle
                    test_counter += 1
                    if test_counter > test_running_cap:
                        tel["state"] = "idle"

                logging.info(f"dist_sensor_values:{dist_sensor_values}")
                last_update_ms = int(round(time.time() * 1000))
            
                # tel tx, needs own thread
                if time_now - last_tel_tx_ms >= TEL_TX_INTERVAL_MS:
                    update_telemetry()
                    send_telemetry()
                    last_tel_tx_ms = int(round(time.time() * 1000))
                

                # ul perf timer
                ul_perf_timer_end = perf_counter() 
                logging.info(f'state:{tel["state"]} ul_perf_s:{ul_perf_timer_end-ul_perf_timer_start}')

            time.sleep(0.001)

        except KeyboardInterrupt:
            logging.info("Keyboad Interrup caught, closing gracefully")
            stop_threads()
            logging.info("Calling sys.exit()")
            sys.exit()

if __name__ == "__main__":
    main()