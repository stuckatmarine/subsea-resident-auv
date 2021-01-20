/// <summary>
/// Telemetry message model.
/// </summary>
[System.Serializable]
public class CommandModel
{
    public string source;
    public int msgNum;
    public string msgType;
    public string timestamp;
    public float thrustFwd;
    public float thrustRight;
    public float thrustRear;
    public float thrustLeft;
    public float vertA;
    public float vertB;
}