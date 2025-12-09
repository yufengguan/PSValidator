using PromoStandards.Validator.Tests.StubServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "PromoStandards Validator Tests Stub Server", 
        Version = "v1",
        Description = "Stub Server implementation of PromoStandards services for testing. Returns various XML responses based on 'code' parameter."
    });
});

// Register services
builder.Services.AddSingleton<IServiceListProvider, ServiceListProvider>();
builder.Services.AddSingleton<IMockResponseProvider, MockResponseProvider>();

// Configure CORS for testing
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseRouting(); // Must be first for endpoint routing to work correctly
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "PromoStandards Stub Server v1");
    c.RoutePrefix = string.Empty; // Swagger at root
});

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
