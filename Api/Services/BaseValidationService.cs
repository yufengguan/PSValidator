using System.Text.Json;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;
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
        
        var resolver = new XmlUrlResolver();
        var schemaDirectory = Path.GetDirectoryName(schemaPath);
        var directoryUri = new Uri(Path.GetFullPath(schemaDirectory) + Path.DirectorySeparatorChar);

        var schemaSet = new XmlSchemaSet();
        schemaSet.XmlResolver = resolver;

        try
        {
            using (var schemaReader = XmlReader.Create(schemaPath, new XmlReaderSettings { XmlResolver = resolver }))
            {
                var schema = XmlSchema.Read(schemaReader, null);
                schema.SourceUri = directoryUri.ToString();
                schemaSet.Add(schema);
            }
            schemaSet.Compile();
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ValidationResultMessages.Add($"Error loading schema: {ex.Message}");
            return result;
        }

        try
        {
            // Use XDocument for validation to avoid double-validation and retain DOM context
            // LoadOptions.SetLineInfo allows us to report line numbers during DOM validation
            var doc = System.Xml.Linq.XDocument.Parse(xmlContent, System.Xml.Linq.LoadOptions.SetLineInfo);
            
            // XDocument.Validate extension method
            doc.Validate(schemaSet, (sender, args) =>
            {
                // Capture both Errors and Warnings
                if (args.Severity == XmlSeverityType.Error)
                {
                    result.IsValid = false;
                }
                result.ValidationResultMessages.Add($"{args.Severity}: {args.Message} at Line {args.Exception.LineNumber}, Pos {args.Exception.LinePosition}");
            });
        }
        catch (System.Xml.XmlException ex)
        {
            result.IsValid = false;
            result.ValidationResultMessages.Add($"XML Parsing Error: {ex.Message} at Line {ex.LineNumber}, Pos {ex.LinePosition}");
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ValidationResultMessages.Add($"Validation Error: {ex.Message}");
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
            _logger.LogWarning(ex, "FormatXml Failed for XML: {XmlContent}", xml.Length > 1000 ? RedactSensitiveData(xml.Substring(0, 1000)) : RedactSensitiveData(xml));
            return xml;
        }
    }

    protected string ExtractSoapBody(string soapXml)
    {
        try
        {
            // Simple check to avoid parsing if it doesn't look like SOAP
            if (string.IsNullOrWhiteSpace(soapXml) || !soapXml.Contains(":Envelope") || !soapXml.Contains(":Body"))
            {
                return soapXml;
            }

            var doc = new XmlDocument();
            doc.PreserveWhitespace = true; // Important to keep formatting
            doc.LoadXml(soapXml);
            
            var bodyNode = doc.SelectSingleNode("//*[local-name()='Body']");
            if (bodyNode != null && bodyNode.HasChildNodes)
            {
                foreach (XmlNode child in bodyNode.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                    {
                        return child.OuterXml;
                    }
                }
            }
            return soapXml; 
        }
        catch
        {
            return soapXml; 
        }
    }

    protected string RedactSensitiveData(string content)
    {
        if (string.IsNullOrEmpty(content)) return content;
        
        try
        {
            // Regex to find XML password tags and replace content with *****
            // Handles <password>value</password>, <Password>value</Password>, <ns:password>value</ns:password>
            // This simple regex assumes relatively well-formed XML tag structure
            return System.Text.RegularExpressions.Regex.Replace(
                content, 
                @"(<[a-zA-Z0-9_:]*[Pp]assword[^>]*>)(.*?)(<\/[a-zA-Z0-9_:]*[Pp]assword>)", 
                "$1*****$3", 
                System.Text.RegularExpressions.RegexOptions.Singleline);
        }
        catch
        {
            return content; // If regex fails, return original (or could return "Error redacting")
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
