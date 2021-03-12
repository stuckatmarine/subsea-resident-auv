#!/usr/bin/env python
#  distance_sensor.py
#  Threaded module to continusly read the distance sensors at a polling
#    interval and update the shared value array.

import threading
import time
import VL53L1X

import timestamp

class DSThread(threading.Thread):
    def __init__(self, config: dict, data_arr: list, id: int):
        threading.Thread.__init__(self)
        self.config             = config
        self.poll_interval_ms   = config["poll_interval_ms"]
        self.value_arr          = data_arr
        self.id                 = id
        self.last_update_ms     = 0
        self.kill_received      = False
        self.tof = VL53L1X.VL53L1X(i2c_bus=1, i2c_address=0x29)

    def read_sensor(self):
        # self.value_arr[self.id] += 0.02
        self.value_arr[self.id] = self.tof.get_distance()
        print(f"dist val: {self.value_arr[self.id]}")

    def run(self):
        if self.id < 0 or self.id >= self.config["total_sensors"]:
            print(f"Sensor id not valid, id:{self.id}")
            return
        try:
            self.tof.open()
            time.sleep(2)
            self.tof.start_ranging(1)

            while not self.kill_received:
                time_now = timestamp.now_int_ms()
                if (time_now - self.last_update_ms >= self.poll_interval_ms):
                    self.read_sensor()
                    self.last_update_ms = time_now
                time.sleep(0.001)

        except Exception as e:
            print(f"Exception in distance sensor loop, e:{e}")
