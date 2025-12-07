using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.IO;

namespace PromoStandards.Validator.Tests.Integration;

[TestClass]
public class ValidatorWithMockResponsesTests
{
    private HttpClient _client = null!;
    private string _mockServiceBaseUrl = null!;
    private string _projectRoot = null!;

    [TestInitialize]
    public void TestInitialize()
    {
        _client = new HttpClient()
        {
            BaseAddress = new Uri(TestConfig.ApiBaseUrl)
        };
        _mockServiceBaseUrl = TestConfig.StubServerBaseUrl.TrimEnd('/');
        
        // Hardcoded path to project root based on user environment
        _projectRoot = @"c:\Projects\PS.SC\PSValidator";
    }

    [TestMethod]
    public async Task Validate_PPC_MockResponses_ShouldMatchExpectations()
    {
        var service = "PPC";
        var mockJsonPath = Path.Combine(_projectRoot, "Docs", "MockXMLResponses", service, "mock_responses.json");
        
        if (!File.Exists(mockJsonPath))
        {
            Assert.Fail($"Mock responses file not found at {mockJsonPath}");
        }

        var jsonContent = await File.ReadAllTextAsync(mockJsonPath);
        var mockConfig = JsonSerializer.Deserialize<MockResponseConfig>(jsonContent);

        if (mockConfig == null || mockConfig.MockResponses == null)
        {
            Assert.Fail("Failed to deserialize mock_responses.json");
        }

        foreach (var responseItem in mockConfig.MockResponses)
        {
            // Construct the endpoint with errorCode
            var endpoint = $"{_mockServiceBaseUrl}/{service}/{responseItem.ErrorCode}";
            
            // We assume the operation is GetConfigurationAndPricingResponse based on PPC service context
            // and that the Validator expects this operation.
            // If the file content is <PPCResponse>, the Validator might need to know this.
            
            var request = new
            {
                Service = service,
                Version = "1.0.0", 
                Operation = "GetConfigurationAndPricingResponse", 
                XmlContent = "<GetConfigurationAndPricingRequest>Dummy Request</GetConfigurationAndPricingRequest>",
                Endpoint = endpoint
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request),
                Encoding.UTF8,
                "application/json");

            var response = await _client.PostAsync("/api/Validator/validate", content);
            
            if (response.StatusCode != HttpStatusCode.OK)
            {
                Assert.Fail($"Validator returned {response.StatusCode} for ErrorCode {responseItem.ErrorCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<ValidationResult>();

            Assert.IsNotNull(result, $"Result is null for ErrorCode {responseItem.ErrorCode}");
            
            // The expected error implies the validation should FAIL (IsValid = false)
            Assert.IsFalse(result.IsValid, $"Expected validation failure for ErrorCode {responseItem.ErrorCode} but got Valid.");

            // Assert the expected error message is present
            bool errorFound = result.ValidationResultMessages.Exists(msg => msg.Contains(responseItem.ExpectedError));
            Assert.IsTrue(errorFound, $"Expected error '{responseItem.ExpectedError}' not found for ErrorCode {responseItem.ErrorCode}. Found: {string.Join(", ", result.ValidationResultMessages)}");
        }
    }
}

// Helper classes duplicated here for simplicity
public class MockResponseConfig
{
    public string Service { get; set; }
    public List<MockResponseItem> MockResponses { get; set; }
}

public class MockResponseItem
{
    public string ErrorCode { get; set; }
    public string StubResponseFile { get; set; }
    public string ExpectedError { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationResultMessages { get; set; } = new();
    public string ResponseContent { get; set; }
}


