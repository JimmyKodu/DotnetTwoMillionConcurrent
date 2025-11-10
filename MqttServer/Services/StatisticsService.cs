using System.Threading;

namespace MqttServer.Services;

public class StatisticsService
{
    private long _connectedDevices = 0;
    private long _messagesReceived = 0;
    private long _messagesProcessed = 0;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public long ConnectedDevices => Interlocked.Read(ref _connectedDevices);
    public long MessagesReceived => Interlocked.Read(ref _messagesReceived);
    public long MessagesProcessed => Interlocked.Read(ref _messagesProcessed);
    public TimeSpan Uptime => DateTime.UtcNow - _startTime;

    public void IncrementConnectedDevices()
    {
        Interlocked.Increment(ref _connectedDevices);
    }

    public void DecrementConnectedDevices()
    {
        Interlocked.Decrement(ref _connectedDevices);
    }

    public void IncrementMessagesReceived()
    {
        Interlocked.Increment(ref _messagesReceived);
    }

    public void IncrementMessagesProcessed()
    {
        Interlocked.Increment(ref _messagesProcessed);
    }

    public double GetMessagesPerSecond()
    {
        var uptime = Uptime.TotalSeconds;
        return uptime > 0 ? MessagesReceived / uptime : 0;
    }
}
