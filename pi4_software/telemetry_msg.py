import timestamp

def make(source, dest = ''):
    tel = {
        "msg_type" : "telemetry",
        "timestamp" : timestamp.now_string(),
        "source" : source,
        "dest" : dest,
        "msg_num" : -1,
        "state" : "none",
        "thrust_enabled" : [False],
        "headlight_setting" : "low",
        "pos_x" : 0.1,              # navigation est
        "pos_y" : 2.1,              # navigation est
        "pos_z" : 1.1,              # navigation est 
        "depth" : 0.0,              # sensor val
        "alt" : 0.0,                # sensor val
        "heading" : 0.0,                # sensor val
        "vel_x" : 11.1,          # m / s , interpolated
        "vel_y" : 12.1,
        "vel_z" : 11.1,
        "target_pos_x": 0.0,
        "target_pos_y": 0.0,
        "target_pos_z": 0.0,
        "target_heading_to": 0.0,
        "imu_dict":{
            "heading" : 315.1,       # deg
            "roll" : 315.2,          
            "pitch" : 315.3,
            "gyro_x" : 11.1,         # deg / s
            "gyro_y" : 11.1,
            "gyro_z" : 11.1,
            "vel_x" : 11.1,          # m / s , interpolated
            "vel_y" : 12.1,
            "vel_z" : 11.1,
            "linear_accel_x" : 11.1, # m/ s^2
            "linear_accel_y" : 11.1,     
            "linear_accel_z" : 11.1,     
            "accel_x" : 11.1,     
            "accel_x" : 11.1,     
            "accel_x" : 11.1,     
            "magnetic_x" : 11.1,     # uTesla
            "magnetic_y" : 11.1,     
            "magnetic_z" : 11.1,     
        },
        "tag_dict":{
            "pos_x": 1.0,
            "pos_y": 1.1,
            "pos_z": 1.2,
            "heading": 1.3,
            "tag_id": -1
        },
        "thrust_values":[      # clockwise from front of vehicle, lats then verts
            0.0,            # FR:
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
            0.0,            # Depth
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