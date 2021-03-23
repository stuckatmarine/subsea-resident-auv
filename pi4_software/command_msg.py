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
        "headlight_setting": "",
        "reset_to_first_waypoint": False,
        "pos_x": 1.0,   # for fly by sim pos
        "pos_y": 1.1,   # for fly by sim pos
        "pos_z": 1.2,   # for fly by sim pos
        "vel_x" : 11.1,          # m / s , interpolated
        "vel_y" : 12.1,
        "vel_z" : 11.1,
        "heading": 1.3, # for fly by sim pos
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
        "can_thrust" : False,
        "thrust_type": "",  # raw_thrust or dir_thrust
        "raw_thrust":[      # clockwise from front of vehicle, lats then verts
            0.0,            # FR
            0.0,            # RR
            0.0,            # RL
            0.0,            # FL
            0.0,            # VR
            0.0,            # VL
        ],
        "dir_thrust":[      # maintain size of 4
            "_",          # fwd , rev or "_"
            "_",    # lat_right , lat_left or "_"
            "_"      # rot_right , rot_left or "_"
            "_",           # up , down or "_"
        ]
    }

    return cmd