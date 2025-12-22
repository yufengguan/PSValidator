using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PromoStandards.Validator.Api.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace PromoStandards.Validator.Tests.Integration;

[TestClass]
public class SampleGenerationTests
{
    private IValidationRequestService _requestService;
    private string _docsPath;

    [TestInitialize]
    public void Setup()
    {
        _docsPath = TestConfig.GetDocsPath();
        
        var inMemorySettings = new Dictionary<string, string> {
            {"DocsPath", _docsPath}
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _requestService = new ValidationRequestService(configuration, NullLogger<ValidationRequestService>.Instance);
    }

    [TestMethod]
    public async Task ValidateAllSampleRequests()
    {
        // 1. Load PSServiceList.json
        var jsonPath = Path.Combine(_docsPath, "PSServiceList.json");
        Assert.IsTrue(File.Exists(jsonPath), $"PSServiceList.json not found at {jsonPath}");

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var services = JsonSerializer.Deserialize<List<ServiceInfo>>(File.ReadAllText(jsonPath), jsonOptions);
        
        Assert.IsNotNull(services, "Failed to deserialize PSServiceList.json");

        var failureMessages = new List<string>();
        int totalChecked = 0;

        // 2. Iterate through all operations
        foreach (var service in services)
        {
            if (service.Versions == null) continue;

            foreach (var version in service.Versions)
            {
                if (version.Operations == null) continue;

                string versionStr = $"{version.Major}.{version.Minor}.{version.Patch}";

                foreach (var op in version.Operations)
                {
                    totalChecked++;
                    string context = $"[{service.ServiceName} v{versionStr} - {op.OperationName}]";
                    Console.WriteLine($"Checking {context}...");

                    try
                    {
                        // 3. Generate Sample Request
                        string sampleXml = await _requestService.GenerateSampleRequest(service.ServiceName, versionStr, op.OperationName);

                        if (string.IsNullOrWhiteSpace(sampleXml))
                        {
                            failureMessages.Add($"{context}: Generated sample is empty.");
                            continue;
                        }

                        if (sampleXml.Trim().StartsWith("<!--") && sampleXml.Contains("Error"))
                        {
                            failureMessages.Add($"{context}: Sample generation returned error: {sampleXml}");
                            continue;
                        }

                        // 4. Validate the Sample Request
                        var validationResult = await _requestService.ValidateRequest(sampleXml, service.ServiceName, versionStr, op.OperationName);

                        if (!validationResult.IsValid)
                        {
                            string errors = string.Join("; ", validationResult.ValidationResultMessages);
                            failureMessages.Add($"{context}: Validation FAIL. Errors: {errors}");
                        }
                    }
                    catch (Exception ex)
                    {
                        failureMessages.Add($"{context}: EXCEPTION: {ex.Message}");
                    }
                }
            }
        }

        Console.WriteLine($"Total Operations Checked: {totalChecked}");

        // 5. Assert No Failures
        if (failureMessages.Count > 0)
        {
            Console.WriteLine("FAILURES FOUND:");
            foreach (var msg in failureMessages)
            {
                Console.WriteLine(msg);
            }
            Assert.Fail($"Found {failureMessages.Count} failures out of {totalChecked} operations.\n" + string.Join("\n", failureMessages));
        }
    }

    // Helper classes for JSON
    private class ServiceInfo
    {
        public string ServiceName { get; set; }
        public List<VersionInfo> Versions { get; set; }
    }

    private class VersionInfo
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public List<OperationInfo> Operations { get; set; }
    }

    private class OperationInfo
    {
        public string OperationName { get; set; }
    }
}
