from pycoral.utils import edgetpu
import numpy as np


SENSOR_LEN = 12

class AutoPilot:
    def __init__(self, tel_msg: dict):
        self.tel_msg = tel_msg

        self.interpreter = edgetpu.make_interpreter('pilot.tflite')
        self.interpreter.allocate_tensors()
        
        self.input_details = self.interpreter.get_input_details()
        self.output_details = self.interpreter.get_output_details()

        self.input0 = np.array(np.zeros(self.input_details[0]['shape']), dtype=np.float32)

    def get_action(self):
        sensor = [
            tel_msg['tag_dict']['recent'][0],
            tel_msg['pos_x'],
            tel_msg['pos_y'],
            tel_msg['pos_z'],
            tel_msg['heading'],
            tel_msg['target_pos_x'],
            tel_msg['target_pos_y'],
            tel_msg['target_pos_z'],
            tel_msg['imu_dict']['linear_accel_x'],
            tel_msg['imu_dict']['linear_accel_y'],
            tel_msg['imu_dict']['linear_accel_z'],
            tel_msg['imu_dict']['gyro_y']
        ]
        input_data = np.array(sensor, dtype=np.float32)
        input_data = np.reshape(input_data, self.input_details[1]['shape'])

        self.interpreter.set_tensor(self.input_details[0]['index'], self.input0)
        self.interpreter.set_tensor(self.input_details[1]['index'], input_data)
        self.interpreter.invoke()

        for i in [19, 20, 30, 31, 32]:
            output_data = self.interpreter.get_tensor(i)
            print(f'{i}: {output_data}')

        dir_thrust = []

        output_dir = self.interpreter.get_tensor(19)[0]
        if output_dir == 1:
            dir_thrust.append('fwd')
        elif output_dir == 2:
            dir_thrust.append('rev')
        else:
            dir_thrust.append('_')

        output_dir = self.interpreter.get_tensor(20)[0]
        if output_dir == 1:
            dir_thrust.append('lat_right')
        elif output_dir == 2:
            dir_thrust.append('lat_left')
        else:
            dir_thrust.append('_')

        output_dir = self.interpreter.get_tensor(30)[0]
        if output_dir == 1:
            dir_thrust.append('rot_right')
        elif output_dir == 2:
            dir_thrust.append('rot_left')
        else:
            dir_thrust.append('_')

        output_dir = self.interpreter.get_tensor(31)[0]
        if output_dir == 1:
            dir_thrust.append('up')
        elif output_dir == 2:
            dir_thrust.append('down')
        else:
            dir_thrust.append('_')

        return dir_thrust
