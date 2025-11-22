using System.Xml;
using System.Xml.Schema;
using System.Text.Json;
using PromoStandards.Validator.Api.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PromoStandards.Validator.Api.Services;

public class ValidationService : IValidationService
{
    private readonly string _docsPath;
    private readonly ILogger<ValidationService> _logger;
    private readonly List<ServiceInfo> _serviceList;

    public ValidationService(IConfiguration configuration, ILogger<ValidationService> logger)
    {
        _logger = logger;
        _docsPath = configuration["DocsPath"];
        
        if (string.IsNullOrEmpty(_docsPath))
        {
            throw new Exception("DocsPath is not configured.");
        }

        var jsonPath = Path.Combine(_docsPath, "PSServiceList.json");
        if (File.Exists(jsonPath))
        {
            var json = File.ReadAllText(jsonPath);
            _serviceList = JsonSerializer.Deserialize<List<ServiceInfo>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        else
        {
            _serviceList = new List<ServiceInfo>();
            _logger.LogError("PSServiceList.json not found.");
        }
    }

    public ValidationResult Validate(string xmlContent, string serviceName, string version, string operationName)
    {
        var result = new ValidationResult();
        
        try
        {
            // 1. Find Schema Name from Service List
            var schemaName = GetResponseSchemaName(serviceName, version, operationName);
            if (string.IsNullOrEmpty(schemaName))
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add($"Could not find schema definition for {serviceName} {version} {operationName}");
                return result;
            }

            // 2. Find Schema File
            var schemaPath = FindSchemaFile(schemaName, version);
            if (string.IsNullOrEmpty(schemaPath))
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add($"Schema file {schemaName}.xsd not found for version {version}");
                return result;
            }

            // 3. Validate
            var settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            
            // Create a resolver that uses the schema file's directory as base
            settings.XmlResolver = new XmlUrlResolver();

            // Load the schema
            // We add the schema to the set. The target namespace is usually defined in the XSD.
            // Passing null as targetNamespace causes it to be read from the XSD.
            settings.Schemas.Add(null, schemaPath);

            using (var stringReader = new StringReader(xmlContent))
            using (var xmlReader = XmlReader.Create(stringReader, settings))
            {
                try
                {
                    while (xmlReader.Read()) { }
                }
                catch (XmlException ex)
                {
                    result.IsValid = false;
                    result.ValidationResultMessages.Add($"XML Parsing Error: {ex.Message} at Line {ex.LineNumber}, Pos {ex.LinePosition}");
                }
            }

            // Capture validation events
            settings.ValidationEventHandler += (sender, args) =>
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add($"{args.Severity}: {args.Message} at Line {args.Exception.LineNumber}, Pos {args.Exception.LinePosition}");
            };

            // Re-read to trigger validation events
            // Wait, I need to attach the event handler BEFORE reading.
            // Let's refactor to do it properly.
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ValidationResultMessages.Add($"System Error: {ex.Message}");
        }

        if (result.ValidationResultMessages.Count == 0)
        {
            result.IsValid = true;
        }

        return result;
    }

    private ValidationResult ValidateInternal(string xmlContent, string schemaPath)
    {
        var result = new ValidationResult { IsValid = true };
        
        var settings = new XmlReaderSettings();
        settings.ValidationType = ValidationType.Schema;
        settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
        settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
        
        // Important: Set the XmlResolver to resolve relative imports
        settings.XmlResolver = new XmlUrlResolver();

        try 
        {
            settings.Schemas.Add(null, schemaPath);
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ValidationResultMessages.Add($"Error loading schema: {ex.Message}");
            return result;
        }

        settings.ValidationEventHandler += (sender, args) =>
        {
            result.IsValid = false;
            result.ValidationResultMessages.Add($"{args.Severity}: {args.Message} at Line {args.Exception.LineNumber}, Pos {args.Exception.LinePosition}");
        };

        try
        {
            using (var stringReader = new StringReader(xmlContent))
            using (var xmlReader = XmlReader.Create(stringReader, settings))
            {
                while (xmlReader.Read()) { }
            }
        }
        catch (XmlException ex)
        {
            result.IsValid = false;
            result.ValidationResultMessages.Add($"XML Parsing Error: {ex.Message} at Line {ex.LineNumber}, Pos {ex.LinePosition}");
        }

        return result;
    }

    private string GetResponseSchemaName(string serviceName, string version, string operationName)
    {
        var service = _serviceList?.FirstOrDefault(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        if (service == null) return null;

        // Version matching: "1.0.0" vs "1.0.0"
        // The JSON has Major, Minor, Patch.
        var verParts = version.Split('.');
        if (verParts.Length != 3) return null;
        if (!int.TryParse(verParts[0], out int major)) return null;
        if (!int.TryParse(verParts[1], out int minor)) return null;
        if (!int.TryParse(verParts[2], out int patch)) return null;

        var ver = service.Versions.FirstOrDefault(v => v.Major == major && v.Minor == minor && v.Patch == patch);
        if (ver == null) return null;

        var op = ver.Operations.FirstOrDefault(o => o.OperationName.Equals(operationName, StringComparison.OrdinalIgnoreCase));
        return op?.ResponseSchema;
    }

    private string FindSchemaFile(string schemaName, string version)
    {
        var schemasDir = Path.Combine(_docsPath, "Schemas");
        var files = Directory.GetFiles(schemasDir, $"{schemaName}.xsd", SearchOption.AllDirectories);
        
        // Filter by version in path
        // The version in path might be "1.0.0" or "2.0.0"
        // We look for the file that has the version string in its parent directory path
        
        foreach (var file in files)
        {
            // Check if version string is part of the path
            // e.g. ...\2.0.0\GetInventoryLevelsResponse.xsd
            if (file.Contains(Path.DirectorySeparatorChar + version + Path.DirectorySeparatorChar))
            {
                return file;
            }
        }

        // Fallback: if only one found, return it
        if (files.Length == 1) return files[0];

        return null;
    }

    // Helper classes for JSON deserialization
    private class ServiceInfo
    {
        public string? ServiceName { get; set; }
        public List<VersionInfo>? Versions { get; set; }
    }

    private class VersionInfo
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public List<OperationInfo>? Operations { get; set; }
    }

    private class OperationInfo
    {
        public string? OperationName { get; set; }
        public string? ResponseSchema { get; set; }
    }
}
