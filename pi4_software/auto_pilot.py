import math
import numpy as np
from time import time

from srauv_settings import SETTINGS

thruster_min_change_time = 0.5  # 500ms

if SETTINGS["hardware"]["coral"] is True:
    from pycoral.utils import edgetpu
else:
    import tensorflow as tf


class AutoPilot:
    def __init__(self, tel_msg: dict):
        if SETTINGS["hardware"]["coral"] is True:
            self.interpreter = edgetpu.make_interpreter('pilot3.tflite')
        else:
            self.interpreter = tf.lite.Interpreter(model_path='pilot3.tflite')

        # for smoothing logic
        self.thruster_timers = [(time(), '_'), (time(), '_'),
                                (time(), '_'), (time(), '_')]
        
        # for velocity calc logic
        self.velocity_timer = 0
        self.last_pos_x = tel_msg['pos_x']
        self.last_pos_y = tel_msg['pos_y']
        self.last_pos_z = tel_msg['pos_z']
    
        self.tel_msg = tel_msg
        self.exp = np.vectorize(math.exp)
        self.interpreter.allocate_tensors()

        self.input_details = self.interpreter.get_input_details()
        self.output_details = self.interpreter.get_output_details()

        self.action_masks = np.array(np.ones(self.input_details[0]['shape']), dtype=np.float32)

    def _collect_observations_accel(self):
        return np.reshape(np.array([
            self.tel_msg['tag_dict']['recent'][0],
            self.tel_msg['pos_x'],
            self.tel_msg['pos_y'],
            self.tel_msg['pos_z'],
            self.tel_msg['heading'],
            self.tel_msg['target_pos_x'],
            self.tel_msg['target_pos_y'],
            self.tel_msg['target_pos_z'],
            self.tel_msg['imu_dict']['linear_accel_x'],
            self.tel_msg['imu_dict']['linear_accel_y'],
            self.tel_msg['imu_dict']['linear_accel_z'],
            self.tel_msg['imu_dict']['gyro_y']
        ], dtype=np.float32), self.input_details[1]['shape'])
    
    def _collect_observations_vel(self):
        curr_time = time()
        vel_x = 0
        vel_y = 0
        vel_z = 0
        
        if self.velocity_timer != 0:
            vel_x = (self.tel_msg['pos_x'] - self.last_pos_x)/(curr_time - self.velocity_timer)
            vel_y = (self.tel_msg['pos_y'] - self.last_pos_y)/(curr_time - self.velocity_timer)
            vel_z = (self.tel_msg['pos_z'] - self.last_pos_z)/(curr_time - self.velocity_timer)
            
            if abs(vel_x) > 0.15: vel_x = 0.15 if vel_x > 0 else -0.15
            if abs(vel_y) > 0.15: vel_y = 0.15 if vel_y > 0 else -0.15
            if abs(vel_z) > 0.15: vel_z = 0.15 if vel_z > 0 else -0.15
        
        # update params for next run
        self.velocity_timer = curr_time
        self.last_pos_x = self.tel_msg['pos_x']
        self.last_pos_y = self.tel_msg['pos_y']
        self.last_pos_z = self.tel_msg['pos_z']
        
        return np.reshape(np.array([
            self.tel_msg['tag_dict']['recent'][0],
            self.tel_msg['pos_x'],
            self.tel_msg['pos_y'],
            self.tel_msg['pos_z'],
            self.tel_msg['heading'],
            self.tel_msg['target_pos_x'],
            self.tel_msg['target_pos_y'],
            self.tel_msg['target_pos_z'],
            vel_x,
            vel_y,
            vel_z,
            self.tel_msg['imu_dict']['gyro_y']
        ], dtype=np.float32), self.input_details[1]['shape'])

    def _thruster_safety(self, dir_thrust):
        curr_time = time()

        for i in range(0, len(dir_thrust)):
            if dir_thrust[i] != self.thruster_timers[i][1]:
                if (self.thruster_timers[i][0] + thruster_min_change_time) < curr_time:
                    # been long enough, its okay to swap dir
                    self.thruster_timers[i] = (curr_time, dir_thrust[i])
                else:
                    # keep old thrust dir, hasnt been long enough to swap
                    dir_thrust[i] = self.thruster_timers[i][1]

        return dir_thrust

    def get_action(self):
        self.interpreter.set_tensor(self.input_details[0]['index'], self.action_masks)
        self.interpreter.set_tensor(self.input_details[1]['index'], self._collect_observations_vel())
        self.interpreter.invoke()

        actions = self.exp(self.interpreter.get_tensor(111)[0]) # 111 0r 105
        if not actions.any():
            return ['_', '_', '_', '_']

        longitudinal = actions[0:3].argmax(axis=0)
        laterial = actions[3:6].argmax(axis=0)
        vertical = actions[6:9].argmax(axis=0)
        yaw = actions[9:12].argmax(axis=0)

        print(f'longitudinal: {longitudinal} {actions[0:3]} {actions[0:3].sum()}')
        print(f'laterial: {laterial} {actions[3:6]} {actions[3:6].sum()}')
        print(f'vertical: {vertical} {actions[6:9]} {actions[6:9].sum()}')
        print(f'yaw: {yaw} {actions[9:12]} {actions[9:12].sum()}')

        print(actions)

        dir_thrust = []

        if longitudinal == 1:
            dir_thrust.append('fwd')
        elif longitudinal == 2:
            dir_thrust.append('rev')
        else:
            dir_thrust.append('_')

        if laterial == 1:
            dir_thrust.append('lat_right')
        elif laterial == 2:
            dir_thrust.append('lat_left')
        else:
            dir_thrust.append('_')

        if yaw == 1:
            dir_thrust.append('rot_right')
        elif yaw == 2:
            dir_thrust.append('rot_left')
        else:
            dir_thrust.append('_')

        if vertical == 1:
            dir_thrust.append('up')
        elif vertical == 2:
            dir_thrust.append('down')
        else:
            dir_thrust.append('_')

        return self._thruster_safety(dir_thrust)
