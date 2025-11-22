using PromoStandards.Validator.Api.Models;

namespace PromoStandards.Validator.Api.Services;

public interface IValidationService
{
    ValidationResult Validate(string xmlContent, string service, string version, string operation);
}
