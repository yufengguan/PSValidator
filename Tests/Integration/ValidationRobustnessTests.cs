using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq.Protected;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PromoStandards.Validator.Api.Services;
using PromoStandards.Validator.Tests.Integration;
using System.Net.Http;
using System.Linq;
using System.Threading;

namespace PromoStandards.Validator.Tests.Integration
{
    [TestClass]
    public class ValidationRobustnessTests
    {
        private ValidationResponseService _service;
        private Mock<IConfiguration> _mockConfig;
        private Mock<ILogger<ValidationResponseService>> _mockLogger;
        private Mock<IHttpClientFactory> _mockHttpClientFactory;

        [TestInitialize]
        public void Setup()
        {
            _mockConfig = new Mock<IConfiguration>();
            // Use real Docs path
            var docsPath = TestConfig.GetDocsPath();
            _mockConfig.Setup(c => c["DocsPath"]).Returns(docsPath);

            _mockLogger = new Mock<ILogger<ValidationResponseService>>();
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            
            // Default mock setup
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'><soapenv:Body><root>Default</root></soapenv:Body></soapenv:Envelope>")
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            _service = new ValidationResponseService(_mockConfig.Object, _mockLogger.Object, _mockHttpClientFactory.Object);
        }

        [TestMethod]
        public async Task Validate_InvalidDataType_ShouldFail()
        {
            var xmlContent = @"
<ns:GetProductResponse xmlns:ns=""http://www.promostandards.org/WSDL/ProductDataService/2.0.0/"" xmlns:shar=""http://www.promostandards.org/WSDL/ProductDataService/2.0.0/SharedObjects/"">
   <ns:Product>
      <shar:productId>123</shar:productId>
      <ns:ProductPriceGroupArray>
         <shar:ProductPriceGroup>
            <shar:groupName>Standard</shar:groupName>
            <shar:currency>USD</shar:currency>
            <shar:ProductPriceArray>
               <shar:ProductPrice>
                   <shar:quantityMin>ABC</shar:quantityMin>
                   <shar:price>10.00</shar:price>
               </shar:ProductPrice>
            </shar:ProductPriceArray>
         </shar:ProductPriceGroup>
      </ns:ProductPriceGroupArray>
   </ns:Product>
</ns:GetProductResponse>";

            string serviceName = "ProductData";
            string version = "2.0.0";
            string operation = "GetProduct";

            // Pass null endpoint to validate the XML content directly without making an HTTP call
            var result = await _service.ValidateResponse(xmlContent, serviceName, version, operation, null);

            if (result.IsValid) System.Console.WriteLine("Unexpected Success: Validated 'ABC' as integer.");
            else 
            {
                System.Console.WriteLine("Test Passed: Caught invalid data type.");
                foreach(var m in result.ValidationResultMessages) System.Console.WriteLine($"Error: {m}");
            }

            Assert.IsFalse(result.IsValid, "Should fail when 'quantityMin' contains non-integer text.");
            Assert.IsTrue(result.ValidationResultMessages.Any(m => m.Contains("ABC") || m.Contains("invalid") || m.Contains("format")), "Should mention the invalid value or format error.");
        }

        [TestMethod]
        public async Task Validate_MalformedXml_ShouldFail()
        {
             var xmlContent = @"<ns:GetProductResponse xmlns:ns=""http://www.promostandards.org/WSDL/ProductDataService/2.0.0/""><ns:Product><ns:productId>123</ns:productId><ns:description>Unclosed String</ns:Product></ns:GetProductResponse>";
             // Intentionally removing closing quote or tag to make it malformed? 
             // The previous one was: <ns:description>Unclosed String \n </ns:Product> ...
             // Let's make it explicitly malformed syntax:
             xmlContent = @"<ns:GetProductResponse xmlns:ns=""http://www.promostandards.org/WSDL/ProductDataService/2.0.0/"">
   <ns:Product>
      <ns:productId>123</ns:productId>
      <ns:description>Unclosed String
   </ns:Product>
</ns:GetProductResponse>";

            // Pass null endpoint
            var result = await _service.ValidateResponse(xmlContent, "ProductData", "2.0.0", "GetProduct", null);

             if (!result.IsValid)
             {
                 foreach(var m in result.ValidationResultMessages) System.Console.WriteLine($"MalformedXml Error: {m}");
             }
            
            Assert.IsFalse(result.IsValid, "Should fail on malformed XML.");
            Assert.IsTrue(result.ValidationResultMessages.Any(m => m.Contains("load") || m.Contains("parse") || m.Contains("XmlException") || m.Contains("Error")), "Should identify XML parsing error. Actual: " + string.Join(", ", result.ValidationResultMessages));
        }

        [TestMethod]
        public async Task Validate_EmptyBody_ShouldFail()
        {
            // Valid SOAP Envelope but empty body content (or empty string passed as extracted body)
            // The service expects the extraction to happen before validation, but here we pass the "extracted body".
            // If we extract nothing, it might be empty string.
            var xmlContent = ""; 
            
            // Pass null endpoint
            var result = await _service.ValidateResponse(xmlContent, "ProductData", "2.0.0", "GetProduct", null);

             if (!result.IsValid)
             {
                 foreach(var m in result.ValidationResultMessages) System.Console.WriteLine($"EmptyBody Error: {m}");
             }

             Assert.IsFalse(result.IsValid, "Should fail on empty content.");
             Assert.IsTrue(result.ValidationResultMessages.Count > 0);
        }
    }
}
