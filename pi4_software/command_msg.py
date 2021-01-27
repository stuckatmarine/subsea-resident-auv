import timestamp

def make(source, dest = ''):
    cmd = {
        "msg_type" : "command",
        "source" : source,
        "dest" : dest,
        "msg_num" : -1,
        "timestamp" : timestamp.now_string(),
        "force_state": "",
        "action": "",
        "can_thrust" : False,
        "thrust_type": "",  # raw_thrust or dir_thrust
        "raw_thrust":[      # clockwise from front of vehicle, lats then verts
            0.0,            # FR: -100 to 100
            0.0,            # RR
            0.0,            # RL
            0.0,            # FL
            0.0,            # VR
            0.0,            # VL
        ],
        "dir_thrust":[      # maintain size of 4
            "fwd",          # fwd , rev or ""
            "lat_right",    # lat_right , lat_left or ""
            "rot_left"      # rot_right , rot_left or ""
            "up",           # up , down or ""
        ]
    }

    return cmd