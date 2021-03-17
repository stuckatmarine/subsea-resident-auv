#!/usr/bin/env python
#  thruster_controller.py
#  Threaded module to continusly apply thruster values and manage ESC state
#

import threading
import time
import can

import timestamp
import logger
from sys import platform
from srauv_settings import SETTINGS

# "CAN_tx_ids":{
#     "arm": 0,              # 0x01 to arm
#     "abort": 1,            # 0x01 to estop, need pwr cycle to recover
#     "motor_onoff": 2,      # 0x01 to enable
#     "set_rpm": 3,          # 3B: 1B-> 1/0 for fwd/rev , 2B -> int @ 3.5k RPM max
#     "set_accel": 4         # 2B: int @ 1500hz max
# },
# "CAN_rx_ids":{
#     "measured_RPM": 5,     # same as RPM format
#     "measured_voltage": 6, # 2B: 1st bit sign, 15 bits int * 2^-10 for FP result
#     "measured_torque": 7,  # ^^
#     "board_state": 8,      # 0 IDLE, 1 ARM, 2 MOTOR_ENABLED, 3 = ABORT
#     "fault_state": 9       # Should be 0, else something bad
# }

class ThrusterThread(threading.Thread):
    def __init__(self, config: dict, tel: dict, id: int, logger: classmethod):
        threading.Thread.__init__(self)
        self.config             = config
        self.thrust_interval_ms = config["thrust_interval_ms"]
        self.deadman_timeout_ms = config["deadman_timeout_ms"]
        self.tx_cmds            = config["CAN_tx_ids"]
        self.rx_cmds            = config["CAN_rx_ids"]
        self.gimp               = config["gimp_thruster_id"]
        self.thrust_arr         = tel["thrust_values"]
        self.thrust_enabled     = tel["thrust_enabled"]
        self.id                 = id
        self.logger             = logger
        self.kill_received      = False

        self.board_id = (id + config["board_id_base"]) << config["board_id_shift"]
        self.bus                = None
        self.can_up             = False
        self.is_armed           = False
        self.is_motor_on        = False
        self.last_update_ms     = 0
        self.motor_rpm          = 0
        
        if SETTINGS["hardware"]["can"] == True:
            self.bus = can.interface.Bus(channel='can0', bustype='socketcan')
            self.can_up = True
        else:
            self.logger.warn(f"can not enabled in srauv_settings.json")
            print(f"CAN not enabled in srauv_settings.json")

        print(f"Thruster thread {self.id} up, can_up:{self.can_up}")
        logger.info(f"Thruster thread {self.id} up, can_up:{self.can_up}")

        #  other examples
        # bus = can.interface.Bus(bustype='socketcan', channel='vcan0', bitrate=250000)
        # bus = can.interface.Bus(bustype='pcan', channel='PCAN_USBBUS1', bitrate=250000)
        # bus = can.interface.Bus(bustype='ixxat', channel=0, bitrate=250000)
        # bus = can.interface.Bus(bustype='vector', app_name='CANalyzer', channel=0, bitrate=250000)
        # notifier = can.Notifier(bus, [can.self.logger.infoer()])
    
    def send_msg(self, cmd_str: str, d: list):
        if SETTINGS["hardware"]["can"] == False:
            return # allows windows rpm debugging
        if self.can_up == True:
            can_id = self.board_id | self.tx_cmds[cmd_str]
            msg = can.Message(arbitration_id=can_id, data=d, is_extended_id=False)
            self.bus.send(msg)
            self.logger.info(f"Thruster sending CAN_id:{can_id:03x} data:{d}")
        else:
            self.logger.warning(f"CANBUS not up, thruster id: \
                {self.id} thrust_enabled:{self.thrust_enabled[0]} motor_on:{self.is_motor_on}")


    def apply_thrust(self):
        # Do nothing if not in a thrust enabled state
        if self.thrust_enabled[0] == False:
            #print ("thrust not enabled")
            if self.is_motor_on == True:
                self.send_msg("motor_onoff", [0x00])
                time.sleep(4)
                #  TODO check for motor off msg
                self.is_motor_on = False
        else:
            if self.is_motor_on == False:
                self.send_msg("motor_onoff", [0x01])
                time.sleep(4)
                #  TODO check for motor on msg
                self.is_motor_on = True
                print("thruster motor on")

            self.logger.info(f"Applying Thrust, Thruster_id:{self.id} thrust_value:{self.thrust_arr[self.id]}")
            
            # Calculate thrust msg params based on thrust values in tel
            thrust_dir = 0x01
            thrust_RPM = self.thrust_arr[self.id]
            if thrust_RPM < 0:
                thrust_dir = 0x00
                thrust_RPM = -thrust_RPM # makes the ned num positive

            if thrust_RPM >= self.config["max_thrust"]:
                thrust_RPM = self.config["rpm_max"]
            else:
                thrust_RPM = thrust_RPM / self.config["max_thrust"] * self.config["rpm_max"]
            
            if self.config["direction_arr"][self.id] == False:
                thrust_dir = 0x01 if thrust_dir == 0x00 else 0x00

            th_hi = (int(thrust_RPM) >> 8) & 0xff
            th_lo = int(thrust_RPM) & 0xff
            self.send_msg("set_rpm", [thrust_dir, th_hi, th_lo])
            # self.send_msg("set_rpm", [0x00,0x05,0xDC]) # low test value
            print(f"thruster id:{self.id} thrust_RPM:{thrust_RPM} th_hi:{th_hi} th_lo:{th_lo}")

    def read_msg(self):
        pass

    def run(self):
        if SETTINGS["hardware"]["can"] == True and self.id != self.gimp:
            self.send_msg("arm", [0x01])
            time.sleep(1)
            print ("thruster armed")
            # TODO: check for armed msg, handle failure conditions
            self.is_armed = True

        while not self.kill_received:
            time_now = timestamp.now_int_ms()
            try:
                if (time_now - self.last_update_ms >= self.thrust_interval_ms):
                    self.apply_thrust()
                    self.last_update_ms = time_now
                time.sleep(0.001)

            except Exception as oof:
                self.logger.info(oof)
            
        self.send_msg("motor_onoff", [0x00])

        # else:
        #     self.logger.warn(f"setting hardware CAN not enabled for thruster id{self.id}")
        #     print(f"setting hardware CAN not enabled for thruster id:{self.id}")
