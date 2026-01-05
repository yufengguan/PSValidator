using PromoStandards.Validator.Api.Models;
using System.Xml.Schema;
using System.Xml;
using System.Diagnostics;

namespace PromoStandards.Validator.Api.Services;

public interface IValidationRequestService
{
    Task<ValidationResult> ValidateRequest(string xmlContent, string service, string version, string operation);
    Task<string> GenerateSampleRequest(string serviceName, string version, string operationName);
    Task<string> GetRequestSchema(string serviceName, string version, string operationName);
}

public class ValidationRequestService : BaseValidationService, IValidationRequestService
{
    public ValidationRequestService(IConfiguration configuration, ILogger<ValidationRequestService> logger) 
        : base(configuration, logger)
    {
    }

    public Task<ValidationResult> ValidateRequest(string xmlContent, string serviceName, string version, string operationName)
    {
        var sw = Stopwatch.StartNew();
        var result = new ValidationResult();
        

        try
        {
            // Format XML for line numbers in errors
            string contentToValidate = FormatXml(xmlContent);
            
            // Extract SOAP Body if present to avoid schema warnings
            contentToValidate = ExtractSoapBody(contentToValidate);
            
            result.ResponseContent = contentToValidate; // Use formatted/extracted XML as the content to return/display

            // 1. Find Schema Name
            var schemaName = GetRequestSchemaName(serviceName, version, operationName);
            if (string.IsNullOrEmpty(schemaName))
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add($"Could not find request schema definition for {serviceName} {version} {operationName}");
                return Task.FromResult(result);
            }

            // 2. Find Schema File
            var schemaPath = FindSchemaFile(schemaName, version, serviceName);
            if (string.IsNullOrEmpty(schemaPath))
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add($"Schema file {schemaName}.xsd not found for version {version}");
                return Task.FromResult(result);
            }

            // 3. Validate
            var validationResult = ValidateAgainstSchema(contentToValidate, schemaPath);
            result.IsValid = validationResult.IsValid;
            result.ValidationResultMessages = validationResult.ValidationResultMessages;
            
            sw.Stop();
            result.ResponseTimeMs = sw.Elapsed.TotalMilliseconds;
            
