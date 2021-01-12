import Timestamp

def make(source, dest = ''):
    cmd = {
        "source" : source,
        "dest" : dest,
        "msgNum" : 0,
        "msgType" : "command",
        "timestamp" : Timestamp.make(),
        "thrustFwd" : 0.0,          # -10.0 to 10.0
        "thrustRight" : 0.0,        # -10.0 to 10.0
        "thrustRear" : 0.0,         # -10.0 to 10.0
        "thrustLeft" : 0.0,         # -10.0 to 10.0
        "vertA" : 0.0,              # -10.0 to 10.0
        "vertB" : 0.0               # -10.0 to 10.0
    }

    return cmd