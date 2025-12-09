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
    public async Task<IActionResult> Post(string service, string? errorCode = null)
    {
        // --------------------------------------------------------------
        // 0️⃣ If errorCode is provided, look up the response in mock_responses.json
        if (!string.IsNullOrWhiteSpace(errorCode))
        {
            var mockJsonPath = Path.Combine(_env.ContentRootPath, "..", "..", "Docs", "MockXMLResponses", service, "mock_responses.json");
            if (System.IO.File.Exists(mockJsonPath))
            {
                try
                {
                    var jsonContent = await System.IO.File.ReadAllTextAsync(mockJsonPath);
                    var mockConfig = JsonSerializer.Deserialize<MockResponseConfig>(jsonContent);
                    var responseItem = mockConfig?.MockResponses?.FirstOrDefault(r => r.ErrorCode.Equals(errorCode, StringComparison.OrdinalIgnoreCase));

                    if (responseItem != null && !string.IsNullOrWhiteSpace(responseItem.StubResponseFile))
                    {
                        var mockPathFromConfig = Path.Combine(_env.ContentRootPath, "..", "..", "Docs", "MockXMLResponses", service, responseItem.StubResponseFile);
                        if (System.IO.File.Exists(mockPathFromConfig))
                        {
                            var payloadFromConfig = await System.IO.File.ReadAllTextAsync(mockPathFromConfig);
                            string soapEnvelopeFromConfig = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header/>
    <soapenv:Body>" + payloadFromConfig + @"</soapenv:Body>
</soapenv:Envelope>";
                            return Content(soapEnvelopeFromConfig, "text/xml; charset=utf-8");
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Log error or fall through
                    Console.WriteLine($"Error reading mock_responses.json: {ex.Message}");
                }
            }
        }

        // --------------------------------------------------------------
        // 1️⃣ Read the raw SOAP request body.
        string requestBody;
        using (var reader = new StreamReader(Request.Body))
        {
            requestBody = await reader.ReadToEndAsync();
        }

        // --------------------------------------------------------------
        // 2️⃣ Extract the operation name **and** the wsVersion from the SOAP body.
        //    The first child element inside <soap:Body> is the operation name.
        //    The <wsVersion> element (in the shared objects namespace) gives the service version.
        string operationName = null;
        string wsVersion = "1.0.0"; // default fallback
        try
        {
            var doc = XDocument.Parse(requestBody);
            XNamespace soapNs = "http://schemas.xmlsoap.org/soap/envelope/";
            XNamespace sharedNs = "http://www.promostandards.org/WSDL/PricingAndConfiguration/1.0.0/SharedObjects/";
            var body = doc.Root.Element(soapNs + "Body");
            var first = body?.Elements().FirstOrDefault();
            operationName = first?.Name.LocalName;
            var versionElem = first?.Element(sharedNs + "wsVersion");
            if (versionElem != null && !string.IsNullOrWhiteSpace(versionElem.Value))
                wsVersion = versionElem.Value.Trim();
        }
        catch
        {
            // Parsing failed – let operationName stay null.
        }

        if (string.IsNullOrWhiteSpace(operationName))
        {
            return BadRequest("Unable to determine operation from SOAP body.");
        }

        // --------------------------------------------------------------
        // 3️⃣ Build the expected mock file name.
        //    Pattern: {service}-{wsVersion}-{operationName}-{sequence}.xml
        //    If multiple files match, pick the first one (deterministic for now).
        string mockFilePattern = $"{service}-{wsVersion}-{operationName}-*.xml";
        var mockDirectory = Path.Combine(_env.ContentRootPath, "..", "..", "Docs", "MockXMLResponses", service);
        
        // Ensure directory exists to avoid crashes
        if (!Directory.Exists(mockDirectory))
        {
             return NotFound($"Mock directory not found for service '{service}'.");
        }

        var matchingFiles = Directory.GetFiles(mockDirectory, mockFilePattern);
        if (matchingFiles.Length == 0)
        {
            return NotFound($"No mock XML file matching pattern '{mockFilePattern}' found for service '{service}'.");
        }
        // Use the first matching file.
        var mockPath = matchingFiles[0];

        // --------------------------------------------------------------
        // 4️⃣ Load the mock XML payload.
        var payload = await System.IO.File.ReadAllTextAsync(mockPath);

        // --------------------------------------------------------------
        // 5️⃣ Wrap the payload in a SOAP envelope (real PromoStandards services
        //    always return SOAP).  The envelope is minimal – just Header + Body.
        string soapEnvelope = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
    <soapenv:Header/>
    <soapenv:Body>" + payload + @"</soapenv:Body>
</soapenv:Envelope>";

        // --------------------------------------------------------------
        // 6️⃣ Return the SOAP response.
        return Content(soapEnvelope, "text/xml; charset=utf-8");
    }
}