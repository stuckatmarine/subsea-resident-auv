/// <summary>
/// Telemetry message model.
/// </summary>
[System.Serializable]
public class TelemetryModel
{
    public string source;
    public int msgNum;
    public string msgType;
    public string timestamp;
    public string depth;
    public string alt;
    // Keeping to primitives for ease of json parsing troubleshooting
    public string fwdDist, rightDist, rearDist, leftDist;
    public float posX;
    public float posY;
    public float posZ;
    public float heading;
    public float dockDist;
    public float dockDistX;
    public float dockDistY;
    public float dockDistZ;
    public float tree1Dist;
    public float tree1DistX;
    public float tree1DistY;
    public float tree1DistZ;
    public float tree2Dist;
    public float tree2DistX;
    public float tree2DistY;
    public float tree2DistZ;
    public float tree3Dist;
    public float tree3DistX;
    public float tree3DistY;
    public float tree3DistZ;
}
