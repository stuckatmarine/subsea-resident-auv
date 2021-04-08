import tensorflow as tf
import numpy as np
import math

# tensorflow 2.3.1
# numpy 1.18.5

interpreter = tf.lite.Interpreter(model_path="pilot.tflite")
interpreter.allocate_tensors()

input_details = interpreter.get_input_details()
output_details = interpreter.get_output_details()

input_shape0 = input_details[0]['shape']
input_data = np.array(np.ones(input_shape0), dtype=np.float32)
interpreter.set_tensor(input_details[0]['index'], input_data)

input_shape1 = input_details[1]['shape']
input_data = np.array([1, 2.0, 2.0, 2.0, 180, 2.0, 1.0, 2.0, 0, 0, 0, 0], dtype=np.float32)
input_data = np.reshape(input_data, input_shape1)
interpreter.set_tensor(input_details[1]['index'], input_data)

interpreter.invoke()
exp = np.vectorize(math.exp)
actions = exp(interpreter.get_tensor(129)[0])

thruster0 = actions[0:7].argmax(axis=0)
thruster1 = actions[7:14].argmax(axis=0)
thruster2 = actions[14:21].argmax(axis=0)
thruster3 = actions[21:28].argmax(axis=0)
verts = actions[28:35].argmax(axis=0)

print(f'thruster0: {thruster0} {actions[0:7]} {actions[0:7].sum()}')
print(f'thruster1: {thruster1} {actions[7:14]} {actions[7:14].sum()}')
print(f'thruster2: {thruster2} {actions[14:21]} {actions[14:21].sum()}')
print(f'thruster3: {thruster3} {actions[21:28]} {actions[21:28].sum()}')
print(f'verts: {verts} {actions[28:35]} {actions[28:35].sum()}')

 