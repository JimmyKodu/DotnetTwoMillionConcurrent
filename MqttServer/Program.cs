using MQTTnet.AspNetCore;
using MQTTnet.Server;
using MqttServer.Services;
using System.Threading.Channels;
using System.Buffers;
using System.Text;
using MQTTnet;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel for high connection count
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxConcurrentConnections = null; // Unlimited
    serverOptions.Limits.MaxConcurrentUpgradedConnections = null; // Unlimited
    serverOptions.ListenAnyIP(5000); // Bind to port 5000 on all interfaces
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure channel for device data processing
builder.Services.AddSingleton(Channel.CreateUnbounded<DeviceData>());

// Add MQTT server
builder.Services.AddHostedMqttServer(mqttServer =>
{
    mqttServer.WithoutDefaultEndpoint();
})
.AddMqttConnectionHandler()
.AddConnections();

// Add device data processor service
builder.Services.AddSingleton<DeviceDataProcessor>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<DeviceDataProcessor>());

// Add statistics service
builder.Services.AddSingleton<StatisticsService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Don't use HTTPS redirection for MQTT/WebSocket connections
// app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

// Map MQTT endpoint
app.MapMqtt("/mqtt");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Configure MQTT server
var mqttServer = app.Services.GetRequiredService<MQTTnet.Server.MqttServer>();
var statisticsService = app.Services.GetRequiredService<StatisticsService>();
var channel = app.Services.GetRequiredService<Channel<DeviceData>>();

mqttServer.ValidatingConnectionAsync += async e =>
{
    // Accept all connections for this demo
    e.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;
    await Task.CompletedTask;
};

mqttServer.ClientConnectedAsync += async e =>
{
    statisticsService.IncrementConnectedDevices();
    Console.WriteLine($"Client connected: {e.ClientId} - Total: {statisticsService.ConnectedDevices}");
    await Task.CompletedTask;
};

mqttServer.ClientDisconnectedAsync += async e =>
{
    statisticsService.DecrementConnectedDevices();
    Console.WriteLine($"Client disconnected: {e.ClientId} - Total: {statisticsService.ConnectedDevices}");
    await Task.CompletedTask;
};

mqttServer.InterceptingPublishAsync += async e =>
{
    // Extract payload from byte array in v4
    string payload = string.Empty;
    if (e.ApplicationMessage.Payload != null && e.ApplicationMessage.Payload.Length > 0)
    {
        payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
    }
    
    var deviceData = new DeviceData
    {
        DeviceId = e.ClientId,
        Topic = e.ApplicationMessage.Topic,
        Payload = payload,
        Timestamp = DateTime.UtcNow
    };
    
    await channel.Writer.WriteAsync(deviceData);
    statisticsService.IncrementMessagesReceived();
};

app.Run();
