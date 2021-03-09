import socket
import sys
import time
import json

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
    print ("socket closed")

while True:
    try:
        client, address = sock.accept()
        print(f"< addr:{address} client:{client}")
        data = client.recvfrom(65536)[0].decode("utf-8")
        response = json.loads(data)
        print(f"< data:{data}")
        client.sendto(default_response_bytes, address)

    except KeyboardInterrupt:
        print("Exiting via interrupt")
        close()
        break

    except socket.timeout as e:
        print(f"e:{e}")
        close()
        break

    except Exception as ex:
        print(f"ex:{ex}")
        close()
        break
    time.sleep(0.001)
        