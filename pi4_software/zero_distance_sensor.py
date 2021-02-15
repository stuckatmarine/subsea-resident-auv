#!/usr/bin/env python
#  zero_distance_sensor.py
#  Read distance sensor value in thread and send over internal socket

import asyncio
import websockets
import sys
import json
import time
import argparse

import distance_sensor
from srauv_settings import SETTINGS

parser = argparse.ArgumentParser()

parser.add_argument('-i','--sensor_idx', help='is to use in sensor array', required=True)
parser.add_argument('-a','--addr', help='server websocket to connect to, ip:port', required=False)
args = parser.parse_args()

uri = "ws://" + str(SETTINGS["external_ip"]) + ":" + str(SETTINGS["external_port"])
config = SETTINGS["dist_sensor_config"]
send_interval = config["socket_send_interval_s"]
msg_num = 0
sensor_idx = -1
ds_threads = []
ds_data = [0.0, 0.0, 0.0, 0.0, 0.0] # for consistency, only the idx will be used and local tot his fn
obj = {}

async def send_sensor_values():

    async with websockets.connect(uri) as websocket:
        msg = json.dumps(obj)
        await websocket.send(msg)
        print(f"tx: {msg}")

        resp = await websocket.recv()
        print(f"rx: {resp}")


def close_gracefully():
    try:
        for t in ds_threads:
            t.kill_received = True
    except Exception as e:
        print(f"Thread stopping err:{e}")

    print("Calling sys.exit()")
    sys.exit()

        
def main():
    global obj, uri, sensor_idx
    print(f'Starting distance thread sensor_idx:{sensor_idx} uri:{uri}')
    obj = {
        "source" : "zero",
        "msg_num" : msg_num,
        "msg_type" : "distance",
        "timestamp" : time.strftime("%Y-%m-%d %H:%M.%S"),
        "sensor_idx" : sensor_idx,
        "sensor_value" : ds_data[sensor_idx]
    }

    try:
        ds_threads.append(distance_sensor.DSThread(config, sensor_idx, ds_data))
        for t in ds_threads:
            t.start()

        while True:
            start_time = time.time()
            obj["sensor_value"] = ds_data[sensor_idx]
            asyncio.get_event_loop().run_until_complete(send_sensor_values())
            time.sleep(send_interval - ((time.time() - start_time) % send_interval))

    except KeyboardInterrupt:
        print("Keyboad Interrup caught, closing gracefully")
        close_gracefully()

    except Exception as e:
        print(f"Exception in update loop, e:{e}")
        close_gracefully()

if __name__ == "__main__":
    if args.sensor_idx != '':
        sensor_idx = int(args.sensor_idx)
        if sensor_idx < 0 or sensor_idx >= config["num_sensors"]:
            print(f"sensor_idx not valid, sensor_idx:{sensor_idx}")
            sys.exit()

    if args.addr != None:
        uri = "ws://" + str(args.addr) # "ip:port"

    main()