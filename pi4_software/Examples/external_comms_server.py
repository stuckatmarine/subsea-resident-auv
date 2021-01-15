"""Communicate from server to client side."""
# EE clone
import socket
import sys
import threading
import queue
from SRAUV_settings import SETTINGS

send = queue.Queue()
threads = []
stop_events = []

srauv_address = (SETTINGS['srauv_ip'], SETTINGS['srauv_port'])
topside_address = (SETTINGS['topside_ip'], SETTINGS['topside_port'])

# Try opening a socket for communication
try:
    srauv_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) # internet, udp
    srauv_socket.settimeout(3)
except socket.error:
    print("Failed To Create Socket")
    sys.exit()
# Bind the ip and port of the raspi to the socket and loop coms
srauv_socket.bind(srauv_address)


def send_data():
    """Send data to topsides."""
    global srauv_socket, send
    sendData = send.get()
    while sendData != "exit":
        srauv_socket.sendto(sendData.encode('utf-8'), topside_address)
        print("sent response: " + sendData + " to " + str(topside_address))
        sendData = send.get()
    srauv_socket.sendto(sendData.encode('utf-8'), topside_address)
    print("sent response: " + sendData + " to " + str(topside_address))


def receive_data():
    """Receive data from topsides."""
    global threads
    while True:
        try:
            data, addr = srauv_socket.recvfrom(1024)
            data = data.decode("utf-8")
        except KeyboardInterrupt:
            sys.exit()
        except socket.timeout as e:
            for i in range(0, 8):
                sys.argv.append(i)
                sys.argv.append(0)
                try:
                    exec(open("fControl.py").read())
                except Exception as e:
                    response = str(e)
                    # print(response)
                del sys.argv[1:]
            continue
        if data == "exit":
            send.put("exit")
            for event in stop_events:
                event.set()
            break
        print(data)
        # Identify the file name and arguments
        nextSpace = data.find(".py") + 3
        file = data[0:nextSpace]
        lastSpace = nextSpace + 1
        nextSpace = data.find(" ", lastSpace)
        while nextSpace != -1:
            sys.argv.append(data[lastSpace:nextSpace])
            lastSpace = nextSpace + 1
            nextSpace = data.find(" ", lastSpace)
        sys.argv.append(data[lastSpace:])

        # Setup threading for receiving data
        flag = threading.Event()
        stop = threading.Event()
        threads.append(threading.Thread(target=execute_data, args=(file, flag, stop,)))
        stop_events.append(stop)
        threads[len(threads) - 1].start()
        flag.wait()
        del sys.argv[1:]
        threads = [i for i in threads if i.isAlive()]


def execute_data(file, flag, stop):
    try:
        exec(open(file).read(), {"send": send, "flag": flag, "stop": stop})
    except Exception as e:
        send.put(str(e))
        flag.set()


# Setup threading for receiving data
threads.append(threading.Thread(target=send_data))

if __name__ == "__main__":
    threads[0].start()
    receive_data()