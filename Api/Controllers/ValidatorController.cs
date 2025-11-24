using Microsoft.AspNetCore.Mvc;
using PromoStandards.Validator.Api.Models;
using PromoStandards.Validator.Api.Services;

namespace PromoStandards.Validator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ValidatorController : ControllerBase
{
    private readonly IValidationService _validationService;
    private readonly ILogger<ValidatorController> _logger;

    public ValidatorController(IValidationService validationService, ILogger<ValidatorController> logger)
    {
        _validationService = validationService;
        _logger = logger;
    }

    [HttpPost("validate")]
    public async Task<IActionResult> Validate([FromBody] ValidationRequest request)
    {
        if (string.IsNullOrEmpty(request.XmlContent))
        {
            return BadRequest("XmlContent is required.");
        }

        var result = await _validationService.Validate(request.XmlContent, request.Service, request.Version, request.Operation, request.Endpoint);
        return Ok(result);
    }

    [HttpGet("sample-request")]
    public async Task<IActionResult> GetSampleRequest(string serviceName, string version, string operationName)
    {
        var xml = await _validationService.GenerateSampleRequest(serviceName, version, operationName);
        return Ok(xml);
    }

    [HttpGet("response-schema")]
    public async Task<IActionResult> GetResponseSchema(string serviceName, string version, string operationName)
    {
        var schema = await _validationService.GetResponseSchema(serviceName, version, operationName);
        return Ok(schema);
    }
}
