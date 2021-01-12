import socket
import sys

# Create a TCP/IP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

# Bind the socket to the port
server_address = ('localhost', 8001)
print(f"{sys.stderr}, 'starting up on %s port %s' - {server_address}")
sock.bind(server_address)

while True:
    print(f"{sys.stderr}, '\nwaiting to receive message' - {server_address}")
    data, address = sock.recvfrom(4096)
    
    print(f"{sys.stderr}, 'received %s bytes from %s' - {server_address}")
    print(f"{sys.stderr}, 'datas' - {data}")
    
    if data:
        sent = sock.sendto(data, address)
        print(f"{sys.stderr}, 'sent %s bytes back to %s' - {server_address}")