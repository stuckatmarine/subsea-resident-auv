/// <summary>
/// Telemetry message model.
/// </summary>
using System.Collections.Generic;

[System.Serializable]
public class TagModel
{
    public float pos_x;
    public float pos_y;
    public float pos_z;
    public float vel_x;
    public float vel_y;
    public float vel_z;
    public float heading;
    public int tag_id;
    public int recv_time;
    public int[] recent;
}
