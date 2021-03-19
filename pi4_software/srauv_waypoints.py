#!/usr/bin/env python
#   srauv_navigation.py
#   Parse waypoints info and route from waypoint_info.json
#   
#   Update next waypoint based on target criteria
import sys
import math
import logger

from waypoint_parser import WAYPOINT_INFO

g_waypoint_idx = -1
g_waypoints = []

g_t_dist_x = 0.0
g_t_dist_y = 0.0
g_t_dist_z = 0.0
g_t_heading = 0.0

def setup_waypoints(tel_msg, logger):
    global g_waypoint_idx, g_waypoints, g_t_dist_x, g_t_dist_y, g_t_dist_z, g_t_heading
    if len(WAYPOINT_INFO["route"]) > 0:
        g_waypoint_idx = 0
        g_waypoints = WAYPOINT_INFO["route"]
        tel_msg["mission_msg"] = f"Target -> {g_waypoints[g_waypoint_idx]} \n" + tel_msg["mission_msg"]
        target = WAYPOINT_INFO["targets"][g_waypoints[g_waypoint_idx]]

        tel_msg["target_pos_x"] = target["pos_x"]
        tel_msg["target_pos_y"]  = target["pos_y"]
        tel_msg["target_pos_z"]  = target["pos_z"]
        
        g_t_dist_x = tel_msg["pos_x"] - target["pos_x"]
        g_t_dist_y = tel_msg["pos_y"] - target["pos_y"]
        g_t_dist_z = tel_msg["pos_z"] - target["pos_z"]

        g_t_heading_off = tel_msg["heading"] - math.degrees(math.atan2(g_t_dist_z, g_t_dist_x))
        if g_t_heading_off > 180.0:
            g_t_heading_off -= 180.0
        elif g_t_heading_off < 180.0:
            g_t_heading_off += 180.0
        tel_msg["target_heading_to"]  = g_t_heading_off

def update_waypoint(tel_msg, logger, srauv_fly_sim):
    global g_waypoint_idx, g_waypoints, g_t_dist_x, g_t_dist_y, g_t_dist_z, g_t_heading
    if g_waypoint_idx == -1:
        print("if g_waypoint_idx == -1:")
        return False

    #  TODO add velocity and hold duration handling
    try:
        target = WAYPOINT_INFO["targets"][g_waypoints[g_waypoint_idx]]
        tol = target["tolerance"]

        tel_msg["target_pos_x"] = target["pos_x"]
        tel_msg["target_pos_y"]  = target["pos_y"]
        tel_msg["target_pos_z"]  = target["pos_z"]
        
        g_t_dist_x = tel_msg["pos_x"] - target["pos_x"]
        g_t_dist_y = tel_msg["pos_y"] - target["pos_y"]
        g_t_dist_z = tel_msg["pos_z"] - target["pos_z"]

        g_t_heading_off = tel_msg["heading"] - math.degrees(math.atan2(g_t_dist_z, g_t_dist_x))
        if g_t_heading_off > 180.0:
            g_t_heading_off -= 180.0
        elif g_t_heading_off < 180.0:
            g_t_heading_off += 180.0
        tel_msg["target_heading_to"]  = g_t_heading_off
        # print(f"target heading to {g_t_heading_off}")

        # if waypoint reached go to next
        if (abs(g_t_dist_x) < tol and
            abs(g_t_dist_y) < tol and
            abs(g_t_dist_z) < tol):
            # abs(t_heading_off) < target["heading_tol"]):
            if g_waypoint_idx < len(g_waypoints) - 1:
                g_waypoint_idx += 1
                logger.info(f"Waypoint reached, moving to next:{g_waypoints[g_waypoint_idx]}")
                print(f"Waypoint reached, moving to next:{g_waypoints[g_waypoint_idx]}")
                tel_msg["mission_msg"] = f"Target -> {g_waypoints[g_waypoint_idx]} \n" + tel_msg["mission_msg"]
            else:
                g_waypoint_idx = -1
                logger.info(f"Waypoint reached, no more in path. Requesting Idle")
                print(f"Waypoint reached, no more in path. Requesting Idle")
                return False
        # else:
        #     print(f'tel  xyz {tel_msg["pos_x"]} {tel_msg["pos_y"]} {tel_msg["pos_z"]}')
        #     print(f'tar  xyz {target["pos_x"]} {target["pos_y"]} {target["pos_z"]}')
        #     print(f"dist xyz {g_t_dist_x} {g_t_dist_y} {g_t_dist_z}")
        return True

    except Exception as e:
        logger.error(f"Error updating waypoints, err:{e}")
        sys.exit()

def estimate_position(tel_msg: dict):
    # TODO calculate position from distance values
    # TODO update distance to target t_dist_xyz

    if tel_msg["imu_dict"]["heading"] >= 360:
        tel_msg["imu_dict"]["heading"] -= 360

def get_target_waypoint_idx():
    return g_waypoint_idx

def reset_to_first_waypoint():
    global g_waypoint_idx, g_waypoints
    if len(WAYPOINT_INFO["route"]) > 0:
        g_waypoint_idx = 0