            if (!result.IsValid)
            {
                _logger.LogWarning("Request Validation Failed for {Service} {Version} {Operation}. Errors: {Errors} Content: {XmlContent}", serviceName, version, operationName, RedactSensitiveData(string.Join("; ", result.ValidationResultMessages)), RedactSensitiveData(xmlContent));
            }

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.ResponseTimeMs = sw.Elapsed.TotalMilliseconds;
            result.IsValid = false;
            result.ValidationResultMessages.Add($"System Error: {ex.Message}");
            _logger.LogError(ex, "System Error during request validation for {Service} {Version} {Operation} Content: {XmlContent}", serviceName, version, operationName, RedactSensitiveData(xmlContent));
            return Task.FromResult(result);
        }
    }
    
    // Sample generation logic moved here...
    public async Task<string> GenerateSampleRequest(string serviceName, string version, string operationName)
    {
        string targetElementName = null;
        string schemaPath = null;

        // 1. Try to get schema info from WSDL first (most accurate for Element Name)
        var wsdlName = GetRequestSchemaFromWsdl(serviceName, version, operationName);
        if (!string.IsNullOrEmpty(wsdlName))
        {
            targetElementName = wsdlName;
            schemaPath = FindSchemaFile(wsdlName, version, serviceName);
        }
        
        // 2. Fallback to JSON mapping if file not found
        if (string.IsNullOrEmpty(schemaPath))
        {
            var jsonName = GetRequestSchemaName(serviceName, version, operationName);
            if (!string.IsNullOrEmpty(jsonName))
            {
                schemaPath = FindSchemaFile(jsonName, version, serviceName);
                // If we didn't have a WSDL name, use the JSON name as the target element
                if (string.IsNullOrEmpty(targetElementName))
                {
                    targetElementName = jsonName;
                }
            }
        }

        if (string.IsNullOrEmpty(targetElementName))
        {
            return $"<!-- Schema/Element name not found for {serviceName} {version} {operationName} -->";
        }

        if (string.IsNullOrEmpty(schemaPath))
        {
            return $"<!-- Schema file for {targetElementName} (or fallback) not found for version {version} -->";
        }

        try
        {
            return await Task.Run(() => 
            {
                var schemaSet = new XmlSchemaSet();
                var resolver = new XmlUrlResolver();
                var schemaDirectory = Path.GetDirectoryName(schemaPath);
                var directoryUri = new Uri(Path.GetFullPath(schemaDirectory) + Path.DirectorySeparatorChar);
                schemaSet.XmlResolver = resolver;

                using (var schemaReader = XmlReader.Create(schemaPath, new XmlReaderSettings { XmlResolver = resolver }))
                {
                    var schema = XmlSchema.Read(schemaReader, null);
                    schema.SourceUri = directoryUri.ToString();
                    schemaSet.Add(schema);
                }
                schemaSet.Compile();

                XmlSchemaElement rootElement = null;
                foreach (XmlSchema schema in schemaSet.Schemas())
                {
                    foreach (XmlSchemaElement element in schema.Elements.Values)
                    {
                        if (element.Name.Equals(targetElementName, StringComparison.OrdinalIgnoreCase) || 
                            element.Name.Equals(operationName + "Request", StringComparison.OrdinalIgnoreCase))
                        {
                            rootElement = element;
                            break;
                        }
                    }
                    if (rootElement != null) break;
                }

                if (rootElement == null)
                    return $"<!-- Root element {targetElementName} not found in schema file -->";

                var doc = new XmlDocument();
                var xmlNode = GenerateElement(doc, rootElement, schemaSet, version);
                doc.AppendChild(xmlNode);

                var sb = new System.Text.StringBuilder();
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\r\n",
                    NewLineHandling = NewLineHandling.Replace,
                    OmitXmlDeclaration = true
                };
                using (var writer = XmlWriter.Create(sb, settings))
                {
                    doc.Save(writer);
                }

                return sb.ToString();
            });
        }
        catch (Exception ex)
        {
            return $"<!-- Error generating sample: {ex.Message} -->";
        }
    }

    private string GetRequestSchemaName(string serviceName, string version, string operationName)
    {
        var service = _serviceList?.FirstOrDefault(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        if (service == null) return null;

        var verParts = version.Split('.');
        if (verParts.Length != 3) return null;
        if (int.TryParse(verParts[0], out int major) && int.TryParse(verParts[1], out int minor) && int.TryParse(verParts[2], out int patch))
        {
            var ver = service.Versions.FirstOrDefault(v => v.Major == major && v.Minor == minor && v.Patch == patch);
            return ver?.Operations.FirstOrDefault(o => o.OperationName.Equals(operationName, StringComparison.OrdinalIgnoreCase))?.RequestSchema;
        }
        return null;
    }

    private string GetRequestSchemaFromWsdl(string serviceName, string version, string operationName)
    {
        try
        {
            var wsdlPath = FindWsdlFile(serviceName, version);
            if (string.IsNullOrEmpty(wsdlPath)) return null;

            var doc = new XmlDocument();
            doc.Load(wsdlPath);

            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("wsdl", "http://schemas.xmlsoap.org/wsdl/");

            var inputNode = doc.SelectSingleNode($"//wsdl:portType/wsdl:operation[@name='{operationName}']/wsdl:input", nsManager);
            if (inputNode == null) return null;

            var messageQName = inputNode.Attributes["message"]?.Value;
            if (string.IsNullOrEmpty(messageQName)) return null;
            var messageName = messageQName.Contains(":") ? messageQName.Split(':')[1] : messageQName;

            var partNode = doc.SelectSingleNode($"//wsdl:message[@name='{messageName}']/wsdl:part", nsManager);
            if (partNode == null) return null;

            var elementQName = partNode.Attributes["element"]?.Value;
            if (string.IsNullOrEmpty(elementQName)) return null;
            return elementQName.Contains(":") ? elementQName.Split(':')[1] : elementQName;
        }
        catch
        {
            return null;
        }
    }

    private string FindWsdlFile(string serviceName, string version)
    {
        var schemasDir = Path.Combine(_docsPath, "Schemas");
        var files = Directory.GetFiles(schemasDir, "*.wsdl", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            if ((file.Contains(Path.DirectorySeparatorChar + version + Path.DirectorySeparatorChar) ||
                 file.Contains(Path.DirectorySeparatorChar + "v" + version + Path.DirectorySeparatorChar)) &&
                (file.Contains(serviceName, StringComparison.OrdinalIgnoreCase) || 
                 file.Contains(serviceName.Replace("Service", ""), StringComparison.OrdinalIgnoreCase)))
            {
                return file;
            }
        }
        return null;
    }

    private XmlElement GenerateElement(XmlDocument doc, XmlSchemaElement element, XmlSchemaSet schemaSet, string version)
    {
        if (!element.RefName.IsEmpty)
        {
            var globalElement = schemaSet.GlobalElements[element.RefName] as XmlSchemaElement;
            if (globalElement != null) return GenerateElement(doc, globalElement, schemaSet, version);
            var missing = doc.CreateElement(element.RefName.Name, element.RefName.Namespace);
            missing.InnerText = "<!-- Reference not found -->";
            return missing;
        }

        var name = element.Name;
        var ns = element.QualifiedName.Namespace;
        var xmlElement = doc.CreateElement(name, ns);

        var complexType = element.ElementSchemaType as XmlSchemaComplexType;
        if (complexType != null)
        {
            GenerateComplexType(doc, xmlElement, complexType, schemaSet, version);
        }
        else
        {
            xmlElement.InnerText = GetSampleValue(element.ElementSchemaType, name, version);
        }

        return xmlElement;
    }

    private void GenerateComplexType(XmlDocument doc, XmlElement parent, XmlSchemaComplexType complexType, XmlSchemaSet schemaSet, string version)
    {
        if (complexType.Particle != null)
            GenerateParticle(doc, parent, complexType.Particle, schemaSet, version);
        
        if (complexType.ContentModel is XmlSchemaComplexContent complexContent && 
            complexContent.Content is XmlSchemaComplexContentExtension extension && 
            extension.Particle != null)
        {
            GenerateParticle(doc, parent, extension.Particle, schemaSet, version);
        }
    }

    private void GenerateParticle(XmlDocument doc, XmlElement parent, XmlSchemaParticle particle, XmlSchemaSet schemaSet, string version)
    {
        if (particle is XmlSchemaSequence sequence)
            foreach (XmlSchemaParticle item in sequence.Items) GenerateParticle(doc, parent, item, schemaSet, version);
        else if (particle is XmlSchemaChoice choice && choice.Items.Count > 0)
            GenerateParticle(doc, parent, (XmlSchemaParticle)choice.Items[0], schemaSet, version);
        else if (particle is XmlSchemaAll all)
            foreach (XmlSchemaParticle item in all.Items) GenerateParticle(doc, parent, item, schemaSet, version);
        else if (particle is XmlSchemaElement element)
            parent.AppendChild(GenerateElement(doc, element, schemaSet, version));
    }

    private string GetSampleValue(XmlSchemaType type, string elementName, string version)
    {
        // 1. Prioritize Schema Type Definitions
        if (type != null && type.Datatype != null)
        {
            // Check for enumerations (walk up the type hierarchy)
            var currentType = type;
            while (currentType is XmlSchemaSimpleType simpleType)
            {
                if (simpleType.Content is XmlSchemaSimpleTypeRestriction restriction)
                {
                    // Collect all enum values first
                    var enumValues = new List<string>();
                    bool hasEnums = false;
                    foreach (var facet in restriction.Facets)
                    {
                        if (facet is XmlSchemaEnumerationFacet enumFacet)
                        {
                            enumValues.Add(enumFacet.Value);
                            hasEnums = true;
                        }
                    }

                    if (hasEnums)
                    {
                        // Prefer specific defaults
                        if (enumValues.Contains("USD")) return "USD";
                        if (enumValues.Contains("US")) return "US";
                        
                        // Fallback to first available
                        return enumValues.Count > 0 ? enumValues[0] : "value";
                    }
                }
                currentType = simpleType.BaseXmlSchemaType;
            }

            switch (type.Datatype.TypeCode)
            {
                case XmlTypeCode.Boolean: return "true";
                case XmlTypeCode.DateTime: return DateTime.Now.ToString("s");
                case XmlTypeCode.Date: return DateTime.Now.ToString("yyyy-MM-dd");
                case XmlTypeCode.Int:
                case XmlTypeCode.Integer: return "1";
                case XmlTypeCode.Decimal: return "10.00";
            }
        }

        // 2. Fallback to heuristics based on element name
        if (elementName.Contains("id", StringComparison.OrdinalIgnoreCase)) return "test_id";
        if (elementName.Contains("password", StringComparison.OrdinalIgnoreCase)) return "test_password";
        if (elementName.Contains("version", StringComparison.OrdinalIgnoreCase)) return version;
        if (elementName.Contains("date", StringComparison.OrdinalIgnoreCase)) return DateTime.Now.ToString("s");
        if (elementName.Equals("localizationCountry", StringComparison.OrdinalIgnoreCase)) return "US";
        if (elementName.Equals("localizationLanguage", StringComparison.OrdinalIgnoreCase)) return "en";
        if (elementName.Equals("currency", StringComparison.OrdinalIgnoreCase)) return "USD";
        if (elementName.Equals("wsVersion", StringComparison.OrdinalIgnoreCase)) return version;

        string defaultValue = $"{elementName}_value";
        
        // Check for maxLength constraint
        if (type is XmlSchemaSimpleType sType && sType.Content is XmlSchemaSimpleTypeRestriction res)
        {
            foreach (var facet in res.Facets)
            {
                if (facet is XmlSchemaMaxLengthFacet maxLengthFacet && int.TryParse(maxLengthFacet.Value, out int maxLength))
                {
                    if (defaultValue.Length > maxLength)
                    {
                        return defaultValue.Substring(0, maxLength);
                    }
                }
            }
        }

        return defaultValue;
    }

    public async Task<string> GetRequestSchema(string serviceName, string version, string operationName)
    {
        string targetElementName = null;
        string schemaPath = null;
        
        // 1. Try WSDL first
        var wsdlName = GetRequestSchemaFromWsdl(serviceName, version, operationName);
        if (!string.IsNullOrEmpty(wsdlName))
        {
            schemaPath = FindSchemaFile(wsdlName, version, serviceName);
        }

        // 2. Fallback to JSON
        if (string.IsNullOrEmpty(schemaPath))
        {
            var jsonName = GetRequestSchemaName(serviceName, version, operationName);
            if (!string.IsNullOrEmpty(jsonName))
            {
                schemaPath = FindSchemaFile(jsonName, version, serviceName);
            }
        }

        if (string.IsNullOrEmpty(schemaPath)) return $"<!-- Request schema file not found for {serviceName} {version} {operationName} -->";

        var schemaContent = await File.ReadAllTextAsync(schemaPath);
        return FormatXml(schemaContent);
    }
}
