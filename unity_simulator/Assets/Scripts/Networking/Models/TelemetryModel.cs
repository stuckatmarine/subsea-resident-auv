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
    public float heading;
    public float vel_x = 0.0f;
    public float vel_y = 0.0f;
    public float vel_z = 0.0f;
    public float depth;
    public float alt;
    public string mission_msg;
    public float target_pos_x;
    public float target_pos_y;
    public float target_pos_z;
    public float target_heading_to;
    public ImuModel imu_dict;
    public TagModel tag_dict;
    public PressureModel depth_sensor_dict;
    public float[] thrust_values;
    public float[] dist_values;
}
