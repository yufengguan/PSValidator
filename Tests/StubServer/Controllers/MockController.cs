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
// [Route("api/{service}/{errorCode?}")] -- OLD
[Route("api/{service}/{version}/{errorCode?}")]
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

    // POST {service}/{version}/{ErrorCode}
    [HttpPost]
    public async Task<IActionResult> Post(string service, string version, string errorCode)
    {
        _logger.LogInformation("Received request for Service: {Service}, Version: {Version}, ErrorCode: {ErrorCode}", service, version, errorCode);

        // --------------------------------------------------------------
        // Validate inputs
        if (string.IsNullOrWhiteSpace(service) || string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(errorCode))
        {
            _logger.LogWarning("Validation failed: Service, Version or ErrorCode is empty.");
            return BadRequest("Service, Version, and ErrorCode are required. usage: api/{service}/{version}/{errorCode}");
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
        // 3. Version is likely exact match (1.0.0), but we should probably use it as is or sanitize.
        
        // Construct Path: Docs/MockXmlResponses/{Service}/{Version}/mock_responses.json
        var mockJsonPath = Path.Combine(docsPath, "MockXmlResponses", safeService, version, "mock_responses.json");
        _logger.LogInformation("Looking for mock configuration at: {MockJsonPath}", mockJsonPath);

        if (!System.IO.File.Exists(mockJsonPath))
        {
            _logger.LogWarning("File not found: {MockJsonPath}. Attempting to resolve Service Code from PSServiceList.json...", mockJsonPath);

            // Attempt to resolve Service Name (ProductData) to Code (PD)
            var serviceListPath = Path.Combine(docsPath, "PSServiceList.json");
            bool resolvedFromList = false;

            if (System.IO.File.Exists(serviceListPath))
            {
                try 
                {
                    var serviceListJson = await System.IO.File.ReadAllTextAsync(serviceListPath);
                    var serviceList = JsonSerializer.Deserialize<List<ServiceInfo>>(serviceListJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    var serviceInfo = serviceList?.FirstOrDefault(s => s.ServiceName.Equals(service, StringComparison.OrdinalIgnoreCase));
                    if (serviceInfo != null && !string.IsNullOrWhiteSpace(serviceInfo.Code))
                    {
                        var safeCode = serviceInfo.Code.ToUpperInvariant();
                        var newPath = Path.Combine(docsPath, "MockXmlResponses", safeCode, version, "mock_responses.json");
                        
                        _logger.LogInformation("Resolved Service '{Service}' to Code '{Code}'. Checking path: {NewPath}", service, safeCode, newPath);
                        
                        if (System.IO.File.Exists(newPath))
                        {
                            mockJsonPath = newPath;
                            resolvedFromList = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read or parse PSServiceList.json");
                }
            }

            if (!resolvedFromList)
            {
                 return NotFound($"Mock configuration not found for service '{safeService}' version '{version}'. Also failed to resolve via PSServiceList. Searched at: {mockJsonPath}");
            }
        }

        try
        {
            var jsonContent = await System.IO.File.ReadAllTextAsync(mockJsonPath);
            var mockConfig = JsonSerializer.Deserialize<MockResponseConfig>(jsonContent);
            var responseItem = mockConfig?.MockResponses?.FirstOrDefault(r => r.ErrorCode.Equals(errorCode, StringComparison.OrdinalIgnoreCase));

            if (responseItem == null)
            {
                _logger.LogWarning("ErrorCode '{ErrorCode}' not found in {MockJsonPath}", errorCode, mockJsonPath);
                return NotFound($"ErrorCode '{errorCode}' not found in mock configuration for service '{safeService}' version '{version}'.");
            }

            if (string.IsNullOrWhiteSpace(responseItem.StubResponseFile))
            {
                _logger.LogError("StubResponseFile is null/empty for ErrorCode '{ErrorCode}'", errorCode);
                return StatusCode(500, "Invalid mock configuration: StubResponseFile is missing.");
            }

            var mockPathFromConfig = Path.Combine(Path.GetDirectoryName(mockJsonPath), responseItem.StubResponseFile);
            _logger.LogInformation("Looking for response file at: {MockPathFromConfig}", mockPathFromConfig);

            if (!System.IO.File.Exists(mockPathFromConfig))
            {
                _logger.LogError("Response file not found at: {MockPathFromConfig}", mockPathFromConfig);
                return NotFound($"Mock response file '{responseItem.StubResponseFile}' not found.");
            }

            var payloadFromConfig = await System.IO.File.ReadAllTextAsync(mockPathFromConfig);
            string soapEnvelopeFromConfig;

            // Check if the payload is already a SOAP Envelope
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
            
            _logger.LogInformation("Successfully generated response for {Service}/{Version}/{ErrorCode}", service, version, errorCode);
            return Content(soapEnvelopeFromConfig, "text/xml; charset=utf-8");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing mock request.");
            return StatusCode(500, $"Error processing mock request: {ex.Message}");
        }
    }

    private class ServiceInfo 
    {
        public string ServiceName { get; set; }
        public string Code { get; set; }
    }
}