using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.IO;

namespace PromoStandards.Validator.Tests.Integration;

[TestClass]
public class PDTests : ServiceTestBase
{
    [TestMethod]
    public async Task Validate_PD_MockResponses_ShouldMatchExpectations()
    {
        var service = "PD";
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
            
            // Determine operation based on filename
            // Names are like "PD-1.0.0-{Operation}Response-{Index}.xml"
            // Default to getProduct if pattern doesn't match
            var operation = "getProduct"; 
            var requestRoot = "GetProductRequest";
            
            // PD specific logic for operations
            if (responseItem.StubResponseFile.Contains("GetProductDateModified"))
            {
                operation = "getProductDateModified";
                requestRoot = "GetProductDateModifiedRequest";
            }
            else if (responseItem.StubResponseFile.Contains("GetProductCloseOut"))
            {
                operation = "getProductCloseOut";
                requestRoot = "GetProductCloseOutRequest";
            }
            else if (responseItem.StubResponseFile.Contains("GetProductSellable"))
            {
                operation = "getProductSellable";
                requestRoot = "GetProductSellableRequest";
            }

            var request = new
            {
                Service = "ProductData",
                Version = "1.0.0", 
                Operation = operation, 
                XmlContent = $"<{requestRoot}>Dummy Request</{requestRoot}>",
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
            
            if (string.IsNullOrWhiteSpace(responseItem.ExpectedErrorDetails))
            {
                // Verify it IS valid if we expect no error details
                Assert.IsTrue(result.IsValid, $"Expected validation SUCCESS for ErrorCode {responseItem.ErrorCode} but got Failure.");
            }
            else
            {
                Assert.IsFalse(result.IsValid, $"Expected validation failure for ErrorCode {responseItem.ErrorCode} but got Valid.");

                // Normalize both expected and actual errors for relaxed comparison
                var expectedNormalized = NormalizeErrorMessage(responseItem.ExpectedErrorDetails);

                bool errorFound = result.ValidationResultMessages.Exists(msg => 
                    NormalizeErrorMessage(msg).Contains(expectedNormalized));
                
                Assert.IsTrue(errorFound, $"Expected error '{responseItem.ExpectedErrorDetails}' not found for ErrorCode {responseItem.ErrorCode}. Found: {string.Join(", ", result.ValidationResultMessages)}");
            }
        }
    }
}
