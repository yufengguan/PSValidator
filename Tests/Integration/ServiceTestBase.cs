using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Text.Json;
using System.IO;

namespace PromoStandards.Validator.Tests.Integration;

public abstract class ServiceTestBase
{
    protected HttpClient _client = null!;
    protected string _mockServiceBaseUrl = null!;
    protected string _projectRoot = null!;

    [TestInitialize]
    public virtual void TestInitialize()
    {
        _client = new HttpClient()
        {
            BaseAddress = new Uri(TestConfig.ApiBaseUrl)
        };
        _mockServiceBaseUrl = TestConfig.StubServerBaseUrl.TrimEnd('/');
        
        // Hardcoded path to project root based on user environment
        _projectRoot = @"c:\Projects\PS.SC\PSValidator";
    }

    protected string NormalizeErrorMessage(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage)) return string.Empty;

        // Remove "Error:" prefix
        var normalized = errorMessage.Replace("Error:", "").Trim();

        // Remove "Line: X, Position: Y" prefix (Official Validator style)
        // Regex to match "Line: \d+, Position: \d+"
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"^Line:\s*\d+,\s*Position:\s*\d+\s*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove "at Line X, Pos Y" suffix (Local Validator style)
        // Regex to match "at Line \d+, Pos \d+$"
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s*at\s+Line\s+\d+,\s+Pos\s+\d+\s*$", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Remove surrounding quotes if present (Official validator messages sometimes wrapped in quotes)
        normalized = normalized.Trim('"');

        return normalized.Trim();
    }
}

// Helper classes shared across tests
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
