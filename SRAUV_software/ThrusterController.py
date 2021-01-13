import threading
import time
import logging

class thruster_thread(threading.Thread):
    def __init__(self, config, id, data_arr):
        threading.Thread.__init__(self)
        self.config = config
        self.id = id
        self.thrust_arr = data_arr
        self.kill_received = False
        self.thrust_interval_s = config["thrust_interval_s"]
        self.deadman_timeout_s = config["deadman_timeout_s"]
        self.last_heartbeat = 0
        self.thrust_enabled = False

    
    def apply_thrust(self):
        if self.thrust_enabled == False:
            return

        # check if deadman has timedout
        if time.time() - self.last_heartbeat >= self.deadman_timeout_s:
            self.thrust_enabled = False
            logging.info(f"Thruster_id:{self.id} deadman expired, thrust_enabled:{self.thrust_enabled}")
        else:
            # update with thrust applying code
            logging.info(f"Thruster_id:{self.id} thrust_value:{self.thrust_arr[self.id]}")

    def do_thrust(self, enable):
        self.thrust_enabled = enable
        self.last_heartbeat = time.time()

    def run(self):
        while not self.kill_received:
            start_time = time.time()
            self.apply_thrust()
            time.sleep(self.thrust_interval_s - ((time.time() - start_time) % self.thrust_interval_s))
    