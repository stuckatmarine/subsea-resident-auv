import numpy as np
import cv2
import time

print("Camera sink open, Waiting for camera feed...")

cap_receive = cv2.VideoCapture('udpsrc port=5001 ! application/x-rtp,payload=96 ! rtph264depay ! avdec_h264 ! videoconvert ! appsink', cv2.CAP_GSTREAMER)

print("Camera feed detected, press 'q' to quit and 'c' to capture")



while not cap_receive.isOpened():
    print('VideoCapture not opened')
    time.sleep(0.1)

cap_count = 0
while True:
    ret,frame = cap_receive.read()

    if not ret:
        print('empty frame')
        break

    cv2.imshow('receive', frame)

    if cv2.waitKey(1)&0xFF == ord('q'):
        break

    if cv2.waitKey(1)&0xFF == ord('c'):
        cv2.imwrite("Frames/capture" + f'{cap_count:03d}' + ".jpg", frame)
        print("Frame " + f'{cap_count:03d}' + " Captured!")
        cap_count = cap_count + 1



cap_receive.release()

cv2.destroyAllWindows()
