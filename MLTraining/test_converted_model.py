import tensorflow as tf
import numpy as np
import math

# tensorflow 2.3.1
# numpy 1.18.5

interpreter = tf.lite.Interpreter(model_path="pilot2.tflite")
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
actions = exp(interpreter.get_tensor(111)[0])

longitudinal = actions[0:3].argmax(axis=0)
laterial = actions[3:6].argmax(axis=0)
vertical = actions[6:9].argmax(axis=0)
yaw = actions[9:12].argmax(axis=0)

print(f'longitudinal: {longitudinal} {actions[0:3]} {actions[0:3].sum()}')
print(f'laterial: {laterial} {actions[3:6]} {actions[3:6].sum()}')
print(f'vertical: {vertical} {actions[6:9]} {actions[6:9].sum()}')
print(f'yaw: {yaw} {actions[9:12]} {actions[9:12].sum()}')

 