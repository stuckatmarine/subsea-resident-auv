import timestamp

def make(source, dest = ''):
    tel = {
        "timestamp" : timestamp.now_string(),
        "msg_type" : "telemetry",
        "source" : source,
        "dest" : dest,
        "msg_num" : -1,
        "state" : "none",
        "thrust_enabled" : [False],
        "depth" : 0.0,             # sensor val
        "alt" : 0.0,               # sensor val
        "imu_dict":{
            "heading" : 315.1,          # sensor val
            "pos_x" : 0.1,              # navigation est
            "pos_y" : 2.1,              # navigation est
            "pos_z" : 1.1,              # navigation est  
            "vel_x" : 11.1,              # sensor val
            "vel_y" : 12.1,              # sensor val
            "vel_z" : 11.1,              # sensor val  
            "target_pos_x" : 12.1,
            "target_pos_y" : 12.2,
            "target_pos_z" : 12.3,
        },
        "thrust_values":[      # clockwise from front of vehicle, lats then verts
            0.0,            # FR: -100 to 100
            0.0,            # RR
            0.0,            # RL
            0.0,            # FL
            0.0,            # VR
            0.0,            # VL
        ],
        "dist_values":[     # Clockwise from front
            0.0,            # Fwd
            0.0,            # Right
            0.0,            # Rear
            0.0,            # Left
            0.0             # Altitude
        ]
        # "asset_distances" : 
        # {
        #     "cage" : 12.1,
        #     "tree1" : 13.1,
        #     "tree2" : 14.1
        # },
    }

    return tel