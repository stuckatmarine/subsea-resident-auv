/// <summary>
/// Telemetry message model.
/// </summary>
[System.Serializable]
public class TelemetryModel
{
    public string source;
    public int msgNum;
    public string msgType;
    public long timestamp;
    public float depth;
    public float alt;
    // Keeping to primitives for ease of json parsing troubleshooting
    public float northDist, eastDist, southDist, westDist;
    public float posX;
    public float posY;
    public float posZ;
    public float cageDist;
    public float tree1Dist;
    public float tree2Dist;
}
