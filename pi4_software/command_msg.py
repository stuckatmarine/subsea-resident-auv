import timestamp

def make(source, dest = ''):
    cmd = {
        "source" : source,
        "dest" : dest,
        "msg_num" : -1,
        "msg_type" : "command",
        "force_state": "",
        "action": "",
        "timestamp" : timestamp.now_string(),
        "can_thrust" : False,
        "thrust_fwd" : 0.0,          # -10.0 to 10.0
        "thrust_right" : 0.0,        # -10.0 to 10.0
        "thrus_rear" : 0.0,         # -10.0 to 10.0
        "thrust_left" : 0.0,         # -10.0 to 10.0
        "thrust_v_right" : 0.0,              # -10.0 to 10.0
        "thrust_v_left" : 0.0               # -10.0 to 10.0
    }

    return cmd