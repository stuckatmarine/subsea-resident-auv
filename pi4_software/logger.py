#!/usr/bin/env python
#   logger.py
#   Custom instantiatable logger, easier to use for
#   multiprocess/threaded individual log files

import logging
formatter = logging.Formatter('%(asctime)s %(levelname)s %(message)s')

def setup_logger(name, log_file, std_out=False, level=logging.INFO):
    """To setup as many loggers as you want"""

    handler = logging.FileHandler(log_file)        
    handler.setFormatter(formatter)

    logger = logging.getLogger(name)
    logger.setLevel(level)
    logger.addHandler(handler)

    if (std_out):
        logging.getLogger().addHandler(logging.StreamHandler())

    return logger