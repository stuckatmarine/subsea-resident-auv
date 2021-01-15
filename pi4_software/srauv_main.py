#!/usr/bin/env python
#  srauv_main.py
#  SRAUV main control program. Has an pdate loop that operates at a
#    deterministic rate (target 60 hz, min 10 hz), to make decisions
#    based on current vehicle state and sensor values and apply the
#    appropriate thrust until the next update cycle.
#
#  Update Loop:
#    update_telemetry_values()
#    estimate_position()
#    evaluate_state()
#    calculate_thrust()
#    log_state()
#    apply_thrust()
#      
#  Threaded I/O operations that update values via shared memory:
#    distance sensor, imu, internal socket messaging

import json
import sys
import socket
import time
import threading
import multiprocessing
from datetime import datetime
from time import perf_counter

# Custome imports
import distance_sensor
import thruster_controller
import timestamp
import command_msg
import telemetry_msg
import internal_socket_server
import logger
from srauv_settings import SETTINGS

###################  Globals  ###################

UPDATE_INTERVAL_MS = SETTINGS["update_interval_ms"]
TEL_TX_INTERVAL_MS = SETTINGS["tel_tx_interval_ms"]
main_internal_address = (SETTINGS["internal_ip"],SETTINGS["main_msg_port"])
source = "srauv_main"
starting_state = "idle"
can_thrust = False
threads = [] # contains all threads
dist_sensor_values = [0.0, 0.0, 0.0, 0.0, 0.0]
ds_threads = []
thrust_values = [0, 0, 0, 0, 0, 0] # int, -100 to 100 as percent max thrust
th_threads = []
internal_socket_threads = []

cmd_msg = command_msg.make(source, "sim")
tel_msg = telemetry_msg.make(source, "sim")

log_filename = str(f'Logs/{datetime.now().strftime("SR--%m-%d-%Y_%H-%M-%S")}.log')
logger = logger.setup_logger("srauv", log_filename)

# stay in each state for test_state_next loops, test only
test_state_count = 0
test_state_next = 20

########  Functions  ########

def over_test_count():
    global test_state_count
    test_state_count += 1

    if test_state_count > test_state_next:
        test_state_count = 0
        return True

    return False


def update_telemetry():
    tel_msg["fwdDist"] = dist_sensor_values[0]
    tel_msg["rightDist"] = dist_sensor_values[0]
    tel_msg["rearDist"] = dist_sensor_values[0]
    tel_msg["leftDist"] = dist_sensor_values[0]
    tel_msg["alt"] = dist_sensor_values[0]
    logger.info(f"tel:{tel_msg}")


def evaluate_state():
    if tel_msg["state"] == "idle":

        # evaluate state
        can_thrust = False

        if over_test_count():
            tel_msg["state"] = "running" 

    elif tel_msg["state"] == "running":

        # evaluate state
        can_thrust = True

        if over_test_count():
            tel_msg["state"] = "idle"


def setup_distance_sensor_threads(ds_config, data_arr):
    logger.info(f'state:{tel_msg["state"]} MSG:Creating distance sensor threads')

    ds_threads = []
    for id in range(ds_config["num_sensors"]):
        ds_threads.append(distance_sensor.DSThread(ds_config, id, data_arr))

    for t in ds_threads:
        t.start()
    threads.extend(ds_threads)


def setup_thruster_threads(th_config, data_arr):
    logger.info(f'state:{tel_msg["state"]} MSG:Creating thruster threads')

    for id in range(th_config["num_thrusters"]):
        th_threads.append(thruster_controller.ThrusterThread(th_config, id, data_arr))

    for t in th_threads:
        t.start()

    threads.extend(th_threads)


def setup_socket_thread():
    logger.info(f'state:{tel_msg["state"]} MSG:Creating srauv socket threads')

    internal_socket_threads.append(internal_socket_server.LocalSocketThread(main_internal_address, tel_msg, cmd_msg))
    internal_socket_threads[0].start()
    threads.extend(internal_socket_threads)


def apply_thrust(enable):
    logger.info(f"thrust_enabled:{th_threads[0].thrust_enabled} thrust_values:{thrust_values}")
    for t in th_threads:
        t.do_thrust(enable)


def has_live_threads(threads):
    return True in [t.is_alive() for t in threads]


def start_threads():
    try:
        setup_socket_thread()
        setup_distance_sensor_threads(SETTINGS["dist_sensor_config"], dist_sensor_values)
        setup_thruster_threads(SETTINGS["thruster_config"], thrust_values)
    except Exception as e:
        logger.error(f"Thread creation err:{e}")
    logger.info(f'state:{tel_msg["state"]} MSG:All threads should be started. num threads:{len(threads)}')


def stop_threads():
    logger.info("Trying to stop threads...")
    while has_live_threads(threads):
        try:
            for t in threads:
                t.kill_received = True  
            
            # Terminate multi processes if any
            # for p in ext_proc:
            #     p.terminate()  # sends a SIGTERM

            # msg sock thread to close it
            sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            sock.sendto(str("stop").encode("utf-8"), main_internal_address)

        except socket.error as se:
            print(f"Failed To Close Socket, err:{se}")
            sys.exit()

        except Exception as e:
            logger.error(f"Thread stopping err:{e}")
            
    logger.info("Stopped threads")


########  Main  ########

def main():
    logger.info(f'state:{tel_msg["state"]} MSG:SRAUV main() starting')
    last_update_ms = 0
    tel_msg["state"] = starting_state

    start_threads()

    logger.info(f'state:{tel_msg["state"]} MSG:Starting update loop')
    while True:
        try:
            time_now = int(round(time.time() * 1000))
            if time_now - last_update_ms >= UPDATE_INTERVAL_MS:
                ul_perf_timer_start = perf_counter()

                update_telemetry()

                # estimate_position()

                evaluate_state()
                
                # calculate_thrust()

                # log_state()

                apply_thrust(can_thrust)

                # update loop performance timer
                ul_perf_timer_end = perf_counter() 
                logger.info(f'state:{tel_msg["state"]} ul_perf_s:{ul_perf_timer_end-ul_perf_timer_start}')
                last_update_ms = time_now

            time.sleep(0.001)

        except KeyboardInterrupt:
            logger.info("Keyboad Interrup caught, closing gracefully")
            stop_threads()
            logger.info("Calling sys.exit()")
            sys.exit()

if __name__ == "__main__":
    main()