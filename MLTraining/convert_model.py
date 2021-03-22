import tensorflow as tf


# first run onnx-tf convert -i "pilot.onnx" -o  "pilot.pb"
path = 'results/realisticTankV1.16_revertedThrust/pilot.pb'

# TODO: look into tf.lite.Optimize
converter = tf.lite.TFLiteConverter.from_saved_model(path) 
tf_lite_model = converter.convert()
open('pilot.tflite', 'wb').write(tf_lite_model)
