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
import math  
from datetime import datetime
from time import perf_counter

# Custome imports
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

###################  Globals  ###################

UPDATE_INTERVAL_MS = SETTINGS["update_interval_ms"]
TEL_TX_INTERVAL_MS = SETTINGS["tel_tx_interval_ms"]

main_internal_address = (SETTINGS["internal_ip"],SETTINGS["main_msg_port"])
source = "srauv_main"
starting_state = "idle"
can_thrust = False
fly_sim = False
threads = [] # contains all threads
dist_sensor_values = [0.0, 0.0, 0.0, 0.0, 0.0]
ds_threads = []
## imu_values = (heading, vel x, y, z, acc x, y, z, rot vel x, y, z)
imu_values = [0.0, 0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9] 
imu_threads = []
thrust_values = [0, 0, 0, 0, 0, 0] # int, -100 to 100 as percent max thrust
th_threads = []
thurster_config = SETTINGS["thruster_config"]
internal_socket_threads = []

cmd = command_msg.make(source, "sim")
tel = telemetry_msg.make(source, "dflt")
cmd_recv = command_msg.make("na", "an")
tel_recv = telemetry_msg.make("na", "an")
cmd_recv_num = -1
tel_recv_num = -1
waypoint_path = []
waypoint_idx = 0
manual_deadman_timestamp = timestamp.now_int_ms()
MANUAL_DEADMAN_TIMEOUT_MS = SETTINGS["manual_deadman_timeout_ms"]

vel_rot = 0.0
t_dist_x = 0.0
t_dist_y = 0.0
t_dist_z = 0.0
t_heading_off = 0.0

log_filename = str(f'Logs/{datetime.now().strftime("SR--%m-%d-%Y_%H-%M-%S")}.log')
logger = logger.setup_logger("srauv", log_filename, SETTINGS["log_to_stdout"])

# stay in each state for test_state_next loops, test only
test_state_count = 0
test_state_next = 20

########  Functions  ########
def close_gracefully():
    stop_threads()
    logger.info("Calling sys.exit()")
    sys.exit()


def go_to_idle():
    global can_thrust
    tel["state"] = "idle"
    can_thrust = False
    logger.info("--- State -> IDLE ---")

def over_test_count():
    global test_state_count
    test_state_count += 1

    if test_state_count > test_state_next:
        test_state_count = 0
        return True

    return False


def parse_received_telemetry():
    global tel_recv_num

    #  only use new msgs/ not same msg twice
    if tel_recv["msg_num"] <= tel_recv_num:
        return
    tel_recv_num = tel_recv["msg_num"]

    #  update srauv telemetry with incoming values, minus exceptions
    for k in tel_recv:
        if k == "source" or k == "dest" or k == "state":
            continue

        tel[k] = tel_recv[k]


def parse_received_command():
    # check kill condition first for safety
    if cmd_recv["force_state"] == "kill" or internal_socket_threads[0].cmd_with_kill_recvd == True:
        close_gracefully()

    global can_thrust, cmd_recv_num, fly_sim, manual_deadman_timestamp
    #  only use new msgs/ not same msg twice
    if cmd_recv["msg_num"] <= cmd_recv_num:
        return

    cmd_recv_num = cmd_recv["msg_num"]

    if cmd_recv["force_state"] != "":      
        #  TODO: functionize state transitions
        tel["state"] = cmd_recv["force_state"]
        if cmd_recv["force_state"] == "idle":
            go_to_idle()

        if tel["state"] == "manual":
            can_thrust = cmd_recv["can_thrust"]
            manual_deadman_timestamp = timestamp.now_int_ms()

        logger.info(f"Forcing state to {tel['state']}, can_thrust:{can_thrust}")

    if cmd_recv["action"] == "fly_sim_true":
        fly_sim = True
    elif cmd_recv["action"] == "fly_sim_false":
        fly_sim = False


def update_telemetry():
    tel["msg_num"] += 1
    tel["timestamp"] = timestamp.now_string()
    tel["fwd_dist"] = dist_sensor_values[0]
    tel["right_dist"] = dist_sensor_values[1]
    tel["rear_dist"] = dist_sensor_values[2]
    tel["left_dist"] = dist_sensor_values[3]
    tel["alt"] = dist_sensor_values[4]
    tel["depth"] = 1.1 # TODO depth sensor getter
    tel["raw_thrust"] = thrust_values
    logger.info(f"tel:{tel}")


def setup_waypoints(waypoint_idx):
    route = WAYPOINT_INFO["route"]
    for w in route:
        logger.info(f"Adding waypoint:'{route[w]}'")
        waypoint_path.append(route[w])

    if len(waypoint_path) > 0:
        waypoint_idx = 0


