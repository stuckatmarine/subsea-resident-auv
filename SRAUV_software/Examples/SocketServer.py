import socket
import sys

send_response = True
default_response_str = ''
default_response_bytes = default_response_str.encode('utf-8')

# Create a TCP/IP socket
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
sock.settimeout(10)

# Bind the socket to the port
server_address = ('localhost', 8001)
print(f"{sys.stderr}, 'starting up on %s port %s' - {server_address}")
sock.bind(server_address)

while True:
    try:
        data, address = sock.recvfrom(4096)
        print(f"received %s bytes from {address}")
        
        if data:
            print(f"data:{data}")

            if send_response:
                sent = sock.sendto(default_response_bytes, address)

    except KeyboardInterrupt:
        print("Exiting via interrupt")
        sys.exit()

    except socket.timeout as e:
        sys.exit()
        