import timestamp

def make(source, dest = ''):
    tel = {
        "source" : source,
        "dest" : dest,
        "msg_num" : -1,
        "state" : "none",
        "msg_type" : "telemetry",
        "timestamp" : timestamp.now_string(),
        "fwd_dist" : 6.1,            # sensor val
        "right_dist" : 7.1,          # sensor val
        "rear_dist" : 8.1,           # sensor val
        "left_dist" : 0.1,           # sensor val
        "depth" : 10.1,             # sensor val
        "alt" : 11.1,               # sensor val
        "heading" : 315.1,          # sensor val
        "pos_x" : 11.1,              # navigation est
        "pos_y" : 12.1,              # navigation est
        "pos_z" : 11.1,              # navigation est  
        "vel_x" : 11.1,              # sensor val
        "vel_y" : 12.1,              # sensor val
        "vel_z" : 11.1,              # sensor val  
        "asset_distances" : 
        {
            "cage" : 12.1,
            "tree1" : 13.1,
            "tree2" : 14.1
        },
        "target_distance" : 
        {
            "x" : 12.2,
            "y" : 13.2,
            "z" : 14.2,
            "h" : 15.2
        }
    }

    return tel