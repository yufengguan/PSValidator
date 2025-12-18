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
    private readonly IHttpClientFactory _httpClientFactory;

    public ValidationService(IConfiguration configuration, ILogger<ValidationService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
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

    public async Task<ValidationResult> Validate(string xmlContent, string serviceName, string version, string operationName, string endpoint)
    {
        var result = new ValidationResult();
        string contentToValidate = xmlContent;

        try
        {
            // 0. If endpoint is provided, call it
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                try 
                {
                    // Validate URL format before making request
                    if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                    {
                        result.IsValid = false;
                        result.ValidationResultMessages.Add($"Invalid Endpoint URL: The URL format is not valid. Please ensure it starts with http:// or https://");
                        return result;
                    }

                    var client = _httpClientFactory.CreateClient();
                    
                    // Wrap in SOAP Envelope
                    var soapRequest = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
                                            <soapenv:Header/>
                                            <soapenv:Body>
                                                {xmlContent}
                                            </soapenv:Body>
                                         </soapenv:Envelope>";

                    var content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml");
                    
                    // Add SOAPAction header (required by many SOAP services)
                    // We might need to look this up, but often "" or the operation name works.
                    // For now, let's try adding a generic one or leaving it to the client config if needed.
                    if (!client.DefaultRequestHeaders.Contains("SOAPAction"))
                    {
                        client.DefaultRequestHeaders.Add("SOAPAction", $"\"{operationName}\"");
                    }
                    
                    var response = await client.PostAsync(endpoint, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    
                    // Display the full SOAP response (with envelope)
                    result.ResponseContent = responseString;
                    
                    // But validate only the unwrapped body
                    contentToValidate = ExtractSoapBody(responseString);

                    if (!response.IsSuccessStatusCode)
                    {
                        result.ValidationResultMessages.Add($"HTTP Error: {response.StatusCode} - {responseString}");
                        result.IsValid = false;
                        return result;
                    }
                }
                catch (UriFormatException ex)
                {
                    result.IsValid = false;
                    result.ValidationResultMessages.Add($"Invalid Endpoint URL: {ex.Message}. Please ensure the URL is properly formatted (e.g., https://example.com/api)");
                    return result;
                }
                catch (HttpRequestException ex)
                {
                    result.IsValid = false;
                    result.ValidationResultMessages.Add($"Network Error: Unable to connect to the endpoint. {ex.Message}");
                    return result;
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.ValidationResultMessages.Add($"Error: {ex.Message}");
                    return result;
                }
            }
            else
            {
                // Fallback: Validate the request XML itself (as before)
                result.ResponseContent = xmlContent;
            }

            // 123. The user wants human-readable line numbers in validation errors.
            contentToValidate = FormatXml(contentToValidate);
            
            // Check for SOAP Fault
            var soapFaultError = ParseSoapFault(contentToValidate);
            if (!string.IsNullOrEmpty(soapFaultError))
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add(soapFaultError);
                // Even if it's a fault, we might want to return the formatted content
                 if (result.ResponseContent == xmlContent || result.ResponseContent == contentToValidate) 
                {
                     result.ResponseContent = contentToValidate;
                } 
                else if (!string.IsNullOrEmpty(result.ResponseContent))
                {
                    result.ResponseContent = FormatXml(result.ResponseContent);
                }
                return result;
            }

            // Also update the displayed response content if we haven't already (or overwrite with formatted)
            // If result.ResponseContent differs from contentToValidate only by SOAP envelope, we might want to format ResponseContent too.
            // But usually validation errors refer to contentToValidate lines.
            // Let's format the displayed content too for consistency if it matches.
            if (result.ResponseContent == xmlContent || result.ResponseContent == contentToValidate) 
            {
                 result.ResponseContent = contentToValidate;
            } 
            else if (!string.IsNullOrEmpty(result.ResponseContent))
            {
                // If it's a SOAP response, try to format that too
                result.ResponseContent = FormatXml(result.ResponseContent);
            }

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
            result.IsValid = true; // Assume valid, set to false on error
            
            var settingsWithHandler = new XmlReaderSettings();
            settingsWithHandler.ValidationType = ValidationType.Schema;
            settingsWithHandler.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            settingsWithHandler.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            // Don't report warnings - only errors. This allows optional fields to be missing.
            // settingsWithHandler.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            
            // CRITICAL: Set the XmlResolver with the base URI to resolve relative schema imports
            var resolver = new XmlUrlResolver();
            var schemaDirectory = Path.GetDirectoryName(schemaPath);
            // Convert Windows path to file URI (e.g., C:\Path -> file:///C:/Path/)
            var directoryUri = new Uri(Path.GetFullPath(schemaDirectory) + Path.DirectorySeparatorChar);
            settingsWithHandler.XmlResolver = resolver;
            
            // Load the schema with the base URI so imports can be resolved
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
                settingsWithHandler.Schemas = schemaSet;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add($"Error loading schema: {ex.Message}");
                return result;
            }
            
            settingsWithHandler.ValidationEventHandler += (sender, args) =>
            {
                // Only report errors, not warnings
                if (args.Severity == System.Xml.Schema.XmlSeverityType.Error)
                {
                    result.IsValid = false;
                    result.ValidationResultMessages.Add($"{args.Severity}: {args.Message} at Line {args.Exception.LineNumber}, Pos {args.Exception.LinePosition}");
                }
            };

            using (var stringReader = new StringReader(contentToValidate))
            using (var xmlReader = XmlReader.Create(stringReader, settingsWithHandler))
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


    private string ExtractSoapBody(string soapResponse)
    {
        try
        {
            var doc = new XmlDocument();
            doc.PreserveWhitespace = true; // Preserve original formatting
            doc.LoadXml(soapResponse);

            var namespaceManager = new XmlNamespaceManager(doc.NameTable);
            namespaceManager.AddNamespace("s", "http://schemas.xmlsoap.org/soap/envelope/");
            // Handle both "s" and "soapenv" prefixes or just search for local name "Body"
            
            var bodyNode = doc.SelectSingleNode("//*[local-name()='Body']");
            if (bodyNode != null && bodyNode.HasChildNodes)
            {
                // With PreserveWhitespace, matching first child might be text/whitespace. 
                // We want the first ELEMENT.
                foreach (XmlNode child in bodyNode.ChildNodes)
                {
                    if (child.NodeType == XmlNodeType.Element)
                    {
                        return child.OuterXml;
                    }
                }
            }
            
            return soapResponse; // Fallback if no body payload found
        }
        catch
        {
            return soapResponse; // Fallback on error
        }
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

    public async Task<string> GenerateSampleRequest(string serviceName, string version, string operationName)
    {
        // ... (existing implementation) ...
        // 1. Try to get schema from WSDL first (most accurate)
        var schemaName = GetRequestSchemaFromWsdl(serviceName, version, operationName);
        
        // 2. Fallback to JSON mapping if WSDL lookup fails
        if (string.IsNullOrEmpty(schemaName))
        {
            schemaName = GetRequestSchemaName(serviceName, version, operationName);
        }

        if (string.IsNullOrEmpty(schemaName))
        {
            return $"<!-- Schema not found for {serviceName} {version} {operationName} -->";
        }

        var schemaPath = FindSchemaFile(schemaName, version);
        if (string.IsNullOrEmpty(schemaPath))
        {
            return $"<!-- Schema file {schemaName}.xsd not found for version {version} -->";
        }

        try
        {
            var schemaSet = new XmlSchemaSet();
            var resolver = new XmlUrlResolver();
            var schemaDirectory = Path.GetDirectoryName(schemaPath);
            // Convert Windows path to file URI (e.g., C:\Path -> file:///C:/Path/)
            var directoryUri = new Uri(Path.GetFullPath(schemaDirectory) + Path.DirectorySeparatorChar);
            
            // CRITICAL: Set the resolver on the set itself so it can resolve imports during Compile
            schemaSet.XmlResolver = resolver;

            using (var schemaReader = XmlReader.Create(schemaPath, new XmlReaderSettings { XmlResolver = resolver }))
            {
                var schema = XmlSchema.Read(schemaReader, (sender, args) => 
                {
                    // Handle read errors if any
                    // Console.WriteLine($"Schema Read Warning: {args.Message}");
                });
                schema.SourceUri = directoryUri.ToString();
                schemaSet.Add(schema);
            }
            
            schemaSet.Compile();

            // Find the root element (usually matches schema name or operation name + "Request")
            XmlSchemaElement rootElement = null;
            foreach (XmlSchema schema in schemaSet.Schemas())
            {
                foreach (XmlSchemaElement element in schema.Elements.Values)
                {
                    // Match exact name, or name with "Request" suffix, or the resolved schema name
                    if (element.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase) || 
                        element.Name.Equals(operationName + "Request", StringComparison.OrdinalIgnoreCase))
                    {
                        rootElement = element;
                        break;
                    }
                }
                if (rootElement != null) break;
            }

            if (rootElement == null)
            {
                return $"<!-- Root element {schemaName} not found in schema file -->";
            }

            var doc = new XmlDocument();
            var nsManager = new XmlNamespaceManager(doc.NameTable);
            
            // Generate XML
            var xmlNode = GenerateElement(doc, rootElement, schemaSet);
            doc.AppendChild(xmlNode);

            // Format XML
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
        }
        catch (Exception ex)
        {
            return $"<!-- Error generating sample: {ex.Message} -->";
        }
    }

    public async Task<string> GetResponseSchema(string serviceName, string version, string operationName)
    {
        var schemaName = GetResponseSchemaName(serviceName, version, operationName);
        if (string.IsNullOrEmpty(schemaName))
        {
            return $"<!-- Response schema name not found for {serviceName} {version} {operationName} -->";
        }

        var schemaPath = FindSchemaFile(schemaName, version);
        if (string.IsNullOrEmpty(schemaPath))
        {
            return $"<!-- Schema file {schemaName}.xsd not found for version {version} -->";
        }

        try
        {
            return await File.ReadAllTextAsync(schemaPath);
        }
        catch (Exception ex)
        {
            return $"<!-- Error reading schema file: {ex.Message} -->";
        }
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
            nsManager.AddNamespace("xsd", "http://www.w3.org/2001/XMLSchema");

            // 1. Find portType operation input message name
            // <wsdl:portType name="..."> <wsdl:operation name="operationName"> <wsdl:input message="tns:MessageName"/>
            var inputNode = doc.SelectSingleNode($"//wsdl:portType/wsdl:operation[@name='{operationName}']/wsdl:input", nsManager);
            if (inputNode == null) return null;

            var messageQName = inputNode.Attributes["message"]?.Value;
            if (string.IsNullOrEmpty(messageQName)) return null;

            var messageName = messageQName.Contains(":") ? messageQName.Split(':')[1] : messageQName;

            // 2. Find message part element
            // <wsdl:message name="MessageName"> <wsdl:part name="..." element="tns:ElementName"/>
            var partNode = doc.SelectSingleNode($"//wsdl:message[@name='{messageName}']/wsdl:part", nsManager);
            if (partNode == null) return null;

            var elementQName = partNode.Attributes["element"]?.Value;
            if (string.IsNullOrEmpty(elementQName)) return null;

            var elementName = elementQName.Contains(":") ? elementQName.Split(':')[1] : elementQName;
            return elementName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing WSDL for schema name");
            return null;
        }
    }

    private string FindSchemaFile(string schemaName, string version)
    {
        var schemasDir = Path.Combine(_docsPath, "Schemas");
        var files = Directory.GetFiles(schemasDir, $"{schemaName}.xsd", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            // Check if version string is part of the path
            // Handle "1.0.0" and "v1.0.0"
            if (file.Contains(Path.DirectorySeparatorChar + version + Path.DirectorySeparatorChar) ||
                file.Contains(Path.DirectorySeparatorChar + "v" + version + Path.DirectorySeparatorChar))
            {
                return file;
            }
        }

        // Removed aggressive fallback that was returning wrong versions (e.g. 2.0.0 for 1.2.1)
        return null;
    }

    private XmlElement GenerateElement(XmlDocument doc, XmlSchemaElement element, XmlSchemaSet schemaSet)
    {
        // Handle Reference
        if (!element.RefName.IsEmpty)
        {
            var globalElement = schemaSet.GlobalElements[element.RefName] as XmlSchemaElement;
            if (globalElement != null)
            {
                return GenerateElement(doc, globalElement, schemaSet);
            }
            // If reference not found, create a placeholder
            var missing = doc.CreateElement(element.RefName.Name, element.RefName.Namespace);
            missing.InnerText = "<!-- Reference not found -->";
            return missing;
        }

        var name = element.Name;
        var ns = element.QualifiedName.Namespace;
        
        var xmlElement = doc.CreateElement(name, ns);

        // Handle complex types
        var complexType = element.ElementSchemaType as XmlSchemaComplexType;
        if (complexType != null)
        {
            GenerateComplexType(doc, xmlElement, complexType, schemaSet);
        }
        else
        {
            // Simple type - add sample value
            xmlElement.InnerText = GetSampleValue(element.ElementSchemaType, name);
        }

        return xmlElement;
    }

    private void GenerateComplexType(XmlDocument doc, XmlElement parent, XmlSchemaComplexType complexType, XmlSchemaSet schemaSet)
    {
        var particle = complexType.Particle;
        if (particle != null)
        {
            GenerateParticle(doc, parent, particle, schemaSet);
        }
        
        // Handle complex content (inheritance)
        if (complexType.ContentModel is XmlSchemaComplexContent complexContent)
        {
            if (complexContent.Content is XmlSchemaComplexContentExtension extension)
            {
                if (extension.Particle != null)
                {
                    GenerateParticle(doc, parent, extension.Particle, schemaSet);
                }
                // Also handle base type if needed, but usually extension includes base particles in compiled schema? 
                // Actually, we might need to look up base type. 
                // For simplicity, let's assume the compiled schema handles this or we just process the extension particle.
            }
        }
    }

    private void GenerateParticle(XmlDocument doc, XmlElement parent, XmlSchemaParticle particle, XmlSchemaSet schemaSet)
    {
        if (particle is XmlSchemaSequence sequence)
        {
            foreach (XmlSchemaParticle item in sequence.Items)
            {
                GenerateParticle(doc, parent, item, schemaSet);
            }
        }
        else if (particle is XmlSchemaChoice choice)
        {
            // For choice, just pick the first one for sample
            if (choice.Items.Count > 0)
            {
                GenerateParticle(doc, parent, (XmlSchemaParticle)choice.Items[0], schemaSet);
            }
        }
        else if (particle is XmlSchemaAll all)
        {
            foreach (XmlSchemaParticle item in all.Items)
            {
                GenerateParticle(doc, parent, item, schemaSet);
            }
        }
        else if (particle is XmlSchemaElement element)
        {
            // Check max occurs? For sample, just generate one.
            var child = GenerateElement(doc, element, schemaSet);
            parent.AppendChild(child);
        }
    }

    private string GetSampleValue(XmlSchemaType type, string elementName)
    {
        // Basic heuristics for sample values
        if (elementName.Contains("id", StringComparison.OrdinalIgnoreCase)) return "test_id";
        if (elementName.Contains("password", StringComparison.OrdinalIgnoreCase)) return "test_password";
        if (elementName.Contains("version", StringComparison.OrdinalIgnoreCase)) return "1.0.0";
        if (elementName.Contains("date", StringComparison.OrdinalIgnoreCase)) return DateTime.Now.ToString("s");
        if (elementName.Equals("localizationCountry", StringComparison.OrdinalIgnoreCase)) return "US";
        if (elementName.Equals("localizationLanguage", StringComparison.OrdinalIgnoreCase)) return "en";
        if (elementName.Equals("currency", StringComparison.OrdinalIgnoreCase)) return "USD";
        
        if (type != null && type.Datatype != null)
        {
            switch (type.Datatype.TypeCode)
            {
                case XmlTypeCode.Boolean: return "true";
                case XmlTypeCode.DateTime: return DateTime.Now.ToString("s");
                case XmlTypeCode.Int:
                case XmlTypeCode.Integer: return "1";
                case XmlTypeCode.Decimal: return "10.00";
            }
        }

        return $"{elementName}_value";
    }

    private string GetRequestSchemaName(string serviceName, string version, string operationName)
    {
        var service = _serviceList?.FirstOrDefault(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        if (service == null) return null;

        var verParts = version.Split('.');
        if (verParts.Length != 3) return null;
        if (!int.TryParse(verParts[0], out int major)) return null;
        if (!int.TryParse(verParts[1], out int minor)) return null;
        if (!int.TryParse(verParts[2], out int patch)) return null;

        var ver = service.Versions.FirstOrDefault(v => v.Major == major && v.Minor == minor && v.Patch == patch);
        if (ver == null) return null;

        var op = ver.Operations.FirstOrDefault(o => o.OperationName.Equals(operationName, StringComparison.OrdinalIgnoreCase));
        return op?.RequestSchema;
    }

    private string FindWsdlFile(string serviceName, string version)
    {
        var schemasDir = Path.Combine(_docsPath, "Schemas");
        // Look for .wsdl files
        var files = Directory.GetFiles(schemasDir, "*.wsdl", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            // Check if version string is part of the path
            // Handle "1.0.0" and "v1.0.0"
            if (file.Contains(Path.DirectorySeparatorChar + version + Path.DirectorySeparatorChar) ||
                file.Contains(Path.DirectorySeparatorChar + "v" + version + Path.DirectorySeparatorChar))
            {
                // Also check if service name matches (fuzzy match or part of path)
                // e.g. ...\InventoryService\2.0.0\InventoryService.wsdl
                if (file.Contains(serviceName, StringComparison.OrdinalIgnoreCase) || 
                    file.Contains(serviceName.Replace("Service", ""), StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }
        }
        
        return null;
    }

    private class OperationInfo
    {
        public string? OperationName { get; set; }
        public string? RequestSchema { get; set; }
        public string? ResponseSchema { get; set; }
    }

    private string ParseSoapFault(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            var nsManager = new XmlNamespaceManager(doc.NameTable);
            nsManager.AddNamespace("s", "http://schemas.xmlsoap.org/soap/envelope/");
            
            // Look for Fault element. It could be root or inside Body.
            // 1. Check if root is Fault
            var faultNode = doc.SelectSingleNode("//*[local-name()='Fault']");
            
            if (faultNode != null)
            {
                // Extract faultstring
                var faultString = faultNode.SelectSingleNode("//*[local-name()='faultstring']")?.InnerText;
                
                // Extract detailed message if available (e.g. from ExceptionDetail)
                var detailMessage = faultNode.SelectSingleNode("//*[local-name()='ExceptionDetail']/*[local-name()='Message']")?.InnerText;

                if (!string.IsNullOrWhiteSpace(detailMessage))
                {
                    return $"SOAP Fault: {detailMessage}";
                }
                
                if (!string.IsNullOrWhiteSpace(faultString))
                {
                     return $"SOAP Fault: {faultString}";
                }
                
                return "SOAP Fault detected but no detail message found.";
            }

            return null;
        }
        catch
        {
            // If parsing fails, it's likely not a fault or invalid XML, which normal validation will catch
            return null;
        }
    }


    private string FormatXml(string xml)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(xml)) return xml;
            
            var doc = System.Xml.Linq.XDocument.Parse(xml);
            return doc.ToString(); 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FormatXml Failed. Input length: {Length}. Snippet: {Snippet}", xml.Length, xml.Substring(0, Math.Min(xml.Length, 100)));
            return xml;
        }
    }
}
