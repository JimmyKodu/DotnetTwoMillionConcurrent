using MQTTnet;
using MQTTnet.Client;
using System.Diagnostics;
using System.Text;

Console.WriteLine("=== Device Simulator for 2 Million Concurrent Devices ===");
Console.WriteLine();

// Configuration
var mqttServerUrl = Environment.GetEnvironmentVariable("MQTT_SERVER") ?? "localhost";
var mqttServerPort = int.Parse(Environment.GetEnvironmentVariable("MQTT_PORT") ?? "5000");
var totalDevices = int.Parse(Environment.GetEnvironmentVariable("TOTAL_DEVICES") ?? "2000000");
var batchSize = int.Parse(Environment.GetEnvironmentVariable("BATCH_SIZE") ?? "10000");
var reportIntervalSeconds = int.Parse(Environment.GetEnvironmentVariable("REPORT_INTERVAL") ?? "180"); // 3 minutes
var connectionsPerSecond = int.Parse(Environment.GetEnvironmentVariable("CONNECTIONS_PER_SECOND") ?? "1000");

Console.WriteLine($"Configuration:");
Console.WriteLine($"  MQTT Server: {mqttServerUrl}:{mqttServerPort}");
Console.WriteLine($"  Total Devices: {totalDevices:N0}");
Console.WriteLine($"  Batch Size: {batchSize:N0}");
Console.WriteLine($"  Report Interval: {reportIntervalSeconds} seconds");
Console.WriteLine($"  Connections/Second: {connectionsPerSecond:N0}");
Console.WriteLine();

var devices = new List<SimulatedDevice>();
var stopwatch = Stopwatch.StartNew();
var connectedCount = 0;
var errorCount = 0;

// Create and connect devices in batches
Console.WriteLine("Starting device connections...");

for (int i = 0; i < totalDevices; i++)
{
    var deviceId = $"Device_{i:D7}";
    var device = new SimulatedDevice(deviceId, mqttServerUrl, mqttServerPort, reportIntervalSeconds);
    devices.Add(device);
    
    // Connect device in background
    _ = Task.Run(async () =>
    {
        try
        {
            await device.ConnectAsync();
            Interlocked.Increment(ref connectedCount);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref errorCount);
            if (errorCount < 10) // Only log first 10 errors
            {
                Console.WriteLine($"Error connecting {deviceId}: {ex.Message}");
            }
        }
    });
    
    // Throttle connections
    if ((i + 1) % connectionsPerSecond == 0)
    {
        await Task.Delay(1000);
    }
    
    // Progress update
    if ((i + 1) % batchSize == 0)
    {
        Console.WriteLine($"Progress: {i + 1:N0}/{totalDevices:N0} devices created ({connectedCount:N0} connected, {errorCount:N0} errors) - Elapsed: {stopwatch.Elapsed:hh\\:mm\\:ss}");
    }
}

Console.WriteLine();
Console.WriteLine($"All {totalDevices:N0} devices created in {stopwatch.Elapsed:hh\\:mm\\:ss}");
Console.WriteLine($"Connected: {connectedCount:N0}, Errors: {errorCount:N0}");
Console.WriteLine();
Console.WriteLine("Devices are now reporting data every 3 minutes.");
Console.WriteLine("Press Ctrl+C to stop...");
Console.WriteLine();

// Display statistics
_ = Task.Run(async () =>
{
    while (true)
    {
        await Task.Delay(10000); // Update every 10 seconds
        var messagesSent = devices.Sum(d => d.MessagesSent);
        Console.WriteLine($"[Stats] Connected: {connectedCount:N0}, Messages Sent: {messagesSent:N0}, Errors: {errorCount:N0}");
    }
});

// Wait forever (or until Ctrl+C)
await Task.Delay(Timeout.Infinite);

class SimulatedDevice
{
    private readonly string _deviceId;
    private readonly string _serverUrl;
    private readonly int _serverPort;
    private readonly int _reportIntervalSeconds;
    private IMqttClient? _mqttClient;
    private long _messagesSent = 0;
    private readonly Random _random = new Random();
    
    public long MessagesSent => Interlocked.Read(ref _messagesSent);
    
    public SimulatedDevice(string deviceId, string serverUrl, int serverPort, int reportIntervalSeconds)
    {
        _deviceId = deviceId;
        _serverUrl = serverUrl;
        _serverPort = serverPort;
        _reportIntervalSeconds = reportIntervalSeconds;
    }
    
    public async Task ConnectAsync()
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();
        
        var options = new MqttClientOptionsBuilder()
            .WithWebSocketServer($"ws://{_serverUrl}:{_serverPort}/mqtt")
            .WithClientId(_deviceId)
            .WithCleanSession()
            .Build();
        
        await _mqttClient.ConnectAsync(options, CancellationToken.None);
        
        // Start sending data periodically
        _ = Task.Run(async () =>
        {
            // Randomize start time to avoid all devices sending at once
            await Task.Delay(_random.Next(0, _reportIntervalSeconds * 1000));
            
            while (_mqttClient?.IsConnected == true)
            {
                try
                {
                    await SendDataAsync();
                    await Task.Delay(_reportIntervalSeconds * 1000);
                }
                catch
                {
                    // Ignore errors during data sending
                }
            }
        });
    }
    
    private async Task SendDataAsync()
    {
        if (_mqttClient == null || !_mqttClient.IsConnected)
            return;
            
        var data = new
        {
            deviceId = _deviceId,
            timestamp = DateTime.UtcNow,
            temperature = 20 + _random.NextDouble() * 15, // 20-35°C
            humidity = 40 + _random.NextDouble() * 40,     // 40-80%
            status = "online"
        };
        
        var payload = System.Text.Json.JsonSerializer.Serialize(data);
        
        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"devices/{_deviceId}/telemetry")
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
            .Build();
        
        await _mqttClient.PublishAsync(message);
        Interlocked.Increment(ref _messagesSent);
    }
}
