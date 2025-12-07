using System.Text.Json;

namespace PromoStandards.Validator.Tests.StubServer.Services;

public interface IServiceListProvider
{
    Task<JsonElement> GetServiceListAsync();
    Task<bool> IsValidOperationAsync(string serviceName, string operationName);
}

public class ServiceListProvider : IServiceListProvider
{
    private readonly IWebHostEnvironment _env;
    private JsonElement _cachedServiceList;
    private bool _isLoaded = false;

    public ServiceListProvider(IWebHostEnvironment env)
    {
        _env = env;
    }

    public async Task<JsonElement> GetServiceListAsync()
    {
        if (!_isLoaded)
        {
            var path = Path.Combine(_env.ContentRootPath, "PSServiceList.json");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("PSServiceList.json not found", path);
            }

            var json = await File.ReadAllTextAsync(path);
            _cachedServiceList = JsonSerializer.Deserialize<JsonElement>(json);
            _isLoaded = true;
        }

        return _cachedServiceList;
    }

    public async Task<bool> IsValidOperationAsync(string serviceName, string operationName)
    {
        var services = await GetServiceListAsync();
        
        foreach (var service in services.EnumerateArray())
        {
            if (service.GetProperty("ServiceName").GetString()?.Equals(serviceName, StringComparison.OrdinalIgnoreCase) == true)
            {
                var versions = service.GetProperty("Versions");
                foreach (var version in versions.EnumerateArray())
                {
                    var operations = version.GetProperty("Operations");
                    foreach (var op in operations.EnumerateArray())
                    {
                        if (op.GetProperty("OperationName").GetString()?.Equals(operationName, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }
}
