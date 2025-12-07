using System;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading;

namespace PromoStandards.Validator.Tests.StubServer.Services;

/// <summary>
/// Provides mock XML responses for the stub server.
/// XML files are stored under the <c>TestXmlResponses</c> folder using the pattern:
/// <c>TestXmlResponses/{service}/{MethodName}-{code}-*.xml</c>
/// where <c>{service}</c> is the lower‑cased service name (e.g., "orderstatus"),
/// <c>{MethodName}</c> is the name of the helper method (e.g., "GetOrderStatusResponse"),
/// and <c>{code}</c> is the response code (e.g., "valid", "error").
/// If no matching file is found, a generic XML response is returned.
/// </summary>
public interface IMockResponseProvider
{
    /// <summary>
    /// Returns an XML response for the given service, operation and code.
    /// </summary>
    string GetResponse(string service, string operation, string code);
}

public class MockResponseProvider : IMockResponseProvider
{
    // Base directory where all mock XML files are kept.
    private static readonly string BasePath;

static MockResponseProvider()
{
    // Load configuration from appsettings.json (if present)
    var builder = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
    var config = builder.Build();
    var path = config["MockResponsePath"];
    if (string.IsNullOrWhiteSpace(path))
    {
        // Fallback to the original folder name
        path = Path.Combine(Directory.GetCurrentDirectory(), "TestXmlResponses");
    }
    else
    {
        // Resolve relative to the current directory
        path = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), path));
    }
    BasePath = path;
}

    public string GetResponse(string service, string operation, string code)
    {
        // Normalise inputs for case‑insensitive lookup.
        service = service.ToLowerInvariant();
        operation = operation.ToLowerInvariant();
        code = code.ToLowerInvariant();

        // Generic error handling that does not depend on external files.
        if (code == "timeout")
        {
            Thread.Sleep(30000); // Simulate a long‑running request.
            return GetValidResponse(service, operation);
        }

        if (code == "server-error")
        {
            throw new Exception("Simulated Internal Server Error");
        }

        if (code == "malformed")
        {
            return "<InvalidXml>This is not well‑formed XML";
        }

        // Dispatch to service‑specific loaders.
        return service switch
        {
            "orderstatus" => GetOrderStatusResponse(operation, code),
            "inventory"   => GetInventoryResponse(operation, code),
            "productdata" => GetProductDataResponse(operation, code),
            _ => GetGenericResponse(service, operation, code)
        };
    }

    private string GetValidResponse(string service, string operation)
    {
        // "valid" is the conventional code used by the test suite.
        return GetResponse(service, operation, "valid");
    }

    // ---------------------------------------------------------------------
    // Service‑specific response loaders
    // ---------------------------------------------------------------------
    private string GetOrderStatusResponse(string operation, string code)
    {
        // The method name is part of the file naming convention.
        return LoadResponse("orderstatus", nameof(GetOrderStatusResponse), code);
    }

    private string GetInventoryResponse(string operation, string code)
    {
        return LoadResponse("inventory", nameof(GetInventoryResponse), code);
    }

    private string GetProductDataResponse(string operation, string code)
    {
        return LoadResponse("productdata", nameof(GetProductDataResponse), code);
    }

    // ---------------------------------------------------------------------
    // Generic fallback response – used when no file matches.
    // ---------------------------------------------------------------------
    private string GetGenericResponse(string service, string operation, string code)
    {
        // Path to the generic fallback XML file.
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "GenericResponse.xml");
        if (File.Exists(filePath))
        {
            return File.ReadAllText(filePath);
        }
        // If the file is missing, return a simple error XML.
        return $"<Error>Generic response file not found for {service}/{operation}/{code}</Error>";
    }

    // ---------------------------------------------------------------------
    // Helper that loads an XML file based on the naming convention.
    // Expected pattern: {MethodName}-{code}-*.xml inside TestXmlResponses/{service}
    // ---------------------------------------------------------------------
    private string LoadResponse(string service, string methodName, string code)
    {
        var serviceDir = Path.Combine(BasePath, service);
        if (!Directory.Exists(serviceDir))
        {
            // Service folder missing – fall back to generic response.
            return GetGenericResponse(service, "unknown", code);
        }

        // Look for a file that matches the exact code.
        var exactPattern = $"{methodName}-{code}-*.xml";
        var exactMatches = Directory.GetFiles(serviceDir, exactPattern);
        if (exactMatches.Length > 0)
        {
            return File.ReadAllText(exactMatches[0]);
        }

        // If no exact match, try a generic "valid" file for the method.
        var genericPattern = $"{methodName}-valid-*.xml";
        var genericMatches = Directory.GetFiles(serviceDir, genericPattern);
        if (genericMatches.Length > 0)
        {
            return File.ReadAllText(genericMatches[0]);
        }

        // No file found – fall back to generic XML response.
        return GetGenericResponse(service, "unknown", code);
    }
}
