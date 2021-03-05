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
#  Threaded I/O operations that update values via shared memory (g_tel_msg):
#    distance sensor, imu, internal socket messaging

import json
import sys
import socket
import time
import threading
import math  
from datetime import datetime
from time import perf_counter
from multiprocessing import Process

# Custome imports
import srauv_states
import distance_sensor
import imu_sensor
import thruster_controller
import timestamp
import command_msg
import telemetry_msg
import internal_socket_server
import logger
from srauv_settings import SETTINGS
from waypoint_parser import WAYPOINT_INFO
from external_ws_server import SrauvExternalWSS_start

###################  Globals  ###################
G_MAIN_INTERNAL_ADDR = (SETTINGS["internal_ip"], SETTINGS["main_msg_port"])
G_LOG_FILENAME = str(f'Logs/{datetime.now().strftime("SR--%m-%d-%Y_%H-%M-%S")}.log')

g_logger = logger.setup_logger("srauv", G_LOG_FILENAME, SETTINGS["log_to_stdout"])
g_tel_msg = telemetry_msg.make("srauv_main", "sim") # primary srauv data (shared mem)
g_last_topside_cmd_time_ms = timestamp.now_int_ms() # for deadman timer
g_topside_cmd = command_msg.make("dflt_src", "dflt_dest")
g_topside_cmd_num = 0
g_threads  = []
g_sub_processes = []

## pi_fly_sim, srauv will be fed telemtry data from the sim instead of using its sensor values
g_pi_fly_sim  = False # False -> thrust self, True -> send cmds to sim to fly
g_cmd_msg = command_msg.make("srauv_main", "sim") # if g_pi_fly_sim
g_tel_recv = telemetry_msg.make("dflt_src", "dflt_dest") # if fly sim, use sim data, pi decisions
g_tel_recv_num = 0

# TODO: waypoint system
waypoint_path = []
waypoint_idx = 0

# TODO: simple flight system based on target waypoint
vel_rot = 0.0
t_dist_x = 0.0
t_dist_y = 0.0
t_dist_z = 0.0
t_heading_off = 0.0

########  State  ########
def update_telemetry():
    g_tel_msg["msg_num"] += 1
    g_tel_msg["timestamp"] = timestamp.now_string()
    g_tel_msg["alt"] = g_tel_msg["dist_values"][4]
    g_logger.info(f"update_telemetry(), tel:{g_tel_msg}")

def go_to_idle():
    g_tel_msg["state"] = "idle"
    g_tel_msg["thrust_enabled"][0] = False
    g_logger.info("--- State -> IDLE ---")

def evaluate_state():
    global g_last_topside_cmd_time_ms
    if g_tel_msg["state"] == "idle":
        g_tel_msg["thrust_enabled"][0] = False

    elif g_tel_msg["state"] == "autonomous":
        g_tel_msg["thrust_enabled"][0] = True
        update_waypoint(waypoint_idx)

    elif g_tel_msg["state"] == "manual":
        if timestamp.now_int_ms() - g_last_topside_cmd_time_ms > SETTINGS["manual_deadman_timeout_ms"]:
            go_to_idle()
            g_logger.warning(f"Manual deadman triggered, going to idle, delta_ms:{g_last_topside_cmd_time_ms - timestamp.now_int_ms()}")
        else:
            g_tel_msg["thrust_enabled"][0] = True


########  waypoints / navigation  ########
def setup_waypoints(waypoint_idx):
    if len(waypoint_path) > 0:
        waypoint_idx = 0
        route = WAYPOINT_INFO["route"]
        for w in route:
            g_logger.info(f"Adding waypoint:'{route[w]}'")
            waypoint_path.append(route[w])

def update_waypoint(waypoint_idx):
    if waypoint_idx == -1:
        return

    global t_dist_x, t_dist_y, t_dist_z, t_heading_off

    #  TODO add velocity and hold duration handling
    try:
        target = WAYPOINT_INFO["targets"][waypoint_path[waypoint_idx]]
        tol = target["tolerance"]

        # update target pos so sim can update visually
        if g_pi_fly_sim == True:
            g_tel_msg["imu_dict"]["target_pos_x"] = target["pos_x"]
            g_tel_msg["imu_dict"]["target_pos_y"]  = target["pos_y"]
            g_tel_msg["imu_dict"]["target_pos_z"]  = target["pos_z"]
        
        t_dist_x = g_tel_msg["imu_dict"]["pos_x"] - target["pos_x"]
        t_dist_y = g_tel_msg["imu_dict"]["pos_y"] - target["pos_y"]
        t_dist_z = g_tel_msg["imu_dict"]["pos_z"] - target["pos_z"]
        t_heading_off = g_tel_msg["imu_dict"]["heading"] - math.degrees(math.atan2(t_dist_z, t_dist_x))
        if t_heading_off > 180.0:
            t_heading_off -= 180.0
        elif t_heading_off < 180.0:
            t_heading_off += 180.0
        
        if (abs(t_dist_x) < tol and
            abs(t_dist_y) < tol and
            abs(t_dist_z) < tol and
            abs(t_heading_off) < target["heading_tol"]):
            
            if waypoint_idx < len(waypoint_path) - 1:
                waypoint_idx += 1
                g_logger.info(f"Waypoint reached, moving to next:{waypoint_path[waypoint_idx]}")
            else:
                waypoint_idx = -1
                g_logger.info(f"Waypoint reached, no more in path. Requesting Idle")

    except Exception as e:
        g_logger.error(f"Error updating waypoints, err:{e}")
        sys.exit()

