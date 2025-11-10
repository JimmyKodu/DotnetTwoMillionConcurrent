namespace MqttServer.Services;

public class DeviceData
{
    public string DeviceId { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
