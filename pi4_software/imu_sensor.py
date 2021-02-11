#!/usr/bin/env python
#  imu_sensor.py
#  Threaded module to continusly read the imu sensor at a polling
#    interval and update the shared value array.

import threading
import time

# remove these test vals when actual sensors available
TEST_TOF_DURATION_S = 0.010
TEST_MAX = 300

class IMU_Thread(threading.Thread):
    def __init__(self, config, data_arr):
        threading.Thread.__init__(self)
        self.config = config
        self.value_arr = data_arr
        self.kill_received = False
        self.poll_interval = config["poll_interval_s"]

    # update with sensor reading code
    def read_sensor(self):
        time.sleep(TEST_TOF_DURATION_S)
        self.value_arr[0] = 0.0 # heading
        self.value_arr[1] = 0.1 # x vel
        self.value_arr[2] = 0.2 # y vel
        self.value_arr[3] = 0.3 # z vel
        self.value_arr[4] = 0.4 # x acc
        self.value_arr[5] = 0.5 # y acc
        self.value_arr[6] = 0.6 # z acc
        self.value_arr[7] = 0.7 # x rot vel
        self.value_arr[8] = 0.8 # y rot vel
        self.value_arr[9] = 0.9 # z rot vel

    def run(self):
        while not self.kill_received:
            start_time = time.time()
            self.read_sensor()
            time.sleep(self.poll_interval - ((time.time() - start_time) % self.poll_interval))
    