def estimate_position():
    # TODO calculate position from distance values
    # TODO update distance to target t_dist_xyz

    if g_tel_msg["imu_dict"]["heading"] >= 360:
        g_tel_msg["imu_dict"]["heading"] -= 360


########  thrust  ########
def add_thrust(val_arr, amt):
    for i in range(val_arr):
        val_arr[i] += amt[i]

def calculate_thrust():
    global g_thrust_values, t_dist_x, t_dist_y, t_dist_z, t_heading_off
    new_thrust_values = [0, 0, 0, 0, 0, 0]

    if g_tel_msg["thrust_enabled"][0] == False:
        g_thrust_values = new_thrust_values.copy()
        return

    # TODO add PID smoothing/ thrust slowing when nearing target
    thurster_config = SETTINGS["thruster_config"]
    
    if g_tel_msg["state"] == "autonomous":
        if abs(t_dist_x) > thurster_config["max_spd_min_range_m"]:
            if t_dist_x > 0:
                add_thrust(new_thrust_values, thurster_config["fwd"])
            else:
                add_thrust(new_thrust_values, thurster_config["rev"])

        if abs(t_dist_y) > thurster_config["max_spd_min_range_m"]:
            if t_dist_y > 0:
                add_thrust(new_thrust_values, thurster_config["up"])
            else:
                add_thrust(new_thrust_values, thurster_config["down"])

        if abs(t_dist_z) > thurster_config["max_spd_min_range_m"]:
            if t_dist_z > 0:
                add_thrust(new_thrust_values, thurster_config["lat_right"])
            else:
                add_thrust(new_thrust_values, thurster_config["lat_left"])

        if abs(t_heading_off) > WAYPOINT_INFO["targets"][waypoint_path[waypoint_idx]]["heading_tol"]:
            if t_heading_off > 0:
                add_thrust(new_thrust_values, thurster_config["rot_right"])
            else:
                add_thrust(new_thrust_values, thurster_config["rot_left"])

        g_thrust_values = new_thrust_values.copy()
    
    elif g_tel_msg["state"] == "manual":
        
        if g_topside_cmd["thrust_type"] == "raw_thrust":
            g_thrust_values = g_topside_cmd["raw_thrust"].copy()
            g_logger.info(f"Setting thrust_values:{g_topside_cmd['raw_thrust']}")

        elif g_topside_cmd["thrust_type"] == "dir_thrust":
            print(f"Updating manual thrust values in calculate_thrust")
            for dir in g_topside_cmd["dir_thrust"]:
                add_thrust(new_thrust_values, dir)
            g_thrust_values = new_thrust_values.copy()
            g_logger.info(f"Addied dir_thrust:{g_topside_cmd['dir_thrust']}")
            print(f"g_thrust_values:{g_thrust_values}")


########  pi_fly_sim  ########
def parse_received_telemetry():
    global g_tel_recv_num

    if g_tel_recv["msg_num"] <= g_tel_recv_num:
        return

    g_tel_recv_num = g_tel_recv["msg_num"]

    #  update srauv telemetry with incoming values, minus exceptions
    for k in g_tel_recv:
        if k == "source" or k == "dest" or k == "state":
            continue
        g_tel_msg[k] = g_tel_recv[k]

def parse_received_command():
    
    # check kill condition first for safety
    if g_topside_cmd["force_state"] == "kill":
        close_gracefully()

    global g_topside_cmd_num, g_pi_fly_sim, g_last_topside_cmd_time_ms

    #  only use new msgs/ not same msg twice
    if g_topside_cmd["msg_num"] <= g_topside_cmd_num:
        return

    g_topside_cmd_num = g_topside_cmd["msg_num"]

    if g_topside_cmd["force_state"] != "":  
        g_logger.warning(f"--- Forcing state ---> {g_topside_cmd['force_state']}")

        #  TODO: functionize state transitions
        g_tel_msg["state"] = g_topside_cmd["force_state"]
        if g_topside_cmd["force_state"] == "idle":
            go_to_idle()

        if g_topside_cmd["force_state"] == "manual":
            g_tel_msg["state"] == "manual"
            g_tel_msg["thrust_enabled"][0] = g_topside_cmd["can_thrust"]
            g_last_topside_cmd_time_ms = timestamp.now_int_ms()

        g_logger.info(f"Forcing state to {g_tel_msg['state']}, g_thrust_enabled:{g_tel_msg['thrust_enabled']}")

    if g_topside_cmd["action"] == "fly_sim_true":
        g_pi_fly_sim = True
    elif g_topside_cmd["action"] == "fly_sim_false":
        g_pi_fly_sim = False

