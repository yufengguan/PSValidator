using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

// Adjust namespace to match folder structure
namespace PromoStandards.Validator.Tests.Integration.PD.V2_0_0;

[TestClass]
public class PDTests : ServiceTestBase
{
    // Static property for DynamicData source
    public static IEnumerable<object[]> MockResponseData
    {
        get
        {
            var service = "PD";
            // Use TestConfig from the parent namespace (or import it if needed, but TestConfig is public static in Integration namespace)
            // Since we are in Integration.PD.V2_0_0, we can access Integration.TestConfig if we add using or qualify it.
            // Using "PromoStandards.Validator.Tests.Integration" should work if added to usings, or just qualify.
            // Let's rely on standard resolution or qualify it.
            var docRoot = TestConfig.GetDocsPath();
            var mockJsonPath = Path.Combine(docRoot, "MockXMLResponses", service, "2.0.0", "mock_responses.json");

            if (!File.Exists(mockJsonPath))
            {
                throw new FileNotFoundException($"Mock responses file not found at {mockJsonPath}");
            }

            var jsonContent = File.ReadAllText(mockJsonPath);
            var mockConfig = JsonSerializer.Deserialize<MockResponseConfig>(jsonContent);

            if (mockConfig == null || mockConfig.MockResponses == null)
            {
                throw new InvalidOperationException("Failed to deserialize mock_responses.json");
            }

            foreach (var responseItem in mockConfig.MockResponses)
            {
                // Yield return the parameters for the test method
                yield return new object[] 
                { 
                    responseItem.ErrorCode, 
                    responseItem.StubResponseFile, 
                    responseItem.ExpectedError ?? string.Empty, 
                    responseItem.ExpectedErrorDetails ?? string.Empty
                };
            }
        }
    }

    [DataTestMethod]
    [DynamicData(nameof(MockResponseData), DynamicDataSourceType.Property)]
    public async Task Validate_PD_MockResponse(string errorCode, string stubFile, string expectedError, string expectedDetails)
    {
        var service = "PD";
        // Construct the endpoint with errorCode
        var endpoint = $"{_mockServiceBaseUrl}/api/{service}/{errorCode}";
        
        // Determine operation based on filename logic
        var operation = "getProduct"; 
        var requestRoot = "GetProductRequest";
        
        // PD specific logic for operations
        if (stubFile.Contains("GetProductDateModified"))
        {
            operation = "getProductDateModified";
            requestRoot = "GetProductDateModifiedRequest";
        }
        else if (stubFile.Contains("GetProductCloseOut"))
        {
            operation = "getProductCloseOut";
            requestRoot = "GetProductCloseOutRequest";
        }
        else if (stubFile.Contains("GetProductSellable"))
        {
            operation = "getProductSellable";
            requestRoot = "GetProductSellableRequest";
        }

        var request = new
        {
            Service = "ProductData",
            Version = "2.0.0", 
            Operation = operation, 
            XmlContent = $"<{requestRoot}>Dummy Request</{requestRoot}>",
            Endpoint = endpoint
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        var response = await _client.PostAsync("/api/Validator/validate-response", content);
        
        if (response.StatusCode != HttpStatusCode.OK)
        {
            Assert.Fail($"Validator returned {response.StatusCode} for ErrorCode {errorCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<ValidationResult>();

        Assert.IsNotNull(result, $"Result is null for ErrorCode {errorCode}");
        
        if (string.IsNullOrWhiteSpace(expectedDetails))
        {
            // Verify it IS valid if we expect no error details
            Assert.IsTrue(result.IsValid, $"Expected validation SUCCESS for ErrorCode {errorCode} but got Failure. Messages: {string.Join(", ", result.ValidationResultMessages)}");
        }
        else
        {
            Assert.IsFalse(result.IsValid, $"Expected validation failure for ErrorCode {errorCode} but got Valid.");

            // Normalize both expected and actual errors for relaxed comparison
            var expectedNormalized = NormalizeErrorMessage(expectedDetails);

            bool errorFound = result.ValidationResultMessages.Exists(msg => 
                NormalizeErrorMessage(msg).Contains(expectedNormalized));
            
            Assert.IsTrue(errorFound, $"Expected error '{expectedDetails}' not found for ErrorCode {errorCode}. Found: {string.Join(", ", result.ValidationResultMessages)}");
        }
    }
}
