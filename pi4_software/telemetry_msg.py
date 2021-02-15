import timestamp

def make(source, dest = ''):
    tel = {
        "msg_type" : "telemetry",
        "source" : source,
        "dest" : dest,
        "msg_num" : -1,
        "state" : "none",
        "timestamp" : timestamp.now_string(),
        "fwd_dist" : 0.1,            # sensor val
        "right_dist" : 1.1,          # sensor val
        "rear_dist" : 2.1,           # sensor val
        "left_dist" : 0.4,           # sensor val
        "depth" : 1.3,             # sensor val
        "alt" : 0.5,               # sensor val
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
        "raw_thrust":[      # clockwise from front of vehicle, lats then verts
            0.0,            # FR: -100 to 100
            0.0,            # RR
            0.0,            # RL
            0.0,            # FL
            0.0,            # VR
            0.0,            # VL
        ],
        # "asset_distances" : 
        # {
        #     "cage" : 12.1,
        #     "tree1" : 13.1,
        #     "tree2" : 14.1
        # },
    }

    return tel