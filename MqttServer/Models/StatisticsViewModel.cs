namespace MqttServer.Models;

public class StatisticsViewModel
{
    public long ConnectedDevices { get; set; }
    public long MessagesReceived { get; set; }
    public long MessagesProcessed { get; set; }
    public TimeSpan Uptime { get; set; }
    public double MessagesPerSecond { get; set; }
}
