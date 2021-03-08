import socket
import sys

try:
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect(("localhost", 7001))
    sock.send(str("test msg").encode("utf-8"))
    print(sock.recvfrom(4096)[0])
    sock.close()

except Exception as e:
    print(f"err:{e}")