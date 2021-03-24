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

# Imports #
import numpy as np
import cv2
import time
from pupil_apriltags import Detector
import math
from socket_sender import send_over_socket

# If DEBUG mode is True, displays tag and camera pose information to video
DEBUG = True

# If STREAM, video is captured from gstreamer pipeline, else its read from a file
STREAM = True

# If WATER, use underwater camera calibration results
WATER = True

# Z offset/scale calculated from z measurements 
z_offset = 0.12274
z_scale = 0.77153

# Define tag detector #
at_detector = Detector(families='tag16h5',
                       nthreads=4,
                       quad_decimate=1.0,
                       quad_sigma=0.0,
                       refine_edges=1,
                       decode_sharpening=0.25,
                       debug=0)

# Global Camera Pose Parameters #
gCam_pose_T = np.array([[0,0,0,0],[0,0,0,0],[0,0,0,0],[0,0,0,0]])
gCam_pose_t = np.array([0,0,0])
gCam_pose_R = np.array([[0,0,0],[0,0,0],[0,0,0]])
gRx = 0.0
gRy = 0.0
gRz = 0.0
gTID = None

# Global AUV Localization Parameters #
gAUVx = 0.0
gAUVy = 0.0
gAUVz = 0.0
gAUVheading = 0.0

# Running average 
gAUVx_ravg = []
gAUVy_ravg = []
gAUVz_ravg = []
gAUVHeading_ravg = []
ravg_len = 10

# Transformation Matrices #

# Tank to Marker Transforms

gTank_T_Tag0 = np.array([[ 1.0, 0.0, 0.0, 0.750],
                         [ 0.0,-1.0, 0.0, 0.500],
                         [ 0.0, 0.0,-1.0, 0.0],
                         [ 0.0, 0.0, 0.0, 1.0]])

gTank_T_Tag1 = np.array([[ 1.0, 0.0, 0.0, 1.548],
                         [ 0.0,-1.0, 0.0, 0.500],
                         [ 0.0, 0.0,-1.0, 0.0],
                         [ 0.0, 0.0, 0.0, 1.0]])

gTank_T_Tag2 = np.array([[ 1.0, 0.0, 0.0, 2.343],
                         [ 0.0,-1.0, 0.0, 0.500],
                         [ 0.0, 0.0,-1.0, 0.0],
                         [ 0.0, 0.0, 0.0, 1.0]])

gTank_T_Tag3 = np.array([[ 1.0, 0.0, 0.0, 0.750],
                         [ 0.0,-1.0, 0.0, 1.265],
                         [ 0.0, 0.0,-1.0, 0.0],
                         [ 0.0, 0.0, 0.0, 1.0]])

gTank_T_Tag4 = np.array([[ 1.0, 0.0, 0.0, 1.550],
                         [ 0.0,-1.0, 0.0, 1.295],
                         [ 0.0, 0.0,-1.0, 0.0],
                         [ 0.0, 0.0, 0.0, 1.0]])

gTank_T_Tag5 = np.array([[ 1.0, 0.0, 0.0, 2.350],
                         [ 0.0,-1.0, 0.0, 1.285],
                         [ 0.0, 0.0,-1.0, 0.0],
                         [ 0.0, 0.0, 0.0, 1.0]])

gTank_T_Tag12 = np.array([[ 1.0, 0.0, 0.0, 0.750],
                         [ 0.0,-1.0, 0.0, 2.057],
                         [ 0.0, 0.0,-1.0, 0.0],
                         [ 0.0, 0.0, 0.0, 1.0]])

gTank_T_Tag14 = np.array([[ 1.0, 0.0, 0.0, 1.545],
                         [ 0.0,-1.0, 0.0, 2.065],
                         [ 0.0, 0.0,-1.0, 0.0],
                         [ 0.0, 0.0, 0.0, 1.0]])

