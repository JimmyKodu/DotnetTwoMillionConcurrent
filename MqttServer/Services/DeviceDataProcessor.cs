using System.Threading.Channels;

namespace MqttServer.Services;

public class DeviceDataProcessor : BackgroundService
{
    private readonly Channel<DeviceData> _channel;
    private readonly StatisticsService _statisticsService;
    private readonly ILogger<DeviceDataProcessor> _logger;

    public DeviceDataProcessor(
        Channel<DeviceData> channel,
        StatisticsService statisticsService,
        ILogger<DeviceDataProcessor> logger)
    {
        _channel = channel;
        _statisticsService = statisticsService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Device Data Processor started");

        await foreach (var deviceData in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                // Process the device data
                // In a real application, you would store this in a database
                // For this demo, we just log and count
                _statisticsService.IncrementMessagesProcessed();

                // Log sample messages (every 10000th message to avoid flooding)
                if (_statisticsService.MessagesProcessed % 10000 == 0)
                {
                    _logger.LogInformation(
                        "Processed {Count} messages. Device: {DeviceId}, Payload: {Payload}",
                        _statisticsService.MessagesProcessed,
                        deviceData.DeviceId,
                        deviceData.Payload);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing device data from {DeviceId}", deviceData.DeviceId);
            }
        }
    }
}