def update_sim_cmd():
    g_cmd_msg["timestamp"] = timestamp.now_string()
    g_cmd_msg["thrust_fwd"] = g_thrust_values.copy()
    g_cmd_msg["thrust_enabled"] = g_tel_msg["thrust_enabled"][0]

    g_logger.info(f"cmd_msg:{g_cmd_msg}")
    g_cmd_msg["msg_num"] += 1


########  Process Helper Functions  ########
def start_threads():
    try:
        g_threads.append(imu_sensor.IMU_Thread(SETTINGS["imu_sensor_config"],
                                               g_tel_msg))
        g_threads.append(internal_socket_server.LocalSocketThread(G_MAIN_INTERNAL_ADDR,
                                                                  g_tel_msg,
                                                                  g_cmd_msg,
                                                                  g_tel_recv,
                                                                  g_topside_cmd))

        for idx in range(SETTINGS["thruster_config"]["num_thrusters"]):
            g_threads.append(thruster_controller.ThrusterThread(SETTINGS["thruster_config"],
                                                                g_tel_msg,
                                                                idx,
                                                                g_logger))

        for idx in range(SETTINGS["dist_sensor_config"]["main_sensors"]):
            g_threads.append(distance_sensor.DSThread(SETTINGS["dist_sensor_config"],
                                                      g_tel_msg,
                                                      idx))

        for t in g_threads:
            t.start()

        # websocket server for external comms as a sub-process
        process = Process(target=SrauvExternalWSS_start, args=())
        g_sub_processes.append(process)
        process.start()

    except Exception as e:
        g_logger.error(f"Thread creation err:{e}")

    g_logger.info(f'state:{g_tel_msg["state"]} MSG:Num threads started:{len(g_threads)}')

def close_gracefully():
    g_logger.info("Trying to stop threads...")
    try:      
         # msg socket thread to close it, its blocking on recv
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        sock.sendto(str("stop").encode("utf-8"), G_MAIN_INTERNAL_ADDR)

        for t in g_threads:
            t.kill_received = True
            t.join()

        # Terminate sub processes if any
        for p in g_sub_processes:
            p.terminate()  # sends a SIGTERM

    except socket.error as se:
        g_logger.error(f"Failed To Close Socket, err:{se}")
        sys.exit()

    except Exception as e:
        g_logger.error(f"Thread stopping err:{e}")
            
    g_logger.info("Stopped threads")
    print("Stopped threads")
    sys.exit()

###############################################################################
########                           Main                                ########
###############################################################################
def main():
    g_logger.info(f'state:{g_tel_msg["state"]} MSG:SRAUV starting')
    last_update_ms = 0
    g_tel_msg["state"] = "idle"
    setup_waypoints(waypoint_idx)

    start_threads()

    g_logger.info(f'state:{g_tel_msg["state"]} MSG:Starting update loop')
    while True:
        try:
            time_now = timestamp.now_int_ms()
            if time_now - last_update_ms >= SETTINGS["update_interval_ms"]:
                ul_perf_timer_start = perf_counter()

                parse_received_command()

                # Fly by sim fed telemetry or use sensors
                if g_pi_fly_sim:
                    parse_received_telemetry()
                    update_sim_cmd()
                else:
                    update_telemetry()

                estimate_position()

                evaluate_state()
                
                calculate_thrust()

                # update loop performance timer
                ul_perf_timer_end = perf_counter() 
                g_logger.info(f'state:{g_tel_msg["state"]} update loop ms:{(ul_perf_timer_end-ul_perf_timer_start) * 1000}')
                last_update_ms = time_now   

                # debug msgs to comfirm thread operation
                # print(f"state         : {g_tel_msg['state']}")
                # print(f"imu heading   : {g_tel_msg['imu_dict']['heading']}")
                # print(f"thrust enabled: {g_tel_msg['thrust_enabled'][0]}")
                # print(f"dist 0        : {g_tel_msg['dist_values'][0]}")
                # print(f"update loop ms: {(ul_perf_timer_end-ul_perf_timer_start) * 1000}\n")

            time.sleep(0.001)    

        except KeyboardInterrupt:
            g_logger.error("Keyboad Interrupt caught")
            close_gracefully()

        except Exception as e:
            g_logger.error(f"Exception in update loop, e:{e}")
            close_gracefully()

if __name__ == "__main__":
    main()