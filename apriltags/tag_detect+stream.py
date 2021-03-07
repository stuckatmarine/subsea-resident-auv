import numpy as np
import cv2
import time
from pupil_apriltags import Detector
import math

# Define tag detector
at_detector = Detector(families='tag36h11',
                       nthreads=3,
                       quad_decimate=1.0,
                       quad_sigma=0.0,
                       refine_edges=1,
                       decode_sharpening=0.25,
                       debug=0)

# Global Camera Pose Parameters
gTag_pose_t = np.array([0,0,0])
gTag_pose_R = np.array([[0,0,0],[0,0,0],[0,0,0]])
gTag_pose_T = np.array([[0,0,0,0],[0,0,0,0],[0,0,0,0],[0,0,0,0]])

gRx = 0.0
gRy = 0.0
gTID = None

print("Camera sink open, Waiting for camera feed...")

cap_receive = cv2.VideoCapture('udpsrc port=5001 ! application/x-rtp,payload=96 ! rtph264depay ! avdec_h264 ! videoconvert ! appsink', cv2.CAP_GSTREAMER)

print("Camera feed detected, press 'q' to quit and 'c' to capture")


if not cap_receive.isOpened():
    print('VideoCapture not opened')
    exit()

cap_count = 0
while True:
    ret,frame = cap_receive.read()

    if not ret:
        print('empty frame')
        break
    
    t1 = time.time()

    # Convert frame to gray and detect april tags
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    tag_results = at_detector.detect(gray, estimate_tag_pose=True, camera_params=[800.5335,801.2600,313.2403,231.1194], tag_size=0.17289)

    time_detect = time.time()-t1

    for tag in tag_results:
        # extract the bounding box (x, y)-coordinates for the AprilTag
        # and convert each of the (x, y)-coordinate pairs to integers
        (ptA, ptB, ptC, ptD) = tag.corners
        ptB = (int(ptB[0]), int(ptB[1]))
        ptC = (int(ptC[0]), int(ptC[1]))
        ptD = (int(ptD[0]), int(ptD[1]))
        ptA = (int(ptA[0]), int(ptA[1]))
        # draw the bounding box of the AprilTag detection
        cv2.line(frame, ptA, ptB, (0, 255, 0), 2)
        cv2.line(frame, ptB, ptC, (0, 255, 0), 2)
        cv2.line(frame, ptC, ptD, (0, 255, 0), 2)
        cv2.line(frame, ptD, ptA, (0, 255, 0), 2)
        # draw the center (x, y)-coordinates of the AprilTag
        (cX, cY) = (int(tag.center[0]), int(tag.center[1]))
        cv2.circle(frame, (cX, cY), 5, (0, 0, 255), -1)
        
        #print(tag.pose_R)
        gTag_pose_t = tag.pose_t
        gTag_pose_R = tag.pose_R
        #gTag_pose_H = np.array([[gTag_pose_R[0,0],gTag_pose_R[0,1],gTag_pose_R[0,2],gTag_pose_t[0,0]],
        #                        [gTag_pose_R[1,0],gTag_pose_R[1,1],gTag_pose_R[1,2],gTag_pose_t[1,0]],
        #                        [gTag_pose_R[2,0],gTag_pose_R[2,1],gTag_pose_R[2,2],gTag_pose_t[2,0]],
        #                        [0.0,0.0,0.0,1.0]])

        gTag_pose_T = np.vstack((np.hstack((gTag_pose_R, gTag_pose_t)), np.array([0.0,0.0,0.0,1.0])))

        gRx = math.atan2(gTag_pose_R[2,1], gTag_pose_R[1,1]) * 180.0 / math.pi
        gRy = math.atan2(gTag_pose_R[0,2], gTag_pose_R[0,0]) * 180.0 / math.pi

        gTID = tag.tag_id

        print(gTag_pose_T)
        print("Camera X Rotation: " + str(gRx))
        print("Camera Y Rotation: "+ str(gRy))

    # Add Pose details to frame view if Tag detected
    if(gTID is not None):
        cv2.putText(frame, "Z: " + f'{gTag_pose_t[2,0]:.3f}' + "m", (50,440), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "Y: " + f'{gTag_pose_t[1,0]:.3f}' + "m", (50,420), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "X: " + f'{gTag_pose_t[0,0]:.3f}' + "m", (50,400), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)

        cv2.putText(frame, "ThetaX: " + f'{(gRx):.3f}' + " Deg", (200,400), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "ThetaY: " + f'{(gRy):.3f}' + " Deg", (200,420), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "TagID: " + str(gTID), (200,440), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        
        cv2.putText(frame, "Detect Time: " + f'{(time_detect*1000):.2f}' + " ms", (400,400), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)


    # Show the output frame after AprilTag detection
    cv2.imshow('receive', frame)

    if cv2.waitKey(1)&0xFF == ord('q'):
        break

    if cv2.waitKey(1)&0xFF == ord('c'):
        cv2.imwrite("Frames/capture" + f'{cap_count:03d}' + ".jpg", frame)
        print("Frame " + f'{cap_count:03d}' + " Captured!")
        cap_count = cap_count + 1



cap_receive.release()

cv2.destroyAllWindows()
