using EvoAITest.Agents.Extensions;
using EvoAITest.Core.Data;
using EvoAITest.Core.Extensions;
using EvoAITest.Examples;
using EvoAITest.LLM.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// ============================================================================
// EvoAITest Login Automation Example
// ============================================================================
// This example demonstrates how to use the EvoAITest framework to automate
// browser-based login workflows using AI-powered planning and execution.
// ============================================================================

Console.WriteLine(@"
????????????????????????????????????????????????????????????????
?                                                              ?
?         EvoAITest - Login Automation Example                ?
?                                                              ?
?  AI-Powered Browser Automation Framework                    ?
?  .NET 10 + Playwright + Azure OpenAI/Ollama                 ?
?                                                              ?
????????????????????????????????????????????????????????????????
");

// Build host with configuration and services
var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        // Add EvoAITest Core services (Browser, Tools, Repository, DbContext)
        services.AddEvoAITestCore(configuration);

        // Add LLM services (Azure OpenAI / Ollama)
        services.AddLLMServices(configuration);

        // Add Agent services (Planner, Executor, Healer)
        services.AddAgentServices();

        // Add the example service
        services.AddTransient<LoginAutomationExample>();

        // Configure logging
        services.AddLogging(logging =>
        {
            logging.AddConsole();
            logging.AddDebug();
        });
    })
    .Build();

// Apply database migrations
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<EvoAIDbContext>();
    
    Console.WriteLine("?? Applying database migrations...");
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("? Database ready");
    Console.WriteLine();
}

// Run the example
try
{
    using var scope = host.Services.CreateScope();
    var example = scope.ServiceProvider.GetRequiredService<LoginAutomationExample>();

    // Parse command-line arguments
    var targetUrl = args.Length > 0 ? args[0] : "https://example.com";
    var username = args.Length > 1 ? args[1] : null;
    var password = args.Length > 2 ? args[2] : null;

    // Run the automation
    var result = await example.RunLoginExampleAsync(targetUrl, username, password);

    // Exit with appropriate code
    Environment.ExitCode = result.Status == ExecutionStatus.Success ? 0 : 1;
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("? Fatal Error:");
    Console.WriteLine(ex.Message);
    Console.WriteLine();
    Console.WriteLine("Stack Trace:");
    Console.WriteLine(ex.StackTrace);
    Environment.ExitCode = 1;
}
finally
{
    await host.StopAsync();
    host.Dispose();
}
