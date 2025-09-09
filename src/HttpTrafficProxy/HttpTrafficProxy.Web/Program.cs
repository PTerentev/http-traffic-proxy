using HttpTrafficProxy.Application.DependencyInjection;
using HttpTrafficProxy.Services.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.ConfigureServices(builder.Configuration);
builder.Services.ConfigureApplication(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "HttpTrafficProxy v1");
});

app.UseHttpsRedirection();
app.MapControllers();

app.MapGet("/healthz", () => Results.Ok("OK"));

if (app.Configuration.GetSection("Application").GetValue<bool>("UseAdvancedMode"))
{
    app.Logger.LogInformation("Используется Advanced режим.");
}
else
{
    app.Logger.LogInformation("Используется Primitive режим.");
}

app.Run();
