using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PromoStandards.Validator.Api.Services;

namespace PromoStandards.Validator.Tests.Integration;

[TestClass]
public class RedactionTests
{
    private class TestValidationService : BaseValidationService
    {
        public TestValidationService(IConfiguration configuration, ILogger logger) : base(configuration, logger)
        {
        }

        public string PublicRedactSensitiveData(string content)
        {
            return base.RedactSensitiveData(content);
        }
    }

    [TestMethod]
    public void RedactSensitiveData_ShouldMaskPasswordContent()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["DocsPath"]).Returns("C:\\Temp"); // Dummy path
        var loggerMock = new Mock<ILogger>();
        
        var service = new TestValidationService(configMock.Object, loggerMock.Object);
        var input = "<soapenv:Envelope><soapenv:Body><Login><password>Secret123!</password></Login></soapenv:Body></soapenv:Envelope>";
        var expected = "<soapenv:Envelope><soapenv:Body><Login><password>*****</password></Login></soapenv:Body></soapenv:Envelope>";

        // Act
        var result = service.PublicRedactSensitiveData(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void RedactSensitiveData_ShouldMaskPasswordWithNamespace()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["DocsPath"]).Returns("C:\\Temp");
        var loggerMock = new Mock<ILogger>();
        
        var service = new TestValidationService(configMock.Object, loggerMock.Object);
        var input = "<ns1:Password>StrongPass</ns1:Password>";
        var expected = "<ns1:Password>*****</ns1:Password>";

        // Act
        var result = service.PublicRedactSensitiveData(input);

        // Assert
        Assert.AreEqual(expected, result);
    }
    
    [TestMethod]
    public void RedactSensitiveData_ShouldMaskPasswordWithAttributes()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["DocsPath"]).Returns("C:\\Temp");
        var loggerMock = new Mock<ILogger>();
        
        var service = new TestValidationService(configMock.Object, loggerMock.Object);
        var input = "<password id=\"123\">admin</password>";
        var expected = "<password id=\"123\">*****</password>";

        // Act
        var result = service.PublicRedactSensitiveData(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void RedactSensitiveData_ShouldMaskMultiplePasswords()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["DocsPath"]).Returns("C:\\Temp");
        var loggerMock = new Mock<ILogger>();

        var service = new TestValidationService(configMock.Object, loggerMock.Object);
        var input = "<root><password>pass1</password><other>data</other><ns:Password>pass2</ns:Password></root>";
        var expected = "<root><password>*****</password><other>data</other><ns:Password>*****</ns:Password></root>";

        // Act
        var result = service.PublicRedactSensitiveData(input);

        // Assert
        Assert.AreEqual(expected, result);
    }
}
