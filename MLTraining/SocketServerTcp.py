import socket
import sys
import time
import json
import base64
import pandas as pd

from datetime import datetime


df = pd.DataFrame(index=list('index'),
                  columns=['index', 'pos_x', 'pos_y', 'pos_z', 'heading', 
                           'gyro_y', 'vel_x', 'vel_y', 'vel_z'])
timestamp = datetime.now().strftime("%m-%d-%Y_%H-%M")
path = f'dataRecording/{timestamp}'
import os
os.mkdir(path)

send_response = True
default_response_str = 'dflt response'
default_response_bytes = default_response_str.encode('utf-8')

# Create a TCP/IP socket
server_address = ('localhost', 7001)
sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
sock.bind(server_address)
sock.listen(5)
print(f"{sys.stderr}, 'starting up on %s port %s' - {server_address}")

def close():
    sock.shutdown(socket.SHUT_RDWR)
    sock.close()
    print("socket closed")
    df.to_csv(f'{path}/data.csv', index=False)
    print("data saved")


try:
    while True:
        client, address = sock.accept()
        print(f"Connected to addr: {address}")
        data = client.recvfrom(65536)[0].decode("utf-8")

        msg = json.loads(data)
        if msg:
            df = df.append({
                'index': msg['msg_num'],
                'pos_x': msg['pos_x'],
                'pos_y': msg['pos_y'],
                'pos_z': msg['pos_z'],
                'heading': msg['imu_dict']['heading'],
                'gyro_y': msg['imu_dict']['gyro_y'],
                'vel_x': msg['imu_dict']['vel_x'],
                'vel_y': msg['imu_dict']['vel_y'],
                'vel_z': msg['imu_dict']['vel_z']},
                ignore_index=True)
            img = base64.b64decode(msg['state'])
            with open(f"{path}/{msg['msg_num']}.jpg", 'wb') as f:
                f.write(img)
            
            if msg['msg_num'] % 100 == 0:
               df.to_csv(f'{path}/data.csv', index=False)
               print("data saved")
    
            print(f"Saved img {msg['msg_num']}")
        # print(f"< data:{data}")
        client.sendto(default_response_bytes, address)
        time.sleep(0.001)

except KeyboardInterrupt:
    print("Exiting via interrupt")
    close()

except socket.timeout as e:
    print(f"e:{e}")
    close()

except Exception as ex:
    print(f"ex:{ex}")
    close()

    
