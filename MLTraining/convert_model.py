import tensorflow as tf


# first run onnx-tf convert -i "pilot.onnx" -o  "pilot.pb"
path = 'results/curriculum_2layers_mediumBuffer_8epoch/pilot.pb'

# TODO: look into tf.lite.Optimize
converter = tf.lite.TFLiteConverter.from_saved_model(path) 
tf_lite_model = converter.convert()
open('pilot2.tflite', 'wb').write(tf_lite_model)
