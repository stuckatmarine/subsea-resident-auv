import socket
import sys
import threading
import time
import queue
import json
from datetime import datetime

import logger

# multiprocess usage example
# def setup_middleware_process(mw_config, tel):
#     logger.info(f'state:{tel["state"]} MSG:Creating middleware process')
#     mw_proc.append(multiprocessing.Process(target=middleware_server.MWThread, args=(mw_config, tel, )))
#     mw_proc[0].start()

# server_address = ('localhost', 8001)
TEST_RESPONSE_STR = "test_response"
TEST_RESPONSE_DATA = TEST_RESPONSE_STR.encode('utf-8')

log_filename = str(f'Logs/{datetime.now().strftime("MW--%m-%d-%Y_%H-%M-%S")}.log')
logger = logger.setup_logger("middleware", log_filename)

# class MWThread(threading.Thread):
class MWThread():
    def __init__(self, config, tel):
        self.kill_received = False
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.address = (config["mw_ip"], config["mw_port"])
        self.sock.bind(self.address)
        self.tel = tel
        self.default_response = str('').encode('utf-8')
        self.inbound_queue_dict = {}
        # self.sock.settimeout(10)

        logger.info(f"starting middleware, address:{self.address}")
        self.receive_messages() # blocks
        logger.info(f"stopping middleware, address:{self.address}")


    def receive_messages(self):
        while not self.kill_received:
            try:
                data, address = self.sock.recvfrom(4096)
                
                if data:
                    logger.info(f"addr:{address} data:{data}")
                    json_data = {"source": ''}

                    if not self.inbound_queue_dict():
                        json_data = json.loads(self.inbound_queue.get().decode('utf8').replace("'", '"'))

                    if json_data["source"] == "vehicle":
                        self.sock.sendto(json_data, address)
                    else:
                        self.sock.sendto(self.default_response, address)

            except KeyboardInterrupt:
                logger.warning("Exiting via interrupt")
                break

            except socket.timeout as e:
                logger.warning(f"Exiting via socket timeout e:{e}")
                break

            time.sleep(0.001)