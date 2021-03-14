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

def setup_waypoints(logger):
    global g_waypoint_idx
    if len(WAYPOINT_INFO["route"]) > 0:
        g_waypoint_idx = 0
        route = WAYPOINT_INFO["route"]
        for w in route:
            logger.info(f"Adding waypoint:'{route[w]}'")
            print(f"Adding waypoint {w}:'{route[w]}'")
            g_waypoints.append(route[w])

def update_waypoint(tel_msg, logger, srauv_fly_sim):
    global g_waypoint_idx
    if g_waypoint_idx == -1:
        return False

    #  TODO add velocity and hold duration handling
    try:
        target = WAYPOINT_INFO["targets"][g_waypoints[g_waypoint_idx]]
        tol = target["tolerance"]

        tel_msg["target_pos_x"] = target["pos_x"]
        tel_msg["target_pos_y"]  = target["pos_y"]
        tel_msg["target_pos_z"]  = target["pos_z"]
        
        t_dist_x = tel_msg["pos_x"] - target["pos_x"]
        t_dist_y = tel_msg["pos_y"] - target["pos_y"]
        t_dist_z = tel_msg["pos_z"] - target["pos_z"]
        t_heading_off = tel_msg["heading"] - math.degrees(math.atan2(t_dist_z, t_dist_x))
        tel_msg["target_heading_to"]  = t_heading_off
        if t_heading_off > 180.0:
            t_heading_off -= 180.0
        elif t_heading_off < 180.0:
            t_heading_off += 180.0
        
        # if waypoint reached go to next
        if (abs(t_dist_x) < tol and
            abs(t_dist_y) < tol and
            abs(t_dist_z) < tol):
            # abs(t_heading_off) < target["heading_tol"]):
            if g_waypoint_idx < len(g_waypoints) - 1:
                g_waypoint_idx += 1
                logger.info(f"Waypoint reached, moving to next:{g_waypoints[g_waypoint_idx]}")
                print(f"Waypoint reached, moving to next:{g_waypoints[g_waypoint_idx]}")
            else:
                g_waypoint_idx = -1
                logger.info(f"Waypoint reached, no more in path. Requesting Idle")
                print(f"Waypoint reached, no more in path. Requesting Idle")
                return False

    except Exception as e:
        logger.error(f"Error updating waypoints, err:{e}")
        sys.exit()

def estimate_position(tel_msg: dict):
    # TODO calculate position from distance values
    # TODO update distance to target t_dist_xyz

    if tel_msg["imu_dict"]["heading"] >= 360:
        tel_msg["imu_dict"]["heading"] -= 360