gTank_T_Tag16 = np.array([[ 1.0, 0.0, 0.0, 2.340],
                         [ 0.0,-1.0, 0.0, 2.077],
                         [ 0.0, 0.0,-1.0, 0.0],
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

gBottomCam_T_AUV = np.array([[ 1.0, 0.0, 0.0, 0.0],
                            [ 0.0,-1.0, 0.0, 0.0],
                            [ 0.0, 0.0,-1.0, -0.05314],
                            [ 0.0, 0.0, 0.0, 1.0]])

Tank_T_AUV = np.array([[0.0, 0.0, 0.0, 0.0  ],
                            [ 0.0, 0.0, 0.0, 0.0  ],
                            [ 0.0, 0.0, 0.0, 0.0],
                            [ 0.0, 0.0, 0.0, 0.0  ]])

# Output video parameters #
vid_encoder = cv2.VideoWriter_fourcc(*"MJPG")
fps = 10
frame_size = (640,480)
video_out = cv2.VideoWriter('output_vid.avi', vid_encoder, fps, frame_size)

# Open Camera Feed

print("Camera sink open, Waiting for camera feed...")

if STREAM:
    cap_receive = cv2.VideoCapture('udpsrc port=5004 ! application/x-rtp,encoding_name=H264,payload=96 ! rtph264depay ! v4l2h264dec capture-io-mode=4 ! v4l2convert capture-io-mode=4  ! appsink sync=false', cv2.CAP_GSTREAMER)
else:
    cap_receive = cv2.VideoCapture('test_vid.mp4') 

print("Camera feed detected, press 'ctrl+c' to quit")

if not cap_receive.isOpened():
    print('VideoCapture not opened')
    exit()

# Program start time
t0 = time.time()

# Main Detection Loop
while True:
    t2 = time.time()
    ret,frame = cap_receive.read()

    if not ret:
        print('empty frame')
        break
    
    # Pre-tag-detection start time
    t1 = time.time()

    # Convert frame to gray and detect april tags using underwater camera calibration
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    if WATER:
        tag_results = at_detector.detect(gray, estimate_tag_pose=True, camera_params=[1127.7,1139.3,314.7352,226.3186], tag_size=0.1555)
    else:
        tag_results = at_detector.detect(gray, estimate_tag_pose=True, camera_params=[800.5335,801.2600,313.2403,231.1194], tag_size=0.1555)


    time_detect = time.time()-t1 
    #print(time_detect)

    tag_count = 0
    gAUVx = 0.0
    gAUVy = 0.0
    gAUVz = 0.0
    gAUVheading = 0.0
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
            if (True):#(abs(ptC[0]-ptA[0]) > 20) and (abs(ptC[1]-ptA[1]) > 20)):

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
                gCam_pose_t = tag.pose_t
                gCam_pose_R = tag.pose_R
                Rx = math.atan2(tag.pose_R[2,1], tag.pose_R[1,1]) * 180.0 / math.pi
                Ry = math.atan2(tag.pose_R[0,2], tag.pose_R[0,0]) * 180.0 / math.pi
                gRz = math.atan2(-gCam_pose_R[0,1], gCam_pose_R[0,0]) * 180.0 / math.pi
                Rz_rad = math.atan2(-gCam_pose_R[0,1], gCam_pose_R[0,0]) 

                # Appox camera pose rotation using only theta Z
                gR_approx = np.array([[ math.cos(Rz_rad), -math.sin(Rz_rad), 0.0],
                                      [ math.sin(Rz_rad), math.cos(Rz_rad), 0.0],
                                      [ 0.0, 0.0, 1.0]])
            
                gCam_pose_T = np.vstack((np.hstack((gR_approx, gCam_pose_t)), np.array([0.0,0.0,0.0,1.0])))

                # Calculate AUV frame relative to tank frame
                if (gCam_pose_T.any()):
                    if tag.tag_id == 0:
                        Tank_T_AUV = gTank_T_Tag0 @ np.linalg.inv(gCam_pose_T) @ gBottomCam_T_AUV
                    elif tag.tag_id == 1:
                        Tank_T_AUV = gTank_T_Tag1 @ np.linalg.inv(gCam_pose_T) @ gBottomCam_T_AUV
                    elif tag.tag_id == 2:
                        Tank_T_AUV = gTank_T_Tag2 @ np.linalg.inv(gCam_pose_T) @ gBottomCam_T_AUV
                    elif tag.tag_id == 3:
                        Tank_T_AUV = gTank_T_Tag3 @ np.linalg.inv(gCam_pose_T) @ gBottomCam_T_AUV
                    elif tag.tag_id == 4:
                        Tank_T_AUV = gTank_T_Tag4 @ np.linalg.inv(gCam_pose_T) @ gBottomCam_T_AUV
                    elif tag.tag_id == 5:
                        Tank_T_AUV = gTank_T_Tag5 @ np.linalg.inv(gCam_pose_T) @ gBottomCam_T_AUV
                    elif tag.tag_id == 12:
                        Tank_T_AUV = gTank_T_Tag12 @ np.linalg.inv(gCam_pose_T) @ gBottomCam_T_AUV
                    elif tag.tag_id == 14:
                        Tank_T_AUV = gTank_T_Tag14 @ np.linalg.inv(gCam_pose_T) @ gBottomCam_T_AUV
                    elif tag.tag_id == 16:
                        Tank_T_AUV = gTank_T_Tag16 @ np.linalg.inv(gCam_pose_T) @ gBottomCam_T_AUV
                    else:
                        print("Not a valid tag ID")
                
                print("TAG! at " + f'{(time.time()-t0):.4f}' + " s")

                if (Tank_T_AUV[0, 3] < 0.0 or Tank_T_AUV[0, 3] > 3.7 or Tank_T_AUV[1, 3] < 0.0 or Tank_T_AUV[1, 3] > 3.7 or Tank_T_AUV[2, 3] < 0.0):
                    print("Coordinates out of bounds!") #Do nothing, coordinates are out of bounds and in error
                else:
                    # Calculte AUV parameters
                    gAUVx += Tank_T_AUV[0, 3]
                    gAUVy += Tank_T_AUV[1, 3]
                    gAUVz += Tank_T_AUV[2, 3]*z_scale + z_offset
                    gAUVheading += math.atan2(Tank_T_AUV[1,0], Tank_T_AUV[0,0]) * 180.0 / math.pi
                    tag_count += 1

    # If tag detected earlier, do running avg, etc
    if tag_count > 0:
        gAUVx = gAUVx/tag_count
        gAUVy = gAUVy/tag_count
        gAUVz = gAUVz/tag_count
        gAUVheading = gAUVheading/tag_count

        gAUVx_ravg.append(gAUVx)
        gAUVy_ravg.append(gAUVy)
        gAUVz_ravg.append(gAUVz)
        gAUVHeading_ravg.append(gAUVheading)

        if len(gAUVx_ravg) > ravg_len:
            gAUVx_ravg.pop(0)
            gAUVy_ravg.pop(0)
            gAUVz_ravg.pop(0)
            gAUVHeading_ravg.pop(0)

    if len(gAUVx_ravg) > 0:
        gAUVx = sum(gAUVx_ravg)/len(gAUVx_ravg)
        gAUVy = sum(gAUVy_ravg)/len(gAUVy_ravg)
        gAUVz = sum(gAUVz_ravg)/len(gAUVz_ravg)
        gAUVheading = sum(gAUVHeading_ravg)/len(gAUVHeading_ravg)

        # Send to srauv_main
        send_over_socket(gAUVx, gAUVy, gAUVz, gAUVheading, gTID)

    # Add Pose details to frame view if Tag detected and DEBUG mode enabled
    if((gTID is not None) and DEBUG):
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
    
    # Delay video file reading to match real-stream FPS
    if (not STREAM):
        time_delay = 1/fps - (time.time() - t2)
        if time_delay > 0:
            time.sleep(time_delay)

# Once finished, release / destroy windows
print("Cleaning up...")
video_out.release()
cap_receive.release()
cv2.destroyAllWindows()