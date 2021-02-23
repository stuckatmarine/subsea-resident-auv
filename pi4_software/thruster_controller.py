#!/usr/bin/env python
#  thruster_controller.py
#  Threaded module to continusly apply thruster values.
#
#  Deadman timout included to zero thrusters if no recend command
#    is received.

import threading
import time
import can

# "CAN_tx_ids":{
#     "arm_id": 0,              # 0x01 to arm
#     "abort_id": 1,            # 0x01 to estop, need pwr cycle to recover
#     "motor_onoff_id": 2,      # 0x01 to enable
#     "set_RPM_id": 3,          # 3B: 1B-> 1/0 for fwd/rev , 2B -> int @ 3.5k RPM max
#     "set_accel_id": 4         # 2B: int @ 1500hz max
# },
# "CAN_rx_ids":{
#     "measured_RPM_id": 5,     # same as RPM format
#     "measured_voltage_id": 6, # 2B: 1st bit sign, 15 bits int * 2^-10 for FP result
#     "measured_torque_id": 7,  # ^^
#     "board_state_id": 8,      # 0 IDLE, 1 ARM, 2 MOTOR_ENABLED, 3 = ABORT
#     "fault_state_id": 9       # Should be 0, else something bad
# },

# send arm 0x01
# send motor_onoff 0x01
# send set_RPM 0x01 3000

class ThrusterThread(threading.Thread):
    def __init__(self, config, id, data_arr):
        threading.Thread.__init__(self)
        self.config = config
        self.id = id
        self.thrust_arr = data_arr
        self.kill_received = False
        self.thrust_interval_s = config["thrust_interval_s"]
        self.deadman_timeout_s = config["deadman_timeout_s"]
        self.last_heartbeat = 0
        self.thrust_enabled = False
        self.can_up = True
        self.last_update_ms = 0

        print(f"Creating canbus objects")
        self.tx_cmds = config["CAN_tx_ids"]
        self.rx_cmds = config["CAN_rx_ids"]
        self.board_id = (id + config["board_id_base"]) << config["board_id_shift"]
        self.is_armed = False
        self.is_motor_on = False
        self.motor_rpm = 0

        self.bus = can.interface.Bus(channel='can0', bustype='socketcan')

        #  other examples
        # bus = can.interface.Bus(bustype='socketcan', channel='vcan0', bitrate=250000)
        # bus = can.interface.Bus(bustype='pcan', channel='PCAN_USBBUS1', bitrate=250000)
        # bus = can.interface.Bus(bustype='ixxat', channel=0, bitrate=250000)
        # bus = can.interface.Bus(bustype='vector', app_name='CANalyzer', channel=0, bitrate=250000)
        # notifier = can.Notifier(bus, [can.Printer()])
    

    def send_msg(self, cmd_str, d):
        can_id = self.board_id | self.tx_cmds[cmd_str]
        msg = can.Message(arbitration_id=can_id, data=d, is_extended_id=False)
        if True:#self.can_up:
            self.bus.send(msg)
            print(f"Thruster sending CAN_id:{can_id} data:{d}")
        else:
            print(f"CANBUS not up, thruster id:{self.id} thrust_enabled:{self.thrust_enabled} motor_on:{self.is_motor_on}")

    def read_msg(self):
        pass

    def apply_thrust(self):
        if self.thrust_enabled == False:
            #  TODO stop thrusters if first cycle afer stopping
            if self.is_motor_on:
                self.send_msg("motor_onoff_id", 0x00)
                #  TODO check for motor off msg
            return

        # check if deadman has timedout
        if time.time() - self.last_heartbeat >= self.deadman_timeout_s:
            self.thrust_enabled = False
            self.send_msg("motor_onoff_id", 0x00)
            print(f"Deadman expired, Thruster_id:{self.id} thrust_enabled:{self.thrust_enabled}")
        else:
            # if not self.is_motor_on:
            #     self.send_msg("motor_onoff_id", 0x00)
            #     #  TODO check for motor off msg

            print(f"Applying Thrust, Thruster_id:{self.id} thrust_value:{self.thrust_arr[self.id]}")
            
            thrust_dir = 0x01
            thrust_RPM = self.thrust_arr[self.id]
            if thrust_RPM < 0:
                thrust_dir = 0x00
                thrust_RPM = -thrust_RPM

            if thrust_RPM >= self.config["max_thrust"]:
                thrust_RPM = self.config["rpm_max"]
            else:
                thrust_RPM = thrust_RPM / self.config["max_thrust"] * self.config["rpm_max"]
            
            self.send_msg("set_RPM_id", [thrust_dir, (thrust_RPM & 0xff00) >> 16, thrust_RPM & 0xff])

    def do_thrust(self, enable):
        if enable and not self.is_motor_on:
            self.send_msg("motor_onoff_id", [0x01])
            # notifier = can.Notifier(bus, [can.Printer()]) #listener alternative?

        self.thrust_enabled = enable
        self.last_heartbeat = time.time()


    def run(self):
        # ARM thruster
        self.send_msg("arm_id", [0x01])
        time.sleep(2)

        # TODO: check armed msg

        while not self.kill_received:
            time_now = timestamp.now_int_ms()
            try:
                if (time_now - self.last_update_ms >= self.thrust_interval_s):
                    # self.apply_thrust()
                    # Send 
                    self.send_msg("set_RPM_id", [0x00,0x00,0xFF])

                    self.last_update_ms = timestamp.now_int_ms()
                time.sleep(0.001)
            except Exception as oof:
                print(oof)
        
        # After kill, turn off thruster
        self.send_msg("motor_onoff_id", [0x00])