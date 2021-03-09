#!/usr/bin/env python
#  imu_sensor.py
#  Threaded module to continusly read the imu sensor at a polling
#    interval and update the shared value array.

import threading
import time
import copy


import timestamp
from srauv_settings import SETTINGS

if SETTINGS["hardware"]["i2c"] == True:
    import board
    import busio
    import adafruit_bno055
    i2c = busio.I2C(board.SCL, board.SDA)
    sensor = adafruit_bno055.BNO055_I2C(i2c, 0x29)
    # sensor.setMode(OPERATION_MODE_IMUPLUS)

class IMU_Thread(threading.Thread):
    def __init__(self, config:dict, tel:dict):
        threading.Thread.__init__(self)
        self.config             = config
        self.poll_interval_s    = config["poll_interval_s"]
        self.values             = tel["imu_dict"]
        self.kill_received      = False

    def read_sensor(self):
        ## see telemetry_msg.py for "imu_values" that map to self.values
        self.values["heading"] = sensor.euler[0] # deg
        self.values["roll"] = sensor.euler[1] # deg
        self.values["pitch"] = sensor.euler[2] # deg
        self.values["gyro_x"] = sensor.gyroscope[0] # deg / s
        self.values["gyro_y"] = sensor.gyroscope[1]
        self.values["gyro_z"] = sensor.gyroscope[2]
        self.values["vel_x"] = (sensor.linearaccel[0] - self.values["linear_accel_x"]) / self.poll_interval_s
        self.values["vel_y"] = (sensor.linearaccel[1] - self.values["linear_accel_y"]) / self.poll_interval_s
        self.values["vel_z"] = (sensor.linearaccel[2] - self.values["linear_accel_z"]) / self.poll_interval_s
        self.values["linear_accel_x"] = sensor.linearaccel[0] # m / s^2 # ignores gravity
        self.values["linear_accel_y"] = sensor.linearaccel[1]
        self.values["linear_accel_z"] = sensor.linearaccel[2]
        self.values["accel_x"] = sensor.accelerometer[0] # m / s^2 # + gravity
        self.values["accel_y"] = sensor.accelerometer[1]
        self.values["accel_z"] = sensor.accelerometer[2]
        self.values["magnetic_x"] = sensor.magnetometer[0] # uTeslas
        self.values["magnetic_y"] = sensor.magnetometer[1]
        self.values["magnetic_z"] = sensor.magnetometer[2]

    def run(self):
        if SETTINGS["hardware"]["i2c"] == True:
            print(f"IMU thread up, fully calibrated:{sensor.isFullyCalibrated()}")
            while not self.kill_received:
                try:
                    start_time = time.time()
                    self.read_sensor()
                    time.sleep(self.poll_interval_s -
                        ((time.time() - start_time) % self.poll_interval_s))
                        
                except Exception as e:
                    print(f"IMU err:{e}")

                time.sleep(0.001)
        else:
            print(f"I2C not enabled in srauv_settings.json")
            
    