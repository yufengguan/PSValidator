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
            var endpoint = $"{_mockServiceBaseUrl}/api/{service}/{responseItem.ErrorCode}";
            
            // We assume the operation is GetConfigurationAndPricingResponse based on PPC service context
            // and that the Validator expects this operation.
            // If the file content is <PPCResponse>, the Validator might need to know this.
            
            var request = new
            {
                Service = "ProductPricingandConfiguration",
                Version = "1.0.0", 
                Operation = "getConfigurationAndPricing", 
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
            // UNLESS ExpectedErrorDetails is empty (meaning the external validator found no error)
            
            if (string.IsNullOrWhiteSpace(responseItem.ExpectedErrorDetails))
            {
                // Verify it IS valid if we expect no error details
                Assert.IsTrue(result.IsValid, $"Expected validation SUCCESS for ErrorCode {responseItem.ErrorCode} but got Failure.");
            }
            else
            {
                Assert.IsFalse(result.IsValid, $"Expected validation failure for ErrorCode {responseItem.ErrorCode} but got Valid.");

                // Assert the expected error message is present
                // Using Case-Insensitive check or relaxed checking might be safer, but user said "validation result should equal"
                // The validator returns a list of messages. One of them should match/contain ExpectedErrorDetails.
                
                bool errorFound = result.ValidationResultMessages.Exists(msg => msg.Contains(responseItem.ExpectedErrorDetails));
                Assert.IsTrue(errorFound, $"Expected error '{responseItem.ExpectedErrorDetails}' not found for ErrorCode {responseItem.ErrorCode}. Found: {string.Join(", ", result.ValidationResultMessages)}");
            }
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
    public string ExpectedErrorDetails { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationResultMessages { get; set; } = new();
    public string ResponseContent { get; set; }
}


