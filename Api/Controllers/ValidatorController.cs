using Microsoft.AspNetCore.Mvc;
using PromoStandards.Validator.Api.Models;
using PromoStandards.Validator.Api.Services;

namespace PromoStandards.Validator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ValidatorController : ControllerBase
{
    private readonly IValidationRequestService _requestService;
    private readonly IValidationResponseService _responseService;
    private readonly ILogger<ValidatorController> _logger;

    public ValidatorController(
        IValidationRequestService requestService, 
        IValidationResponseService responseService,
        ILogger<ValidatorController> logger)
    {
        _requestService = requestService;
        _responseService = responseService;
        _logger = logger;
    }

    [HttpPost("validate-response")]
    public async Task<IActionResult> ValidateResponse([FromBody] ValidationRequest request)
    {
        if (string.IsNullOrEmpty(request.XmlContent))
        {
            return BadRequest("XmlContent is required.");
        }

        var result = await _responseService.ValidateResponse(request.XmlContent, request.Service, request.Version, request.Operation, request.Endpoint);
        return Ok(result);
    }
    
    [HttpPost("validate-request")]
    public async Task<IActionResult> ValidateRequest([FromBody] ValidationRequest request)
    {
        if (string.IsNullOrEmpty(request.XmlContent))
        {
            return BadRequest("XmlContent is required.");
        }

        var result = await _requestService.ValidateRequest(request.XmlContent, request.Service, request.Version, request.Operation);
        return Ok(result);
    }

    [HttpGet("sample-request")]
    public async Task<IActionResult> GetSampleRequest(string serviceName, string version, string operationName)
    {
        var xml = await _requestService.GenerateSampleRequest(serviceName, version, operationName);
        return Ok(new { xmlContent = xml });
    }

    [HttpGet("response-schema")]
    public async Task<IActionResult> GetResponseSchema(string serviceName, string version, string operationName)
    {
        var schema = await _responseService.GetResponseSchema(serviceName, version, operationName);
        return Ok(schema);
    }

    [HttpGet("request-schema")]
    public async Task<IActionResult> GetRequestSchema(string serviceName, string version, string operationName)
    {
        var schema = await _requestService.GetRequestSchema(serviceName, version, operationName);
        return Ok(schema);
    }
}
