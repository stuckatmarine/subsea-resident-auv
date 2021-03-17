import tensorflow as tf
import numpy as np

# tensorflow 2.3.1
# numpy 1.18.5

interpreter = tf.lite.Interpreter(model_path="pilot.tflite")
interpreter.allocate_tensors()

input_details = interpreter.get_input_details()
output_details = interpreter.get_output_details()

input_shape0 = input_details[0]['shape']
input_data = np.array(np.zeros(input_shape0), dtype=np.float32)
interpreter.set_tensor(input_details[0]['index'], input_data)

input_shape1 = input_details[1]['shape']
input_data = np.array([1, 2.0, 2.0, 2.0, 180, 2.0, 3.0, 2.0, 0, 0, 0, 0], dtype=np.float32)
input_data = np.reshape(input_data, input_shape1)
interpreter.set_tensor(input_details[1]['index'], input_data)


# https://forum.unity.com/threads/loading-ml-agents-trained-froze-graph-into-tensorflow.932076/#post-6093753
# https://github.com/Unity-Technologies/ml-agents/blob/main/com.unity.ml-agents/Runtime/Inference/TensorNames.cs
interpreter.invoke()
for i in [19, 20, 30, 31, 32]:
    output_data = interpreter.get_tensor(i)
    print(f'{i}: {output_data}')

# %%

#import onnx
#
#onnx_model = onnx.load('results/realisticTankV1.15_hackVel_revertedThrust/pilot.onnx')

 