using System.Text.Json;
using System.Xml;
using System.Xml.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PromoStandards.Validator.Api.Models;

namespace PromoStandards.Validator.Api.Services;

public abstract class BaseValidationService
{
    protected readonly string _docsPath;
    protected readonly ILogger _logger;
    protected readonly List<ServiceInfo> _serviceList;

    protected BaseValidationService(IConfiguration configuration, ILogger logger)
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
            _serviceList = JsonSerializer.Deserialize<List<ServiceInfo>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<ServiceInfo>();
        }
        else
        {
            _serviceList = new List<ServiceInfo>();
            _logger.LogError("PSServiceList.json not found.");
        }
    }

    protected string FindSchemaFile(string schemaName, string version, string fallbackName = null)
    {
        var schemasDir = Path.Combine(_docsPath, "Schemas");
        
        // Helper to search for a file
        string SearchForFile(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var files = Directory.GetFiles(schemasDir, $"{name}.xsd", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                if (file.Contains(Path.DirectorySeparatorChar + version + Path.DirectorySeparatorChar) ||
                    file.Contains(Path.DirectorySeparatorChar + "v" + version + Path.DirectorySeparatorChar))
                {
                    return file;
                }
            }
            return null;
        }

        var path = SearchForFile(schemaName);
        if (!string.IsNullOrEmpty(path)) return path;

        if (!string.IsNullOrEmpty(fallbackName))
        {
            return SearchForFile(fallbackName);
        }

        return null;
    }

    public ValidationResult ValidateAgainstSchema(string xmlContent, string schemaPath)
    {
        var result = new ValidationResult { IsValid = true };
        
        var settings = new XmlReaderSettings();
        settings.ValidationType = ValidationType.Schema;
        settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
        settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
        // Don't report warnings, only errors
        
        var resolver = new XmlUrlResolver();
        var schemaDirectory = Path.GetDirectoryName(schemaPath);
        var directoryUri = new Uri(Path.GetFullPath(schemaDirectory) + Path.DirectorySeparatorChar);
        settings.XmlResolver = resolver;

        try
        {
            var schemaSet = new XmlSchemaSet();
            schemaSet.XmlResolver = resolver;
            using (var schemaReader = XmlReader.Create(schemaPath, new XmlReaderSettings { XmlResolver = resolver }))
            {
                var schema = XmlSchema.Read(schemaReader, null);
                schema.SourceUri = directoryUri.ToString();
                schemaSet.Add(schema);
            }
            schemaSet.Compile();
            settings.Schemas = schemaSet;
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ValidationResultMessages.Add($"Error loading schema: {ex.Message}");
            return result;
        }
        
        settings.ValidationEventHandler += (sender, args) =>
        {
            if (args.Severity == XmlSeverityType.Error)
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add($"{args.Severity}: {args.Message} at Line {args.Exception.LineNumber}, Pos {args.Exception.LinePosition}");
            }
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

    public string FormatXml(string xml)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(xml)) return xml;

            var doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(xml);

            var sb = new System.Text.StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                OmitXmlDeclaration = true // Optional, but usually cleaner for display
            };

            using (var writer = XmlWriter.Create(sb, settings))
            {
                doc.Save(writer);
            }

            var formatted = sb.ToString();

            // Post-process to wrap attributes for better readability
            // 1. Wrap xmlns definitions
            formatted = formatted.Replace(" xmlns:", "\r\n     xmlns:");
            
            // 2. Wrap other common long attributes in XSDs
            formatted = formatted.Replace(" targetNamespace=", "\r\n     targetNamespace=");
            formatted = formatted.Replace(" elementFormDefault=", "\r\n     elementFormDefault=");
            formatted = formatted.Replace(" xsi:schemaLocation=", "\r\n     xsi:schemaLocation=");

            return formatted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FormatXml Failed");
            return xml;
        }
    }

    // Helper classes for JSON deserialization
    protected class ServiceInfo
    {
        public string? ServiceName { get; set; }
        public List<VersionInfo>? Versions { get; set; }
    }

    protected class VersionInfo
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public List<OperationInfo>? Operations { get; set; }
    }

    protected class OperationInfo
    {
        public string? OperationName { get; set; }
        public string? RequestSchema { get; set; }
        public string? ResponseSchema { get; set; }
    }
}
