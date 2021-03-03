#!/usr/bin/env python
#  distance_sensor.py
#  Threaded module to continusly read the distance sensors at a polling
#    interval and update the shared value array.

import threading
import time

import timestamp

class DSThread(threading.Thread):
    def __init__(self, config: dict, tel: dict, id: int):
        threading.Thread.__init__(self)
        self.config             = config
        self.poll_interval_ms   = config["poll_interval_ms"]
        self.value_arr          = tel["dist_values"]
        self.kill_received      = False
        self.id                 = id
        self.last_update_ms     = 0

    def read_sensor(self):
        # TODO: replace with sensor reading code
        time.sleep(0.010)
        self.value_arr[self.id] += 1
        if self.value_arr[self.id] > 300:
            self.value_arr[self.id] = 0

    def run(self):
        if self.id < 0 or self.id >= self.config["total_sensors"]:
            print(f"Sensor id not valid, id:{self.id}")
            return
        try:
            while not self.kill_received:
                time_now = timestamp.now_int_ms()
                if (time_now - self.last_update_ms >= self.poll_interval_ms):
                    self.read_sensor()
                    self.last_update_ms = time_now
                time.sleep(0.001)

        except Exception as e:
            print(f"Exception in distance sensor loop, e:{e}")
