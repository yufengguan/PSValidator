namespace PromoStandards.Validator.Tests.StubServer.Models;

public class MockResponseConfig
{
    public string Service { get; set; }
    public List<MockResponseItem> MockResponses { get; set; }
}

public class MockResponseItem
{
    public string ErrorCode { get; set; }
    public string StubResponseFile { get; set; }
    public string ExpectedError { get; set; }
}
