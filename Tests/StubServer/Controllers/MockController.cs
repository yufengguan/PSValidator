// c:\Projects\PS.SC\PSValidator\Tests.StubServer\Controllers\MockController.cs
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using PromoStandards.Validator.Tests.StubServer.Services;
using PromoStandards.Validator.Tests.StubServer.Models;
using System.Xml.Linq;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace PromoStandards.Validator.Tests.StubServer.Controllers;

[ApiController]
// The stub mimics a SOAP service for a given PromoStandards service.
// URL pattern: POST /api/{service}/{ErrorCode}
// Using 'api/' prefix to avoid conflicts with Swagger UI static files
[Route("api/{service}/{errorCode?}")]
public class MockController : ControllerBase
{
    private readonly IMockResponseProvider _provider;
    private readonly IWebHostEnvironment _env;

    public MockController(IMockResponseProvider provider, IWebHostEnvironment env)
    {
        _provider = provider;
        _env = env;
    }

    // POST {service}/{ErrorCode}
    [HttpPost]
    public async Task<IActionResult> Post(string service, string errorCode)
    {
        // --------------------------------------------------------------
        // Validate inputs
        if (string.IsNullOrWhiteSpace(service) || string.IsNullOrWhiteSpace(errorCode))
        {
            return BadRequest("Service and ErrorCode are required.");
        }

        // --------------------------------------------------------------
        // Look up the response in mock_responses.json
        // Resolve Docs path: check /app/Docs (Docker) first, then fallback to ../../Docs (Local)
        var docsPath = Path.Combine(_env.ContentRootPath, "Docs");
        if (!Directory.Exists(docsPath))
        {
            docsPath = Path.Combine(_env.ContentRootPath, "..", "..", "Docs");
        }

        var mockJsonPath = Path.Combine(docsPath, "MockXMLResponses", service, "mock_responses.json");
        
        if (!System.IO.File.Exists(mockJsonPath))
        {
            return NotFound($"Mock configuration not found for service '{service}'. Searched in: {docsPath}");
        }

        try
        {
            var jsonContent = await System.IO.File.ReadAllTextAsync(mockJsonPath);
            var mockConfig = JsonSerializer.Deserialize<MockResponseConfig>(jsonContent);
            var responseItem = mockConfig?.MockResponses?.FirstOrDefault(r => r.ErrorCode.Equals(errorCode, StringComparison.OrdinalIgnoreCase));

            if (responseItem == null)
            {
                return NotFound($"ErrorCode '{errorCode}' not found in mock configuration for service '{service}'.");
            }

            if (string.IsNullOrWhiteSpace(responseItem.StubResponseFile))
            {
                return StatusCode(500, "Invalid mock configuration: StubResponseFile is missing.");
            }

            var mockPathFromConfig = Path.Combine(docsPath, "MockXMLResponses", service, responseItem.StubResponseFile);
            
            if (!System.IO.File.Exists(mockPathFromConfig))
            {
                return NotFound($"Mock response file '{responseItem.StubResponseFile}' not found.");
            }

            var payloadFromConfig = await System.IO.File.ReadAllTextAsync(mockPathFromConfig);
            string soapEnvelopeFromConfig = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header/>
    <soapenv:Body>" + payloadFromConfig + @"</soapenv:Body>
</soapenv:Envelope>";
            return Content(soapEnvelopeFromConfig, "text/xml; charset=utf-8");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error processing mock request: {ex.Message}");
        }
    }
}