using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace PromoStandards.Validator.Tests.Integration
{
    public static class TestConfig
    {
        private static IConfiguration _configuration;

        static TestConfig()
        {
            var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true)
                .AddEnvironmentVariables();

            _configuration = builder.Build();
        }

        public static string ApiBaseUrl => _configuration["ApiBaseUrl"] ?? "http://localhost:5166";
        public static string StubServerBaseUrl => _configuration["StubServerBaseUrl"] ?? "http://localhost:5086";

        public static string GetDocsPath()
        {
            // The test project runs from Tests/Integration/bin/Debug/net8.0
            // We need to navigate to the solution root and then to Docs
            var testProjectDir = Directory.GetCurrentDirectory();

            // Go up from bin/Debug/net8.0 to Integration, then Tests, then to solution root
            var solutionDir = Path.GetFullPath(Path.Combine(testProjectDir, "..", "..", "..", "..", ".."));
            var docsPath = Path.Combine(solutionDir, "Docs");

            // Verify the path exists
            if (!Directory.Exists(docsPath))
            {
                throw new DirectoryNotFoundException($"Docs directory not found at: {docsPath}");
            }
            return docsPath;
        }
    }
}
