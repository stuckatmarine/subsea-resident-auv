/// <summary>
/// Telemetry message model.
/// </summary>
[System.Serializable]
public class TelemetryModel
{
    public string msg_type;
    public string source;
    public string dest;
    public int msg_num;
    public string state;
    public string timestamp;
    public float fwd_dist, right_dist, rear_dist, left_dist;
    public float depth;
    public float alt;
    public float heading;
    // Keeping to primitives for ease of json parsing troubleshooting
    public float pos_x, pos_y, pos_z;
    public float vel_x, vel_y, vel_z;
    public float target_pos_x, target_pos_y, target_pos_z;

    // public float dockDist;
    // public float dockDistX;
    // public float dockDistY;
    // public float dockDistZ;
    // public float tree1Dist;
    // public float tree1DistX;
    // public float tree1DistY;
    // public float tree1DistZ;
    // public float tree2Dist;
    // public float tree2DistX;
    // public float tree2DistY;
    // public float tree2DistZ;
    // public float tree3Dist;
    // public float tree3DistX;
    // public float tree3DistY;
    // public float tree3DistZ;
}
