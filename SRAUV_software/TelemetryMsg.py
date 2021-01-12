import Timestamp

def make(source, dest = ''):
    tel = {
        "source" : source,
        "dest" : dest,
        "msgNum" : 0,
        "state" : "none",
        "msgType" : "telemetry",
        "timestamp" : Timestamp.make(),
        "fwdDist" : 6.1,            # sensor val
        "rightDist" : 7.1,          # sensor val
        "rearDist" : 8.1,           # sensor val
        "leftDist" : 0.1,           # sensor val
        "depth" : 10.1,             # sensor val
        "alt" : 11.1,               # sensor val
        "heading" : 315.1,          # sensor val
        "posX" : 11.1,              # navigation est
        "posY" : 12.1,              # navigation est
        "posZ" : 11.1,              # navigation est  
        "assetDistances" : 
        {
            "cage" : 12.1,
            "tree1" : 13.1,
            "tree2" : 14.1
        }
    }

    return tel