#!/usr/bin/env python
#  imu_sensor.py
#  Threaded module to continusly read the imu sensor at a polling
#    interval and update the shared value array.

import threading
import time
import copy
import ms5837

import timestamp
from srauv_settings import SETTINGS

if SETTINGS["hardware"]["i2c"] == True:
    import board
    import busio
    import adafruit_bno055
    i2c = busio.I2C(board.SCL, board.SDA)
    sensor = ms5837.MS5837_02BA() # Default I2C bus is 1 (Raspberry Pi 3)

class IMU_Thread(threading.Thread):
    def __init__(self, config:dict, tel:dict):
        threading.Thread.__init__(self)
        self.config             = config
        self.poll_interval_s    = config["poll_interval_s"]
        self.values             = tel["depth"]
        self.kill_received      = False
        self.values["vel_x"]    = 0
        self.values["vel_y"]    = 0
        self.values["vel_z"]    = 0

    def read_sensor(self):
        if sensor.read():
            print("P: %0.1f mbar  %0.3f psi\tT: %0.2f C  %0.2f F") % (
            sensor.pressure(), # Default is mbar (no arguments)
            sensor.pressure(ms5837.UNITS_psi), # Request psi
            sensor.temperature(), # Default is degrees C (no arguments)
            sensor.temperature(ms5837.UNITS_Farenheit)) # Request Farenheit
        else:
            print("dist sensor read failed!")
            exit(1)
        ## see telemetry_msg.py for "imu_values" that map to self.values
        self.values["heading"] = sensor.euler[0] # deg
        self.values["roll"] = sensor.euler[1] # deg
        self.values["pitch"] = sensor.euler[2] # deg
        self.values["gyro_x"] = sensor.gyro[0] # deg / s
        self.values["gyro_y"] = sensor.gyro[1]
        self.values["gyro_z"] = sensor.gyro[2]
        self.values["vel_x"] += (sensor.linear_acceleration[0] - self.values["linear_accel_x"]) / self.poll_interval_s
        self.values["vel_y"] += (sensor.linear_acceleration[1] - self.values["linear_accel_y"]) / self.poll_interval_s
        self.values["vel_z"] += (sensor.linear_acceleration[2] - self.values["linear_accel_z"]) / self.poll_interval_s
        self.values["linear_accel_x"] = sensor.linear_acceleration[0] # m / s^2 # ignores gravity
        self.values["linear_accel_y"] = sensor.linear_acceleration[1]
        self.values["linear_accel_z"] = sensor.linear_acceleration[2]
        self.values["accel_x"] = sensor.acceleration[0] # m / s^2 # + gravity
        self.values["accel_y"] = sensor.acceleration[1]
        self.values["accel_z"] = sensor.acceleration[2]
        self.values["magnetic_x"] = sensor.magnetic[0] # uTeslas
        self.values["magnetic_y"] = sensor.magnetic[1]
        self.values["magnetic_z"] = sensor.magnetic[2]

        # print(self.values)

    def run(self):
        if SETTINGS["hardware"]["i2c"] == True:
            # We must initialize the sensor before reading it
            if not sensor.init():
                print("Sensor could not be initialized")
                exit(1)
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
            
    