#!/usr/bin/env python
#   srauv_settings.py
#   Parse json file with SRAUV config and settings.

import json
import os

filename = "srauv_settings.json"
SETTINGS = {}

try:
    with open(filename) as file: 
        data = json.load(file)
        SETTINGS = data
except Exception as ex:
    print(f"Error loading SETTINGS from {filename}, err:" + str(ex))