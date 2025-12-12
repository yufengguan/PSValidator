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
    private readonly ILogger<MockController> _logger;

    public MockController(IMockResponseProvider provider, IWebHostEnvironment env, ILogger<MockController> logger)
    {
        _provider = provider;
        _env = env;
        _logger = logger;
    }

    // POST {service}/{ErrorCode}
    [HttpPost]
    public async Task<IActionResult> Post(string service, string errorCode)
    {
        _logger.LogInformation("Received request for Service: {Service}, ErrorCode: {ErrorCode}", service, errorCode);

        // --------------------------------------------------------------
        // Validate inputs
        if (string.IsNullOrWhiteSpace(service) || string.IsNullOrWhiteSpace(errorCode))
        {
            _logger.LogWarning("Validation failed: Service or ErrorCode is empty.");
            return BadRequest("Service and ErrorCode are required.");
        }

        // --------------------------------------------------------------
        // Resolve Docs path
        // Check /app/Docs (Docker) first, then fallback to ../../Docs (Local)
        var docsPath = Path.Combine(_env.ContentRootPath, "Docs");
        if (!Directory.Exists(docsPath))
        {
            _logger.LogInformation("Docs path not found at {DocsPath}, checking fallback.", docsPath);
            docsPath = Path.Combine(_env.ContentRootPath, "..", "..", "Docs");
        }

        if (!Directory.Exists(docsPath))
        {
            _logger.LogError("Docs directory NOT found at {DocsPath}", docsPath);
            return NotFound($"Server Misconfiguration: Docs directory not found. Searched at: {docsPath}");
        }

        _logger.LogInformation("Using Docs path: {DocsPath}", docsPath);

        // NORMALIZE CASING:
        // 1. Folder is "MockXmlResponses" (CamelCase XML), not "MockXMLResponses".
        // 2. Service folder is likely Uppercase (e.g. "PPC"), but URL might be "ppc".
        var safeService = service.ToUpperInvariant(); 

        var mockJsonPath = Path.Combine(docsPath, "MockXmlResponses", safeService, "mock_responses.json");
        _logger.LogInformation("Looking for mock configuration at: {MockJsonPath}", mockJsonPath);

        if (!System.IO.File.Exists(mockJsonPath))
        {
            _logger.LogWarning("File not found: {MockJsonPath}", mockJsonPath);
            
            // Debugging aid: list directories in MockXmlResponses if possible
            var parentDir = Path.Combine(docsPath, "MockXmlResponses");
            if (Directory.Exists(parentDir))
            {
               var subDirs = Directory.GetDirectories(parentDir).Select(d => Path.GetFileName(d));
               _logger.LogInformation("Available service directories: {Dirs}", string.Join(", ", subDirs));
            }

            return NotFound($"Mock configuration not found for service '{safeService}'. Searched in: {mockJsonPath}");
        }

        try
        {
            var jsonContent = await System.IO.File.ReadAllTextAsync(mockJsonPath);
            var mockConfig = JsonSerializer.Deserialize<MockResponseConfig>(jsonContent);
            var responseItem = mockConfig?.MockResponses?.FirstOrDefault(r => r.ErrorCode.Equals(errorCode, StringComparison.OrdinalIgnoreCase));

            if (responseItem == null)
            {
                _logger.LogWarning("ErrorCode '{ErrorCode}' not found in {MockJsonPath}", errorCode, mockJsonPath);
                return NotFound($"ErrorCode '{errorCode}' not found in mock configuration for service '{safeService}'.");
            }

            if (string.IsNullOrWhiteSpace(responseItem.StubResponseFile))
            {
                _logger.LogError("StubResponseFile is null/empty for ErrorCode '{ErrorCode}'", errorCode);
                return StatusCode(500, "Invalid mock configuration: StubResponseFile is missing.");
            }

            var mockPathFromConfig = Path.Combine(docsPath, "MockXmlResponses", safeService, responseItem.StubResponseFile);
            _logger.LogInformation("Looking for response file at: {MockPathFromConfig}", mockPathFromConfig);

            if (!System.IO.File.Exists(mockPathFromConfig))
            {
                _logger.LogError("Response file not found at: {MockPathFromConfig}", mockPathFromConfig);
                return NotFound($"Mock response file '{responseItem.StubResponseFile}' not found.");
            }

            var payloadFromConfig = await System.IO.File.ReadAllTextAsync(mockPathFromConfig);
            string soapEnvelopeFromConfig;

            // Check if the payload is already a SOAP Envelope
            // We look for the standard SOAP envelope namespace. 
            // A robust check would parse XML, but for mocks, a string check is usually sufficient and faster.
            if (payloadFromConfig.Contains("http://schemas.xmlsoap.org/soap/envelope/") && 
                (payloadFromConfig.Contains(":Envelope") || payloadFromConfig.Contains("Envelope")))
            {
                 _logger.LogInformation("Payload already contains SOAP Envelope. Skipping wrapper.");
                 soapEnvelopeFromConfig = payloadFromConfig;
            }
            else
            {
                _logger.LogInformation("Payload is raw body. Wrapping in SOAP Envelope.");
                soapEnvelopeFromConfig = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header/>
    <soapenv:Body>" + payloadFromConfig + @"</soapenv:Body>
</soapenv:Envelope>";
            }
            
            _logger.LogInformation("Successfully generated response for {Service}/{ErrorCode}", service, errorCode);
            return Content(soapEnvelopeFromConfig, "text/xml; charset=utf-8");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mock request.");
            return StatusCode(500, $"Error processing mock request: {ex.Message}");
        }
    }
}