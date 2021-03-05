#!/usr/bin/env python
#   srauv_navigation.py
#   Parse waypoints info and route from waypoint_info.json
#   
#   Update next waypoint based on target criteria
import sys
import math

from waypoint_parser import WAYPOINT_INFO

g_waypoint_idx = -1
g_waypoints = []

def setup_waypoints(logger: classmethod):
    global g_waypoint_idx
    if len(g_waypoints) > 0:
        g_waypoint_idx = 0
        route = WAYPOINT_INFO["route"]
        for w in route:
            logger.info(f"Adding waypoint:'{route[w]}'")
            print(f"Adding waypoint:'{route[w]}'")
            g_waypoints.append(route[w])

def update_waypoint(tel_msg: dict, logger: classmethod, srauv_fly_sim: bool):
    global g_waypoint_idx
    if g_waypoint_idx == -1:
        return

    global t_dist_x, t_dist_y, t_dist_z, t_heading_off

    #  TODO add velocity and hold duration handling
    try:
        target = WAYPOINT_INFO["targets"][g_waypoints[g_waypoint_idx]]
        tol = target["tolerance"]

        # update target pos so sim can update visually
        if srauv_fly_sim == True:
            tel_msg["imu_dict"]["target_pos_x"] = target["pos_x"]
            tel_msg["imu_dict"]["target_pos_y"]  = target["pos_y"]
            tel_msg["imu_dict"]["target_pos_z"]  = target["pos_z"]
        
        t_dist_x = tel_msg["imu_dict"]["pos_x"] - target["pos_x"]
        t_dist_y = tel_msg["imu_dict"]["pos_y"] - target["pos_y"]
        t_dist_z = tel_msg["imu_dict"]["pos_z"] - target["pos_z"]
        t_heading_off = tel_msg["imu_dict"]["heading"] - math.degrees(math.atan2(t_dist_z, t_dist_x))
        if t_heading_off > 180.0:
            t_heading_off -= 180.0
        elif t_heading_off < 180.0:
            t_heading_off += 180.0
        
        if (abs(t_dist_x) < tol and
            abs(t_dist_y) < tol and
            abs(t_dist_z) < tol and
            abs(t_heading_off) < target["heading_tol"]):
            
            if g_waypoint_idx < len(g_waypoints) - 1:
                g_waypoint_idx += 1
                logger.info(f"Waypoint reached, moving to next:{g_waypoints[g_waypoint_idx]}")
            else:
                g_waypoint_idx = -1
                logger.info(f"Waypoint reached, no more in path. Requesting Idle")

    except Exception as e:
        logger.error(f"Error updating waypoints, err:{e}")
        sys.exit()

def estimate_position(tel_msg: dict):
    # TODO calculate position from distance values
    # TODO update distance to target t_dist_xyz

    if tel_msg["imu_dict"]["heading"] >= 360:
        tel_msg["imu_dict"]["heading"] -= 360