def update_waypoint(waypoint_idx):
    if waypoint_idx == -1:
        return

    global t_dist_x, t_dist_y, t_dist_z, t_heading_off

    #  TODO add velocity and hold duration handling
    try:
        target = WAYPOINT_INFO["targets"][waypoint_path[waypoint_idx]]
        tol = target["tolerance"]

        # update target pos so sim can update visually
        if fly_sim == True:
            tel["target_pos_x"] = target["pos_x"]
            tel["target_pos_y"]  = target["pos_y"]
            tel["target_pos_z"]  = target["pos_z"]
        
        t_dist_x = tel["pos_x"] - target["pos_x"]
        t_dist_y = tel["pos_y"] - target["pos_y"]
        t_dist_z = tel["pos_z"] - target["pos_z"]
        t_heading_off = tel["heading"] - math.degrees(math.atan2(t_dist_z, t_dist_x))
        if t_heading_off > 180.0:
            t_heading_off -= 180.0
        elif t_heading_off < 180.0:
            t_heading_off += 180.0
        
        print(f"target vector x,y,z,h:({t_dist_x}, {t_dist_y}, {t_dist_z}, {t_heading_off})")
        
        if (abs(t_dist_x) < tol and
            abs(t_dist_y) < tol and
            abs(t_dist_z) < tol and
            abs(t_heading_off) < target["heading_tol"]):
            
            if waypoint_idx < len(waypoint_path) - 1:
                waypoint_idx += 1
                logger.info(f"Waypoint reached, moving to next:{waypoint_path[waypoint_idx]}")
            else:
                waypoint_idx = -1
                logger.info(f"Waypoint reached, no more in path. Requesting Idle")

    except Exception as e:
        logger.error(f"Error updating waypoints, err:{e}")
        sys.exit()

def estimate_position():
    # TODO calculate position from distance values
    # TODO update distance to targer t_dist_xyz

    if tel["heading"] >= 360:
        tel["heading"] -= 360


def evaluate_state():
    global can_thrust
    if tel["state"] == "idle":

        # evaluate state
        can_thrust = False

        if over_test_count():
            tel["state"] = "running" 

    elif tel["state"] == "running":

        # evaluate state
        can_thrust = True
        update_waypoint(waypoint_idx)

        if over_test_count():
            go_to_idle()

    elif tel["state"] == "manual":

        # evaluate state
        can_thrust = True
        if manual_deadman_timestamp - timestamp.now_int_ms >= MANUAL_DEADMAN_TIMEOUT_MS:
            go_to_idle()
            logger.warning(f"Manual deadman triggered, going to idle, delta_ms:{manual_deadman_timestamp - timestamp.now_int_ms}")


def add_thrust(val_arr, direction):
    amt = thurster_config[direction]
    for i in range(thurster_config["num_thrusters"]):
        if i == "":
            continue

        val_arr[i] += amt[i]


def calculate_thrust(thrust_values):
    # TODO add PID smoothing/ thrust slowing when nearing target
    global t_dist_x, t_dist_y, t_dist_z, t_heading_off, can_thrust
    new_thrust_values = [0, 0, 0, 0, 0, 0]
    
    print(f"targets x,y,z,h:({t_dist_x}, {t_dist_y}, {t_dist_z}, {t_heading_off})")
    print(f"min range to thrust {thurster_config['max_spd_min_range_m']})")
    
    if tel["state"] == "running":
        if abs(t_dist_x) > thurster_config["max_spd_min_range_m"]:
            if t_dist_x > 0:
                add_thrust(new_thrust_values, "fwd")
            else:
                add_thrust(new_thrust_values, "rev")

        if abs(t_dist_y) > thurster_config["max_spd_min_range_m"]:
            if t_dist_y > 0:
                add_thrust(new_thrust_values, "up")
            else:
                add_thrust(new_thrust_values, "down")

        if abs(t_dist_z) > thurster_config["max_spd_min_range_m"]:
            if t_dist_z > 0:
                add_thrust(new_thrust_values, "lat_right")
            else:
                add_thrust(new_thrust_values, "lat_left")

        if abs(t_heading_off) > WAYPOINT_INFO["targets"][waypoint_path[waypoint_idx]]["heading_tol"]:
            if t_heading_off > 0:
                add_thrust(new_thrust_values, "rot_right")
            else:
                add_thrust(new_thrust_values, "rot_left")
    
    elif tel["state"] == "manual": # update srauv with cmd'ed values
        if cmd_recv["thrust_type"] == "raw_thrust":
            thrust_values[0] = cmd_recv["raw_thrust"][0]
            thrust_values[1] = cmd_recv["raw_thrust"][1]
            thrust_values[2] = cmd_recv["raw_thrust"][2]
            thrust_values[3] = cmd_recv["raw_thrust"][3]
            thrust_values[4] = cmd_recv["raw_thrust"][4]
            thrust_values[5] = cmd_recv["raw_thrust"][5]

        elif cmd_recv["thrust_type"] == "dir_thrust":
            add_thrust(new_thrust_values, cmd_recv["dir_thrust"][0])
            add_thrust(new_thrust_values, cmd_recv["dir_thrust"][1])
            add_thrust(new_thrust_values, cmd_recv["dir_thrust"][2])
            add_thrust(new_thrust_values, cmd_recv["dir_thrust"][3])

    for i in range(6):
        thrust_values[i] = new_thrust_values[i]
    print(f"Thrust valuse {thrust_values}")


