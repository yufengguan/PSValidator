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
    public IActionResult Validate([FromBody] ValidationRequest request)
    {
        if (string.IsNullOrEmpty(request.XmlContent))
        {
            return BadRequest("XmlContent is required.");
        }

        var result = _validationService.Validate(request.XmlContent, request.Service, request.Version, request.Operation);
        return Ok(result);
    }
}
