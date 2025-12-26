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

namespace PromoStandards.Validator.Tests.Integration
{
    [TestClass]
    public class ValidationReproductionTests
    {
        private ValidationResponseService _service; // Changed to ValidationResponseService
        private Mock<IConfiguration> _mockConfig;
        private Mock<ILogger<ValidationResponseService>> _mockLogger; // Updated generic type
        private Mock<IHttpClientFactory> _mockHttpClientFactory;

        [TestInitialize]
        public void Setup()
        {
            _mockConfig = new Mock<IConfiguration>();
            // Use real Docs path
            var docsPath = TestConfig.GetDocsPath();
            _mockConfig.Setup(c => c["DocsPath"]).Returns(docsPath);

            _mockLogger = new Mock<ILogger<ValidationResponseService>>(); // Updated generic type
            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            
            // Setup HttpClient mock
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'><soapenv:Body><ns:GetProductResponse xmlns:ns='http://www.promostandards.org/WSDL/ProductDataService/2.0.0/'><ns:ProductId>123</ns:ProductId></ns:GetProductResponse></soapenv:Body></soapenv:Envelope>")
                });

            var client = new HttpClient(mockHttpMessageHandler.Object);
            _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(client);

            _service = new ValidationResponseService(_mockConfig.Object, _mockLogger.Object, _mockHttpClientFactory.Object);
        }

        [TestMethod]
        public async Task Validate_ProductMissingId_ShouldFail()
        {
            // Arrange
            // This is the XML extracted from the SOAP body that should cause an error
            var xmlContent = @"
<ns:GetProductResponse
     xmlns:ns=""http://www.promostandards.org/WSDL/ProductDataService/2.0.0/""
     xmlns:shar=""http://www.promostandards.org/WSDL/ProductDataService/2.0.0/SharedObjects/"">
   <ns:Product>
      <!-- productId is missing -->
      <shar:productName>Mock Product Missing ID</shar:productName>
   </ns:Product>
</ns:GetProductResponse>";

            string serviceName = "ProductData";
            string version = "2.0.0";
            string operation = "GetProduct";

            // Act
            var result = await _service.ValidateResponse(xmlContent, serviceName, version, operation, "http://test.com/api");

            // Assert
            bool printed = false;
            foreach(var msg in result.ValidationResultMessages)
            {
                System.Console.WriteLine("VALIDATION ERROR: " + msg);
                printed = true;
            }
            if (!printed && result.IsValid) System.Console.WriteLine("VALIDATION SUCCESS (Unexpected)");

            Assert.IsFalse(result.IsValid, "Validation should fail when productId is missing.");
        }

        [TestMethod]
        public async Task Validate_NamespaceMismatch_ShouldFail()
        {
            // Scenario: 1.0.0 Schema expecting http://www.promostandards.org/WSDL/ProductDataService/1.0.0/
            // Input: 2.0.0 XML with http://www.promostandards.org/WSDL/ProductDataService/2.0.0/
            var xmlContent = @"
<ns:GetProductResponse xmlns:ns=""http://www.promostandards.org/WSDL/ProductDataService/2.0.0/"">
   <ns:Product>
       <!-- Valid 2.0.0 content -->
   </ns:Product>
</ns:GetProductResponse>";

            string serviceName = "ProductData";
            string version = "1.0.0"; // User selected 1.0.0
            string operation = "GetProduct";

            // Act
            var result = await _service.ValidateResponse(xmlContent, serviceName, version, operation, "http://test.com/api");

            if (result.IsValid)
            {
                 System.Console.WriteLine("Test Failed: Validation succeeded despite namespace mismatch.");
            }
            else
            {
                 System.Console.WriteLine("Test Passed: Validation failed as expected.");
                 foreach(var msg in result.ValidationResultMessages) System.Console.WriteLine("Error: " + msg);
            }

            Assert.IsFalse(result.IsValid, "Should fail when XML namespace does not match Schema namespace.");
            Assert.IsTrue(result.ValidationResultMessages.Any(m => 
                m.Contains("Namespace Mismatch", StringComparison.OrdinalIgnoreCase) || 
                m.Contains("Could not find schema", StringComparison.OrdinalIgnoreCase)), 
                $"Should have specific error about namespace or schema. Got: {string.Join(", ", result.ValidationResultMessages)}");
        }

        [TestMethod]
        public void ExtractSoapBody_ShouldReturnInnerXml()
        {
            string soapResponse = @"<soapenv:Envelope
     xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/""
     xmlns:ns=""http://www.promostandards.org/WSDL/ProductDataService/2.0.0/""
     xmlns:shar=""http://www.promostandards.org/WSDL/ProductDataService/2.0.0/SharedObjects/"">
   <soapenv:Header />
   <soapenv:Body>
      <ns:GetProductResponse>
         <ns:Product>
            <!-- productId is missing -->
            <shar:productName>Mock Product Missing ID</shar:productName>
         </ns:Product>
      </ns:GetProductResponse>
   </soapenv:Body>
</soapenv:Envelope>";

            string extracted = ExtractSoapBody(soapResponse);
            System.Console.WriteLine("EXTRACTED: " + extracted);

            Assert.IsTrue(extracted.Contains("<ns:GetProductResponse"));
            Assert.IsFalse(extracted.Contains("soapenv:Body"));
        }

        // Copied from ValidationService.cs for verification
        private string ExtractSoapBody(string soapResponse)
        {
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.PreserveWhitespace = true; 
                doc.LoadXml(soapResponse);

                var namespaceManager = new System.Xml.XmlNamespaceManager(doc.NameTable);
                namespaceManager.AddNamespace("s", "http://schemas.xmlsoap.org/soap/envelope/");
                
                var bodyNode = doc.SelectSingleNode("//*[local-name()='Body']");
                if (bodyNode != null && bodyNode.HasChildNodes)
                {
                    foreach (System.Xml.XmlNode child in bodyNode.ChildNodes)
                    {
                        if (child.NodeType == System.Xml.XmlNodeType.Element)
                        {
                            return child.OuterXml;
                        }
                    }
                }
                
                return soapResponse; 
            }
            catch
            {
                return soapResponse; 
            }
        }
    }
}