def setup_distance_sensor_threads(ds_config, data_arr):
    logger.info(f'state:{tel["state"]} MSG:Creating distance sensor threads')

    ds_threads = []
    for id in range(ds_config["num_sensors"]):
        ds_threads.append(distance_sensor.DSThread(ds_config, id, data_arr))

    for t in ds_threads:
        t.start()
    threads.extend(ds_threads)


def setup_thruster_threads(th_config, data_arr):
    logger.info(f'state:{tel["state"]} MSG:Creating thruster threads')

    for id in range(th_config["num_thrusters"]):
        th_threads.append(thruster_controller.ThrusterThread(th_config, id, data_arr))

    for t in th_threads:
        t.start()

    threads.extend(th_threads)


def setup_socket_thread():
    logger.info(f'state:{tel["state"]} MSG:Creating srauv socket threads')

    internal_socket_threads.append(internal_socket_server.LocalSocketThread(main_internal_address, tel, cmd, tel_recv, cmd_recv))
    internal_socket_threads[0].start()
    threads.extend(internal_socket_threads)


def setup_imu_thread(imu_config, data_arr):
    logger.info(f'state:{tel["state"]} MSG:Creating srauv IMU thread')

    imu_threads.append(imu_sensor.IMU_Thread(imu_config, data_arr))
    imu_threads[0].start()
    threads.extend(imu_threads)


def update_sim_cmd():
    cmd["timestamp"] = timestamp.now_string()
    cmd["thrust_fwd"] = thrust_values[0]
    cmd["thrust_right"] = thrust_values[1]
    cmd["thrust_rear"] = thrust_values[2]
    cmd["thrust_left"] = thrust_values[3]
    cmd["thrust_v_right"] = thrust_values[4]
    cmd["thrust_v_left"] = thrust_values[5]
    cmd["can_thrust"] = can_thrust

    print(f"cmd:{cmd}")
    cmd["msg_num"] += 1

def apply_thrust():
    global can_thrust

    if fly_sim:
        update_sim_cmd()
    else:
        for t in th_threads:
            t.do_thrust(can_thrust)
    logger.info(f"enable:{can_thrust}, t[]thrust_enabled:{th_threads[0].thrust_enabled} thrust_values:{thrust_values}")


def has_live_threads(threads):
    return True in [t.is_alive() for t in threads]


def start_threads():
    try:
        setup_socket_thread()
        setup_distance_sensor_threads(SETTINGS["dist_sensor_config"], dist_sensor_values)
        setup_thruster_threads(SETTINGS["thruster_config"], thrust_values)
    except Exception as e:
        logger.error(f"Thread creation err:{e}")
    logger.info(f'state:{tel["state"]} MSG:All threads should be started. num threads:{len(threads)}')


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
            logger.error(f"Failed To Close Socket, err:{se}")
            sys.exit()

        except Exception as e:
            logger.error(f"Thread stopping err:{e}")
            
    logger.info("Stopped threads")


########  Main  ########

def main():
    logger.info(f'state:{tel["state"]} MSG:SRAUV main() starting')
    last_update_ms = 0
    tel["state"] = starting_state
    setup_waypoints(waypoint_idx)

    start_threads()

    logger.info(f'state:{tel["state"]} MSG:Starting update loop')
    while True:
        try:
            time_now = timestamp.now_int_ms()
            if time_now - last_update_ms >= UPDATE_INTERVAL_MS:
                ul_perf_timer_start = perf_counter()

                parse_received_command()

                # Fly by sim fed telemetry or use sensors
                if fly_sim:
                    parse_received_telemetry()
                else:
                    update_telemetry()

                estimate_position()

                evaluate_state()
                
                calculate_thrust(thrust_values)

                apply_thrust()

                # update loop performance timer
                ul_perf_timer_end = perf_counter() 
                logger.info(f'state:{tel["state"]} ul_perf_s:{ul_perf_timer_end-ul_perf_timer_start}')
                last_update_ms = time_now

                time.sleep(0.001)

        except KeyboardInterrupt:
            logger.info("Keyboad Interrup caught, closing gracefully")
            close_gracefully()

        except Exception as e:
            logger.info(f"Exception in update loop, e:{e}")
            close_gracefully()


if __name__ == "__main__":
    main()