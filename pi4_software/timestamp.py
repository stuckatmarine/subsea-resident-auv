import time
from datetime import datetime

def now_string():
    return datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]

def now_int_ms():
    return int(round(time.time() * 1000))