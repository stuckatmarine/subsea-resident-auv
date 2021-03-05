#!/usr/bin/env python
#  imu_sensor.py
#  Threaded module to continusly read the imu sensor at a polling
#    interval and update the shared value array.

import threading
import time
import copy

# Import Adafruit stuff
import board
import busio
import adafruit_bno055

i2c = busio.I2C(board.SCL, board.SDA)
sensor = adafruit_bno055.BNO055_I2C(i2c, 0x29)

import timestamp

class IMU_Thread(threading.Thread):
    def __init__(self, config:dict, tel:dict):
        threading.Thread.__init__(self)
        self.config             = config
        self.poll_interval_s    = config["poll_interval_s"]
        self.values             = tel["imu_dict"]
        self.kill_received      = False
        print(f"IMU thread up")

    # update with sensor reading code
    def read_sensor(self):
        time.sleep(0.010) # TODO: replace with actual sensor read code

        #print(sensor.euler)

        ## see srauv_settings.json for "imu_values" that map to self.values
        self.values["heading"] = sensor.euler[0]
        self.values["vel_x"] = 0.01

    def run(self):
        while not self.kill_received:
            try:
                start_time = time.time()
                self.read_sensor()
                time.sleep(self.poll_interval_s -
                    ((time.time() - start_time) % self.poll_interval_s))
                    
            except Exception as e:
                print(f"IMU err:{e}")
    