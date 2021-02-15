#!/usr/bin/env python
#   waypoint_parser.py
#   Parse json file with waypoint info.

import json

filename = "waypoint_info.json"
WAYPOINT_INFO = {}

try:
    with open(filename) as file: 
        data = json.load(file)
        WAYPOINT_INFO = data
except Exception as ex:
    print(f"Error loading WAYPOINT_INFO from {filename}, err:" + str(ex))