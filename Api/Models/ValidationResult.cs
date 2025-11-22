namespace PromoStandards.Validator.Api.Models;

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> ValidationResultMessages { get; set; } = new List<string>();
}
