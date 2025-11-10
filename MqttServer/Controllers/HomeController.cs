using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MqttServer.Models;
using MqttServer.Services;

namespace MqttServer.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly StatisticsService _statisticsService;

    public HomeController(ILogger<HomeController> logger, StatisticsService statisticsService)
    {
        _logger = logger;
        _statisticsService = statisticsService;
    }

    public IActionResult Index()
    {
        var model = new StatisticsViewModel
        {
            ConnectedDevices = _statisticsService.ConnectedDevices,
            MessagesReceived = _statisticsService.MessagesReceived,
            MessagesProcessed = _statisticsService.MessagesProcessed,
            Uptime = _statisticsService.Uptime,
            MessagesPerSecond = _statisticsService.GetMessagesPerSecond()
        };
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
