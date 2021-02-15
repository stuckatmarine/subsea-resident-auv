#!/usr/bin/env python
#  distance_sensor.py
#  Threaded module to continusly read the distance sensors at a polling
#    interval and update the shared value array.

import threading
import time


# remove these test vals when actual sensors available
TEST_TOF_DURATION_S = 0.010
TEST_MAX = 300

class DSThread(threading.Thread):
    def __init__(self, config, id, data_arr):
        threading.Thread.__init__(self)
        self.config = config
        self.id = id
        self.value_arr = data_arr
        self.kill_received = False
        self.poll_interval = config["poll_interval_s"]

    # update with sensor reading code
    def read_sensor(self):
        time.sleep(TEST_TOF_DURATION_S)

        # fake sensor tests, cycels to man and repeats
        self.value_arr[self.id] += 1
        if self.value_arr[self.id] > TEST_MAX:
            self.value_arr[self.id] = 0

    def run(self):
        if self.id < 0 or self.id >= self.config["num_sensors"]:
            print(f"Sensor id not valid, id:{self.id}")
            return
        try:
            while not self.kill_received:
                start_time = time.time()
                self.read_sensor()
                time.sleep(self.poll_interval - ((time.time() - start_time) % self.poll_interval))
       
        except KeyboardInterrupt:
            print("Keyboad Interrup caught, closing gracefully")
            return

        except Exception as e:
            print(f"Exception in update loop, e:{e}")
            return