/// <summary>
/// Telemetry message model.
/// </summary>
[System.Serializable]
public class CommandModel
{   
    public string msg_type = "command";
    public string source;
    public string dest;
    public int msg_num = -1;
    public string timestamp = "";
    public string force_state = "";
    public string action = "";
    public bool can_thrust = false;
    public string thrust_type = "";
    public float[] raw_thrust; // 6
            // 0.0,            # FR: -100 to 100
            // 0.0,            # RR
            // 0.0,            # RL
            // 0.0,            # FL
            // 0.0,            # VR
            // 0.0,            # VL
    public string[] dir_thrust; // 4
            // "fwd",          # fwd , rev or ""
            // "lat_right",    # lat_right , lat_left or ""
            // "rot_left"      # rot_right , rot_left or ""
            // "up",           # up , down or ""
}