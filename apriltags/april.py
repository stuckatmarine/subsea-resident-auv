from pupil_apriltags import Detector
import cv2
import time

t1 = time.time()
img = cv2.imread("DockTag2.PNG")
print(img.shape)
gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)

at_detector = Detector(families='tag36h11',
                       nthreads=1,
                       quad_decimate=2.0,
                       quad_sigma=0.0,
                       refine_edges=1,
                       decode_sharpening=0.25,
                       debug=0)

results = at_detector.detect(gray, estimate_tag_pose=True, camera_params=[800.5335,801.2600,313.2403,231.1194], tag_size=0.20)

for r in results:
    # extract the bounding box (x, y)-coordinates for the AprilTag
    # and convert each of the (x, y)-coordinate pairs to integers
    (ptA, ptB, ptC, ptD) = r.corners
    ptB = (int(ptB[0]), int(ptB[1]))
    ptC = (int(ptC[0]), int(ptC[1]))
    ptD = (int(ptD[0]), int(ptD[1]))
    ptA = (int(ptA[0]), int(ptA[1]))
    # draw the bounding box of the AprilTag detection
    cv2.line(img, ptA, ptB, (0, 255, 0), 2)
    cv2.line(img, ptB, ptC, (0, 255, 0), 2)
    cv2.line(img, ptC, ptD, (0, 255, 0), 2)
    cv2.line(img, ptD, ptA, (0, 255, 0), 2)
    # draw the center (x, y)-coordinates of the AprilTag
    (cX, cY) = (int(r.center[0]), int(r.center[1]))
    cv2.circle(img, (cX, cY), 5, (0, 0, 255), -1)

print(time.time()-t1)
print(1/30)
# show the output img after AprilTag detection
cv2.imshow("img", img)
cv2.waitKey(0)
