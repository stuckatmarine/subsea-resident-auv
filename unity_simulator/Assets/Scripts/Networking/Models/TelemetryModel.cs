/// <summary>
/// Telemetry message model.
/// </summary>
using System.Collections.Generic;

[System.Serializable]
public class TelemetryModel
{
    public string msg_type;
    public string timestamp;
    public string source;
    public string dest;
    public int msg_num;
    public string state;
    public bool[] thrust_enabled;
    public string headlight_setting;
    public float pos_x;
    public float pos_y;
    public float pos_z;
    public float depth;
    public float alt;
    public ImuModel imu_dict;
    public TagModel tag_dict;
    public float[] thrust_values;
    public float[] dist_values;
}
