#!/usr/bin/env python
#  imu_sensor.py
#  Threaded module to continusly read the imu sensor at a polling
#    interval and update the shared value array.

import threading
import time

# remove these test vals when actual sensors available
TEST_TOF_DURATION_S = 0.010

class IMU_Thread(threading.Thread):
    def __init__(self, config, data_arr):
        threading.Thread.__init__(self)
        self.config = config
        self.values = data_arr
        self.kill_received = False
        self.poll_interval = config["poll_interval_s"]
        print(f"IMU thread up")

    # update with sensor reading code
    def read_sensor(self):
        time.sleep(TEST_TOF_DURATION_S)

        ## see srauv_settings.json for "imu_values" that map to self.values
        #  u can add as needed
        self.values["heading"] += 0.02
        self.values["pos_x"] += 0.01
        self.values["pos_y"] = 0.01
        self.values["pos_z"] = 0.01
        self.values["vel_x"] = 0.01

        # print(f"IMU test val heading:{self.values['heading']}")

    def run(self):
        while not self.kill_received:
            start_time = time.time()
            self.read_sensor()
            time.sleep(self.poll_interval - ((time.time() - start_time) % self.poll_interval))
    