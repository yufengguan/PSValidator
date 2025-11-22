using Microsoft.AspNetCore.Mvc;

namespace PromoStandards.Validator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceListController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ServiceListController> _logger;

    public ServiceListController(IConfiguration configuration, ILogger<ServiceListController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult GetServiceList()
    {
        var docsPath = _configuration["DocsPath"];
        if (string.IsNullOrEmpty(docsPath))
        {
            return StatusCode(500, "DocsPath is not configured.");
        }

        var filePath = Path.Combine(docsPath, "PSServiceList.json");
        if (!System.IO.File.Exists(filePath))
        {
            _logger.LogError($"PSServiceList.json not found at {filePath}");
            return NotFound("PSServiceList.json not found.");
        }

        var json = System.IO.File.ReadAllText(filePath);
        return Content(json, "application/json");
    }
}
