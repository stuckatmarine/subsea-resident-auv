#!/usr/bin/env python
#  zero_distance_sensor.py
#  Read distance sensor value in thread and send over internal socket
#  Usage: ./zero_distance_sensor.py -i 0 -a x.x.x.x:port

import asyncio
import websockets
import sys
import json
import time
import argparse
import VL53L1X

import timestamp
import distance_sensor
from srauv_settings import SETTINGS

parser = argparse.ArgumentParser()
parser.add_argument('-i','--sensor_idx', help='is to use in sensor array', required=True)
parser.add_argument('-a','--addr', help='server websocket to connect to, ip:port', required=False)
args = parser.parse_args()

tof = VL53L1X.VL53L1X(i2c_bus=1, i2c_address=0x29)
tof.open()

obj = {
    "source" : "zero",
    "msg_num" : msg_num,
    "msg_type" : "distance",
    "timestamp" : time.strftime("%Y-%m-%d %H:%M.%S"),
    "sensor_idx" : sensor_idx,
    "sensor_value" : ds_data[sensor_idx]
}

try:
    tof.start_ranging(1)

    while True:
        distance = tof.get_distance()
        print(distance)

        start_time = time.time()
        if (start_time - )
            obj["sensor_value"] = distance
            time.sleep(send_interval - ((time.time() - start_time) % send_interval))
        time.sleep(0.001)


except KeyboardInterrupt:
    tof.stop_ranging
    
except Exception as e:
    tof.stop_ranging
    print(f"dist sensor error: {e}")