######################################
# APRIL Tags & Camera Pose
#
# Completed for 2021 SRAUV Capstone
# and ECE 8410 Term Project
#
# Mark Belbin - 2021
######################################

# Run this on the Raspberry Pi 4 only, requires 
# custom build of OpenCV 4 to enable GStreamer

import numpy as np
import cv2
import time
from pupil_apriltags import Detector
import math

from socket_sender import send_over_socket

# Define tag detector
at_detector = Detector(families='tag16h5',
                       nthreads=4,
                       quad_decimate=2.0,
                       quad_sigma=0.0,
                       refine_edges=1,
                       decode_sharpening=0.50,
                       debug=0)

# Global Camera Pose Parameters #
gCam_pose_T = np.array([[0,0,0,0],[0,0,0,0],[0,0,0,0],[0,0,0,0]])
gCam_pose_t = np.array([0,0,0])
gCam_pose_R = np.array([[0,0,0],[0,0,0],[0,0,0]])
gRx = 0.0
gRy = 0.0
gTID = None

# Global AUV Localization Parameters #
gAUVx = 0.0
gAUVy = 0.0
gAUVz = 0.0
gAUVheading = 0.0

# Transformation Matrices #

# Tank to Marker Transforms
gTank_T_Marker1 = np.array([[0.0, 0.0,-1.0, 0.0],
                            [1.0, 0.0, 0.0, 2.0],
                            [0.0,-1.0, 0.0, 2.0],
                            [0.0, 0.0, 0.0, 1.0]])

gTank_T_Marker2 = np.array([[1.0, 0.0, 0.0, 2.0],
                            [0.0, 0.0, 1.0, 4.0],
                            [0.0,-1.0, 0.0, 2.0],
                            [0.0, 0.0, 0.0, 1.0]])

gTank_T_Marker3 = np.array([[ 0.0, 0.0, 1.0, 4.0],
                            [-1.0, 0.0, 0.0, 2.0],
                            [ 0.0,-1.0, 0.0, 2.0],
                            [ 0.0, 0.0, 0.0, 1.0]])

gTank_T_Marker4 = np.array([[-1.0, 0.0, 0.0, 2.0],
                            [ 0.0, 0.0,-1.0, 0.0],
                            [ 0.0,-1.0, 0.0, 2.0],
                            [ 0.0, 0.0, 0.0, 1.0]])

# Camera to AUV transforms
gFrontCam_T_AUV = np.array([[1.0, 0.0, 0.0, 0.0],
                            [0.0, 0.0,-1.0, 0.0],
                            [0.0, 1.0, 0.0,-0.184],
                            [0.0, 0.0, 0.0, 1.0]])

gBackCam_T_AUV = np.array([[-1.0, 0.0, 0.0, 0.0  ],
                            [ 0.0, 0.0,-1.0, 0.0  ],
                            [ 0.0,-1.0, 0.0,-0.184],
                            [ 0.0, 0.0, 0.0, 1.0  ]])

Tank_T_AUV = np.array([[0.0, 0.0, 0.0, 0.0  ],
                            [ 0.0, 0.0, 0.0, 0.0  ],
                            [ 0.0, 0.0, 0.0, 0.0],
                            [ 0.0, 0.0, 0.0, 0.0  ]])

# Output video parameters #
vid_encoder = cv2.VideoWriter_fourcc(*"MJPG")
fps = 15
frame_size = (640,480)
video_out = cv2.VideoWriter('output_vid.avi', vid_encoder, fps, frame_size)

# Open Camera Feed

print("Camera sink open, Waiting for camera feed...")

#cap_receive = cv2.VideoCapture('udpsrc port=5004 ! application/x-rtp,encoding_name=H264,payload=96 ! rtph264depay ! v4l2h264dec ! videoconvert ! appsink', cv2.CAP_GSTREAMER)
cap_receive = cv2.VideoCapture('udpsrc port=5004 ! application/x-rtp,encoding_name=H264,payload=96 ! rtph264depay ! v4l2h264dec capture-io-mode=4 ! v4l2convert capture-io-mode=4  ! appsink sync=false', cv2.CAP_GSTREAMER)


print("Camera feed detected, press 'ctrl+c' to quit")

if not cap_receive.isOpened():
    print('VideoCapture not opened')
    exit()

# Program start time
t0 = time.time()

