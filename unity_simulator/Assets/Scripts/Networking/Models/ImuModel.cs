/// <summary>
/// Telemetry message model.
/// </summary>
using System.Collections.Generic;

[System.Serializable]
public class ImuModel
{
    public float heading;
    public float roll;
    public float pitch;
    public float gyro_x;
    public float gyro_y;
    public float gyro_z;
    public float vel_x;
    public float vel_y;
    public float vel_z;
    public float linear_accel_x;
    public float linear_accel_y;
    public float linear_accel_z;
    public float accel_x;
    public float accel_y;
    public float accel_z;
    public float magnetic_x;
    public float magnetic_y;
    public float magnetic_z;
}
