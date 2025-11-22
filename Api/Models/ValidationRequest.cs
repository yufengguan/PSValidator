namespace PromoStandards.Validator.Api.Models;

public class ValidationRequest
{
    public string Service { get; set; }
    public string Version { get; set; }
    public string Operation { get; set; }
    public string XmlContent { get; set; }
}
