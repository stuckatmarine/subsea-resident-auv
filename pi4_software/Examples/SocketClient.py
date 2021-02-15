import socket
import sys


# Create a UDP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

server_address = ('localhost', 8001)
message = str.encode('This is the message.  It will be repeated.')

try:

    # Send data
    print(f"{sys.stderr}, 'sending msg' - {message}")
    sent = sock.sendto(message, server_address)

    # Receive response
    print(f"{sys.stderr}, 'waiting to receive'")
    data, server = sock.recvfrom(4096)
    print(f"{sys.stderr}, 'received' - {data}")

finally:
    print(f"{sys.stderr}, 'closing socket'")
    sock.close()