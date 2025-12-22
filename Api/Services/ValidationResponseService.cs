using PromoStandards.Validator.Api.Models;
using System.Xml;
using System.Diagnostics;

namespace PromoStandards.Validator.Api.Services;

public interface IValidationResponseService
{
    Task<ValidationResult> ValidateResponse(string xmlContent, string service, string version, string operation, string endpoint);
    Task<string> GetResponseSchema(string serviceName, string version, string operationName);
}

public class ValidationResponseService : BaseValidationService, IValidationResponseService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ValidationResponseService(IConfiguration configuration, ILogger<ValidationResponseService> logger, IHttpClientFactory httpClientFactory) 
        : base(configuration, logger)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ValidationResult> ValidateResponse(string xmlContent, string serviceName, string version, string operationName, string endpoint)
    {
        var sw = Stopwatch.StartNew();
        var result = new ValidationResult();
        string contentToValidate = xmlContent;

        try
        {
            // 0. If endpoint is provided, call it
            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                try 
                {
                    if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
                    {
                        result.IsValid = false;
                        result.ValidationResultMessages.Add($"Invalid Endpoint URL: The URL format is not valid.");
                        return result;
                    }

                    var client = _httpClientFactory.CreateClient();
                    
                    var soapRequest = $@"<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"">
                                            <soapenv:Header/>
                                            <soapenv:Body>
                                                {xmlContent}
                                            </soapenv:Body>
                                         </soapenv:Envelope>";

                    var content = new StringContent(soapRequest, System.Text.Encoding.UTF8, "text/xml");
                    
                    if (!client.DefaultRequestHeaders.Contains("SOAPAction"))
                    {
                        client.DefaultRequestHeaders.Add("SOAPAction", $"\"{operationName}\"");
                    }
                    
                    var response = await client.PostAsync(endpoint, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    
                    result.ResponseContent = responseString;
                    contentToValidate = ExtractSoapBody(responseString);

                    if (!response.IsSuccessStatusCode)
                    {
                        result.ValidationResultMessages.Add($"HTTP Error: {response.StatusCode} - {responseString}");
                        result.IsValid = false;
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    result.IsValid = false;
                    result.ValidationResultMessages.Add($"Error calling endpoint: {ex.Message}");
                    return result;
                }
            }
            else
            {
                // Fallback: Validate the XML itself as if it were the response
                result.ResponseContent = xmlContent;
            }

            // Format for line numbers
            contentToValidate = FormatXml(contentToValidate);
            
            // Format displayed content
            if (!string.IsNullOrEmpty(result.ResponseContent))
            {
                result.ResponseContent = FormatXml(result.ResponseContent);
            }
            
            // Check for SOAP Fault
            var soapFaultError = ParseSoapFault(contentToValidate);
            if (!string.IsNullOrEmpty(soapFaultError))
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add(soapFaultError);
                return result;
            }

            // 1. Find Schema Name
            var schemaName = GetResponseSchemaName(serviceName, version, operationName);
            if (string.IsNullOrEmpty(schemaName))
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add($"Could not find response schema definition for {serviceName} {version} {operationName}");
                return result;
            }

            // 2. Find Schema File
            var schemaPath = FindSchemaFile(schemaName, version, serviceName);
            if (string.IsNullOrEmpty(schemaPath))
            {
                result.IsValid = false;
                result.ValidationResultMessages.Add($"Schema file {schemaName}.xsd not found for version {version}");
                return result;
            }

            // 3. Validate
            var validationResult = ValidateAgainstSchema(contentToValidate, schemaPath);
            result.IsValid = validationResult.IsValid;
            result.ValidationResultMessages = validationResult.ValidationResultMessages;
            
            sw.Stop();
            result.ResponseTimeMs = sw.Elapsed.TotalMilliseconds;
            
            if (!result.IsValid)
            {
               _logger.LogWarning("Response Validation Failed for {Service} {Version} {Operation} Endpoint: {Endpoint}. Errors: {Errors}", serviceName, version, operationName, endpoint, string.Join("; ", result.ValidationResultMessages));
            }
            
            return result;

        }
        catch (Exception ex)
        {
            sw.Stop();
            result.ResponseTimeMs = sw.Elapsed.TotalMilliseconds;
            result.IsValid = false;
            result.ValidationResultMessages.Add($"System Error: {ex.Message}");
            _logger.LogError(ex, "System Error during validation for {Service} {Version} {Operation} Endpoint: {Endpoint}", serviceName, version, operationName, endpoint);
            return result;
        }
    }

    public async Task<string> GetResponseSchema(string serviceName, string version, string operationName)
    {
        var schemaName = GetResponseSchemaName(serviceName, version, operationName);
        if (string.IsNullOrEmpty(schemaName)) return $"<!-- Response schema name not found -->";

        var schemaPath = FindSchemaFile(schemaName, version, serviceName);
        if (string.IsNullOrEmpty(schemaPath)) return $"<!-- Schema file {schemaName}.xsd not found -->";

        var schemaContent = await File.ReadAllTextAsync(schemaPath);
        return FormatXml(schemaContent);
    }

    private string GetResponseSchemaName(string serviceName, string version, string operationName)
    {
        var service = _serviceList?.FirstOrDefault(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
        if (service == null) return null;

        var verParts = version.Split('.');
        if (verParts.Length != 3) return null;
        if (int.TryParse(verParts[0], out int major) && int.TryParse(verParts[1], out int minor) && int.TryParse(verParts[2], out int patch))
        {
            var ver = service.Versions.FirstOrDefault(v => v.Major == major && v.Minor == minor && v.Patch == patch);
            return ver?.Operations.FirstOrDefault(o => o.OperationName.Equals(operationName, StringComparison.OrdinalIgnoreCase))?.ResponseSchema;
        }
        return null;
    }



    private string ParseSoapFault(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            var faultNode = doc.SelectSingleNode("//*[local-name()='Fault']");
            
            if (faultNode != null)
            {
                var faultString = faultNode.SelectSingleNode("//*[local-name()='faultstring']")?.InnerText;
                var detailMessage = faultNode.SelectSingleNode("//*[local-name()='ExceptionDetail']/*[local-name()='Message']")?.InnerText;

                if (!string.IsNullOrWhiteSpace(detailMessage)) return $"SOAP Fault: {detailMessage}";
                if (!string.IsNullOrWhiteSpace(faultString)) return $"SOAP Fault: {faultString}";
                return "SOAP Fault detected but no detail message found.";
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
