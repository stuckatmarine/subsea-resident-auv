#!/usr/bin/env python
#  imu_sensor.py
#  Threaded module to continusly read the imu sensor at a polling
#    interval and update the shared value array.

import threading
import time
import copy

import timestamp
from srauv_settings import SETTINGS

USE_ME = False
FRESHWATER = 997.0474 # kg/m^3
SALTWATER  = 1023.6   # kg/m^3
GRAVITY    = 9.80665  # m/s^2
FG_MBAR    = FRESHWATER * GRAVITY * 1000

if SETTINGS["hardware"]["i2c"] == True and USE_ME:
    import board
    import busio
    import ms5837
    i2c = busio.I2C(board.SCL, board.SDA)
    sensor = ms5837.MS5837_02BA() # Default I2C bus is 1 (Raspberry Pi 3)

class Depth_Thread(threading.Thread):
    def __init__(self, tel:dict):
        threading.Thread.__init__(self)
        # self.config             = config
        # self.poll_interval_s    = config["poll_interval_s"]
        self.value_dict         = tel["depth_sensor_dict"]
        self.kill_received      = False
        self.last_update_ms     = 0
        self.poll_interval_ms   = 50

    def read_sensor(self):
        if sensor.read():
            print("P: %0.1f mbar  %0.3f psi\tT: %0.2f C  %0.2f F") % (
                    sensor.pressure(), # Default is mbar (no arguments)
                    sensor.pressure(ms5837.UNITS_psi), # Request psi
                    sensor.temperature(), # Default is degrees C (no arguments)
                    sensor.temperature(ms5837.UNITS_Farenheit)) # Request Farenheit
            self.value_dict["mbar"] = sensor.pressure()
            self.value_dict["temp"] = sensor.temperature()
            self.value_dict["depth"] = self.value_dict["mbar"] / FG_MBAR
            print(f"calculated depth:{self.value_dict['depth']}")
        else:
            print("depth sensor read failed!")
            exit(1)

        # print(self.values)

    def run(self):
        if SETTINGS["hardware"]["i2c"] == True and USE_ME:
            # We must initialize the sensor before reading it
            if not sensor.init():
                print("Sensor could not be initialized")
                exit(1)
            while not self.kill_received:
                try:
                    # start_time = time.time()
                    # self.read_sensor()
                    # time.sleep(self.poll_interval_s -
                    #     ((time.time() - start_time) % self.poll_interval_s))

                    time_now = timestamp.now_int_ms()
                    if (time_now - self.last_update_ms >= self.poll_interval_ms):
                        self.read_sensor()
                        self.last_update_ms = time_now
                    time.sleep(0.001)
                        
                except Exception as e:
                    print(f"Depth Sensor err:{e}")

                time.sleep(0.001)
        else:
            print(f"I2C not enabled in srauv_settings.json")
            
    