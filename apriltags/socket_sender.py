## Socket to send extimated pos to srauv_main.py's internal socket
import socket
import time
import json

socket_connected = False
srauv_address = ("localhost", 7003)
last_tx_time_s = 0
socket_send_interval_s = 0.200
msg_num = -1
msg = {
    "source" : "tag_detect",
    "msg_num" : msg_num,
    "msg_type" : "position",
    "timestamp" : time.strftime("%Y-%m-%d %H:%M.%S"),
    "pos_x" : -0.1,
    "pos_y" : -0.2,
    "pos_z" : -0.3,
    "heading" : 0.4,
    "tag_id" : -1
}

try:
    srauv_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # internet, udp
    socket_connected = True
    print(f"socket_sender forwarding to internal socket {srauv_address}")
except socket.error:
    print(f"socket_sender failed to create socket {srauv_address}")
   
def send_over_socket(x: float, y: float, z: float, h: float, t: int):
    global msg_num, last_tx_time_s
    if not socket_connected or time.time() - last_tx_time_s < socket_send_interval_s:
        return

    msg_num += 1
    last_tx_time_s = time.time()
    try:
        # Send position data to srauv_main
        msg["pos_x"] = x
        msg["pos_y"] = y
        msg["pos_z"] = z
        msg["heading"] = h
        msg["tag_id"] = t
        print(f"socket_sender tx msg:{msg}")
        srauv_socket.sendto(json.dumps(msg).encode("utf-8"), srauv_address)
        # Get response from srauv_main. Log it
        resp, addr = srauv_socket.recvfrom(4096)
        print(f"socket_sender rx utf8 resp:{resp}")
    except socket.error as e:
        print(f"socket_sender failed sendOverSocket {srauv_address}, err:{e}")
    except Exception as ex:
        print(f"socket_sender ex:{ex}")
## End socket stuff