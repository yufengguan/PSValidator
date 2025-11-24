using PromoStandards.Validator.Api.Models;
using PromoStandards.Validator.Api.Models;

namespace PromoStandards.Validator.Api.Services;

public interface IValidationService
{
    Task<ValidationResult> Validate(string xmlContent, string service, string version, string operation, string endpoint);
    Task<string> GenerateSampleRequest(string serviceName, string version, string operationName);
    Task<string> GetResponseSchema(string serviceName, string version, string operationName);
}
