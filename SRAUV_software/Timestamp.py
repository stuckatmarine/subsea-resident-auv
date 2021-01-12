import time
from datetime import datetime

def make():
    return datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S.%f")[:-3]