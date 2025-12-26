using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

// Adjust namespace to match folder structure
namespace PromoStandards.Validator.Tests.Integration.PPC.V1_0_0;

[TestClass]
public class PPCTests : ServiceTestBase
{
    // Static property for DynamicData source
    public static IEnumerable<object[]> MockResponseData
    {
        get
        {
            var service = "PPC";
            var docRoot = TestConfig.GetDocsPath();
            var mockJsonPath = Path.Combine(docRoot, "MockXMLResponses", service, "1.0.0", "mock_responses.json");

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
    public async Task Validate_PPC_MockResponse(string errorCode, string stubFile, string expectedError, string expectedDetails)
    {
        var service = "PPC";
        var version = "1.0.0";
        // Construct the endpoint with errorCode
        var endpoint = $"{_mockServiceBaseUrl}/api/{service}/{version}/{errorCode}";
        
        // Determine operation based on filename
        // Default to getConfigurationAndPricing if pattern doesn't match
        var operation = "getConfigurationAndPricing"; 
        var requestRoot = "GetConfigurationAndPricingRequest";
        
        // PPC specific logic for operations
        if (stubFile.Contains("GetFobPoints"))
        {
            operation = "getFobPoints";
            requestRoot = "GetFobPointsRequest";
        }
        else if (stubFile.Contains("GetAvailableLocations"))
        {
            operation = "getAvailableLocations";
            requestRoot = "GetAvailableLocationsRequest";
        }
        else if (stubFile.Contains("GetDecorationColors"))
        {
            operation = "getDecorationColors";
            requestRoot = "GetDecorationColorsRequest";
        }
        else if (stubFile.Contains("GetAvailableCharges"))
        {
            operation = "getAvailableCharges";
            requestRoot = "GetAvailableChargesRequest";
        }

        var request = new
        {
            Service = "ProductPricingandConfiguration",
            Version = "1.0.0", 
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
