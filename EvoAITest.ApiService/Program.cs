using EvoAITest.Core.Extensions;
using EvoAITest.Core.Data;
using EvoAITest.ApiService.Endpoints;
using EvoAITest.Agents.Extensions;
using EvoAITest.LLM.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add EvoAITest.Core services (Browser Agent, Tool Registry, Tool Executor, DbContext, Repositories)
builder.Services.AddEvoAITestCore(builder.Configuration);

// Add LLM services (required for AI-powered recording analysis)
builder.Services.AddLLMServices(builder.Configuration);

// Add Agent services (includes ActionAnalyzer, TestGenerator, RecordingAgent)
builder.Services.AddAgentServices();

// Add services to the container.
builder.Services.AddProblemDetails();

// Add controllers for Visual Regression API
builder.Services.AddControllers();

// Add authentication and authorization for API endpoints.
// In production, configure a proper authentication scheme (e.g., JWT Bearer).
// For development, the API endpoints fall back to "anonymous-user" if no authenticated user is present.
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply database migrations in development environment
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<EvoAIDbContext>();
    await dbContext.Database.MigrateAsync();
    app.Logger.LogInformation("Database migrations applied successfully");
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// Enable authentication and authorization middleware.
// Note: In development, requests without authentication will fall back to "anonymous-user".
// In production, configure proper authentication (e.g., JWT Bearer) to secure endpoints.
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Map controllers (includes VisualRegressionController)
app.MapControllers();

// Map API endpoints
app.MapTaskEndpoints();
app.MapRecordingEndpoints();

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapDefaultEndpoints();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

namespace EvoAITest.ApiService
{
    /// <summary>
    /// Partial class declaration to enable WebApplicationFactory-based testing.
    /// </summary>
    public partial class Program { }
}