# Main Detection Loop
while True:
    ret,frame = cap_receive.read()

    if not ret:
        print('empty frame')
        break
    
    # Pre-tag-detection start time
    t1 = time.time()

    # Convert frame to gray and detect april tags using underwater camera calibration
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    tag_results = at_detector.detect(gray, estimate_tag_pose=True, camera_params=[1127.7,1139.3,314.7352,226.3186], tag_size=0.1555)

    time_detect = time.time()-t1 

    for tag in tag_results:
        # Eliminate false positives by checking the hamming attribute
        if (tag.hamming == 0):
            # extract the bounding box (x, y)-coordinates for the AprilTag
            # and convert each of the (x, y)-coordinate pairs to integers
            (ptA, ptB, ptC, ptD) = tag.corners
            ptB = (int(ptB[0]), int(ptB[1]))
            ptC = (int(ptC[0]), int(ptC[1]))
            ptD = (int(ptD[0]), int(ptD[1]))
            ptA = (int(ptA[0]), int(ptA[1]))

            # Filter out small false positives that pass hamming test
            if (ptC[0]-ptA[0] > 30) and (ptC[1]-ptA[1] > 30):

                gTID = tag.tag_id # Set tag ID

                # draw the bounding box of the AprilTag detection
                cv2.line(frame, ptA, ptB, (0, 255, 0), 2)
                cv2.line(frame, ptB, ptC, (0, 255, 0), 2)
                cv2.line(frame, ptC, ptD, (0, 255, 0), 2)
                cv2.line(frame, ptD, ptA, (0, 255, 0), 2)

                # draw the center (x, y)-coordinates of the AprilTag
                (cX, cY) = (int(tag.center[0]), int(tag.center[1]))
                cv2.circle(frame, (cX, cY), 5, (0, 0, 255), -1)
                
                # Calculate camera pose parameters
                gCam_pose_T = np.vstack((np.hstack((tag.pose_R, tag.pose_t)), np.array([0.0,0.0,0.0,1.0])))
                gCam_pose_t = tag.pose_t
                gCam_pose_R = tag.pose_R
                Rx = math.atan2(tag.pose_R[2,1], tag.pose_R[1,1]) * 180.0 / math.pi
                Ry = math.atan2(tag.pose_R[0,2], tag.pose_R[0,0]) * 180.0 / math.pi

                # Calculate AUV frame relative to tank frame
                if (gCam_pose_T.any()):
                    if tag.tag_id == 0:
                        Tank_T_AUV = gTank_T_Marker1 @ np.linalg.inv(gCam_pose_T) @ gFrontCam_T_AUV
                    elif tag.tag_id == 1:
                        Tank_T_AUV = gTank_T_Marker2 @ np.linalg.inv(gCam_pose_T) @ gFrontCam_T_AUV
                    elif tag.tag_id == 2:
                        Tank_T_AUV = gTank_T_Marker3 @ np.linalg.inv(gCam_pose_T) @ gFrontCam_T_AUV
                    elif tag.tag_id == 3:
                        Tank_T_AUV = gTank_T_Marker4 @ np.linalg.inv(gCam_pose_T) @ gFrontCam_T_AUV
                    else:
                        Tank_T_AUV = gTank_T_Marker1 @ np.linalg.inv(gCam_pose_T) @ gBackCam_T_AUV
                
                # Calculte AUV parameters
                gAUVx = Tank_T_AUV[0, 3]
                gAUVy = Tank_T_AUV[1, 3]
                gAUVz = Tank_T_AUV[2, 3]
                gAUVheading = math.atan2(Tank_T_AUV[1,0], Tank_T_AUV[0,0]) * 180.0 / math.pi

                print("TAG! at " + f'{(time.time()-t0):.4f}' + " s")

    # Add Pose details to frame view if Tag detected
    if(gTID is not None):
        cv2.putText(frame, "CAM_X: " + f'{gCam_pose_t[0,0]:.3f}' + "m", (50,400), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "CAM_Y: " + f'{gCam_pose_t[1,0]:.3f}' + "m", (50,420), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "CAM_Z: " + f'{gCam_pose_t[2,0]:.3f}' + "m", (50,440), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)

        cv2.putText(frame, "CAM_ThetaX: " + f'{(gRx):.3f}' + " Deg", (200,400), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "CAM_ThetaY: " + f'{(gRy):.3f}' + " Deg", (200,420), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "TagID: " + str(gTID), (200,440), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "Detect Time: " + f'{(time_detect*1000):.2f}' + " ms", (200,460), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)

        cv2.putText(frame, "AUV_X: " + f'{gAUVx:.3f}' + "m", (440,400), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "AUV_Y: " + f'{gAUVy:.3f}' + "m", (440,420), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "AUV_Z: " + f'{gAUVz:.3f}' + "m", (440,440), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
        cv2.putText(frame, "AUV_Yaw: " + f'{gAUVheading:.3f}' + " Deg", (440,460), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (255,255,255), 1, cv2.LINE_AA)
    
    # Write frame to recorded video
    video_out.write(frame)
    send_over_socket(gAUVx, gAUVy, gAUVz)

# Once finished, release / destroy windows
print("Cleaning up...")
video_out.release()
cap_receive.release()
cv2.destroyAllWindows()