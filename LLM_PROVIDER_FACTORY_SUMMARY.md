# LLM Provider Factory Implementation - Complete

> **Status**: ? **COMPLETE** - Production-ready factory pattern for dynamic LLM provider selection

## Overview

Successfully implemented a comprehensive `LLMProviderFactory` class that dynamically creates LLM provider instances based on configuration. This enables seamless switching between Azure OpenAI (production), Ollama (development), and custom local LLM providers without code changes.

## Files Created/Updated

### 1. Created: `EvoAITest.LLM/Factory/LLMProviderFactory.cs`
Complete factory implementation with:
- Configuration-based provider selection
- Automatic validation on initialization
- Support for all three provider types
- Comprehensive logging and error handling
- Helper methods for provider info and availability checks

### 2. Updated: `EvoAITest.LLM/Extensions/ServiceCollectionExtensions.cs`
Added factory registration and DI integration:
- `AddLLMServices(IServiceCollection, IConfiguration)` - Main registration method
- Automatic configuration binding
- Factory-based provider creation
- Backward compatibility with legacy method

## Key Features

### 1. Configuration-Based Provider Selection

The factory automatically selects the correct provider based on `appsettings.json`:

**Azure OpenAI (Production):**
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "AzureOpenAIEndpoint": "https://your-resource.openai.azure.com",
      "AzureOpenAIDeployment": "gpt-5"
    }
  }
}
```

**Ollama (Development):**
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Ollama",
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "qwen2.5:32b"
    }
  }
}
```

**Local Custom LLM:**
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Local",
      "LocalLLMEndpoint": "http://localhost:8080/v1/chat/completions"
    }
  }
}
```

### 2. Automatic Configuration Validation

The factory validates configuration on initialization:

```csharp
try
{
    _options.ValidateConfiguration();
}
catch (InvalidOperationException ex)
{
    _logger.LogError(ex, "LLM provider configuration is invalid");
    throw;
}
```

**Validation checks:**
- Required settings for selected provider
- Endpoint format validation
- API key presence (Azure OpenAI)
- Model availability (Ollama)

### 3. Dual Authentication Support (Azure OpenAI)

Automatically selects the best authentication method:

**Managed Identity (Preferred for Production):**
```csharp
if (string.IsNullOrWhiteSpace(_options.AzureOpenAIApiKey))
{
    // Use DefaultAzureCredential
    return new AzureOpenAIProvider(
        _options.AzureOpenAIEndpoint,
        _options.AzureOpenAIDeployment,
        logger);
}
```

**API Key (Development/Testing):**
```csharp
return new AzureOpenAIProvider(
    _options.AzureOpenAIEndpoint,
    _options.AzureOpenAIApiKey,
    _options.AzureOpenAIDeployment,
    logger);
```

### 4. Comprehensive Logging

All provider creation operations are logged:

```csharp
_logger.LogInformation("Creating LLM provider: {Provider}", _options.LLMProvider);
_logger.LogInformation(
    "Creating Azure OpenAI provider with Managed Identity authentication. Endpoint: {Endpoint}, Deployment: {Deployment}",
    _options.AzureOpenAIEndpoint,
    _options.AzureOpenAIDeployment);
```

### 5. Helper Methods

**Get Provider Info:**
```csharp
var factory = serviceProvider.GetRequiredService<LLMProviderFactory>();
var info = factory.GetProviderInfo();

foreach (var (key, value) in info)
{
    Console.WriteLine($"{key}: {value}");
}

// Output for Azure OpenAI:
// Provider: Azure OpenAI
// Endpoint: https://your-resource.openai.azure.com
// Deployment: gpt-5
// Model: gpt-5
// ApiVersion: 2025-01-01-preview
// Authentication: Managed Identity
```

**Check Availability:**
```csharp
var factory = serviceProvider.GetRequiredService<LLMProviderFactory>();
bool isAvailable = await factory.IsProviderAvailableAsync();

if (!isAvailable)
{
    Console.WriteLine("Warning: LLM provider is not available!");
}
```

## Usage Examples

### Example 1: Basic DI Registration (Program.cs)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register LLM services with configuration
builder.Services.AddLLMServices(builder.Configuration);

var app = builder.Build();
```

### Example 2: Using ILLMProvider in Services

```csharp
public class AutomationService
{
    private readonly ILLMProvider _llm;
    private readonly ILogger<AutomationService> _logger;
    
    public AutomationService(ILLMProvider llm, ILogger<AutomationService> logger)
    {
        _llm = llm;
        _logger = logger;
    }
    
    public async Task<string> GeneratePlanAsync(string userGoal)
    {
        _logger.LogInformation("Generating plan with {Provider}", _llm.Name);
        
        var response = await _llm.GenerateAsync(
            prompt: "Create an automation plan for: {goal}",
            variables: new Dictionary<string, string> { ["goal"] = userGoal },
            maxTokens: 1000
        );
        
        return response;
    }
}
```

### Example 3: Checking Provider at Startup

```csharp
var app = builder.Build();

// Verify LLM provider is available
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<LLMProviderFactory>();
    var providerName = factory.GetConfiguredProviderName();
    
    Console.WriteLine($"Configured LLM Provider: {providerName}");
    
    bool isAvailable = await factory.IsProviderAvailableAsync();
    if (!isAvailable)
    {
        Console.WriteLine("WARNING: LLM provider is not available!");
        Console.WriteLine("Provider Info:");
        foreach (var (key, value) in factory.GetProviderInfo())
        {
            Console.WriteLine($"  {key}: {value}");
        }
    }
}

app.Run();
```

### Example 4: Blazor Component with ILLMProvider

```razor
@page "/automation"
@inject ILLMProvider LLM
@inject ILogger<Automation> Logger

<h3>AI-Powered Browser Automation</h3>

<div class="provider-info">
    <strong>Active Provider:</strong> @LLM.Name
</div>

<div class="input-section">
    <label>What would you like to automate?</label>
    <textarea @bind="userGoal" rows="4"></textarea>
    <button @onclick="GeneratePlan">Generate Plan</button>
</div>

@if (!string.IsNullOrEmpty(plan))
{
    <div class="plan-output">
        <h4>Generated Plan:</h4>
        <pre>@plan</pre>
    </div>
}

@code {
    private string userGoal = "";
    private string plan = "";
    
    private async Task GeneratePlan()
    {
        if (string.IsNullOrWhiteSpace(userGoal))
            return;
        
        Logger.LogInformation("Generating plan with {Provider}", LLM.Name);
        
        try
        {
            plan = await LLM.GenerateAsync(
                prompt: "Create a detailed browser automation plan for: {goal}",
                variables: new Dictionary<string, string> { ["goal"] = userGoal },
                maxTokens: 1500
            );
            
            var usage = LLM.GetLastTokenUsage();
            Logger.LogInformation(
                "Plan generated. Tokens: {Tokens}, Cost: ${Cost:F4}",
                usage.TotalTokens,
                usage.EstimatedCostUSD);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to generate plan");
            plan = $"Error: {ex.Message}";
        }
    }
}
```

### Example 5: Switching Providers via Environment

**Development (appsettings.Development.json):**
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Ollama",
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "qwen2.5:32b",
      "HeadlessMode": false,
      "LogLevel": "Debug"
    }
  }
}
```

**Production (appsettings.json + Azure Config):**
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "AzureOpenAIDeployment": "gpt-5",
      "AzureOpenAIApiVersion": "2025-01-01-preview",
      "HeadlessMode": true,
      "LogLevel": "Information"
    }
  }
}
```

Environment variables:
```bash
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
# API key from Azure Key Vault or Managed Identity
```

**No code changes required!**

### Example 6: Health Check Endpoint

```csharp
[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly LLMProviderFactory _factory;
    
    public HealthController(LLMProviderFactory factory)
    {
        _factory = factory;
    }
    
    [HttpGet("llm")]
    public async Task<IActionResult> CheckLLMHealth()
    {
        var providerName = _factory.GetConfiguredProviderName();
        var providerInfo = _factory.GetProviderInfo();
        var isAvailable = await _factory.IsProviderAvailableAsync();
        
        return Ok(new
        {
            provider = providerName,
            available = isAvailable,
            info = providerInfo,
            timestamp = DateTimeOffset.UtcNow
        });
    }
}
```

## Factory Methods

### CreateProvider()
```csharp
public ILLMProvider CreateProvider()
```

Creates an LLM provider instance based on configuration.

**Returns:** `ILLMProvider` (AzureOpenAIProvider, OllamaProvider, or custom)

**Throws:**
- `InvalidOperationException` - Unknown provider or creation failure

**Example:**
```csharp
var factory = serviceProvider.GetRequiredService<LLMProviderFactory>();
var provider = factory.CreateProvider();
Console.WriteLine($"Provider: {provider.Name}");
```

### GetConfiguredProviderName()
```csharp
public string GetConfiguredProviderName()
```

Gets the name of the configured provider without creating an instance.

**Returns:** `"AzureOpenAI"`, `"Ollama"`, or `"Local"`

**Example:**
```csharp
var providerName = factory.GetConfiguredProviderName();
Console.WriteLine($"Using: {providerName}");
```

### IsProviderAvailableAsync()
```csharp
public async Task<bool> IsProviderAvailableAsync(CancellationToken cancellationToken = default)
```

Checks if the configured provider is available and ready.

**Returns:** `true` if available; `false` otherwise

**Example:**
```csharp
if (!await factory.IsProviderAvailableAsync())
{
    throw new InvalidOperationException("LLM provider is not available");
}
```

### GetProviderInfo()
```csharp
public Dictionary<string, string> GetProviderInfo()
```

Gets detailed information about the configured provider.

**Returns:** Dictionary with provider-specific details

**Example:**
```csharp
var info = factory.GetProviderInfo();
Console.WriteLine($"Provider: {info["Provider"]}");
Console.WriteLine($"Endpoint: {info["Endpoint"]}");
Console.WriteLine($"Model: {info["Model"]}");
```

## Error Handling

### Configuration Validation Errors

```csharp
// Invalid provider
{
  "LLMProvider": "Unknown"
}
// Throws: InvalidOperationException: Unknown LLM provider

// Missing Azure OpenAI endpoint
{
  "LLMProvider": "AzureOpenAI",
  "AzureOpenAIEndpoint": ""
}
// Throws: InvalidOperationException: Azure OpenAI endpoint is required

// Missing Ollama model
{
  "LLMProvider": "Ollama",
  "OllamaModel": ""
}
// Throws: InvalidOperationException: Ollama model is required
```

### Runtime Errors

```csharp
try
{
    var provider = factory.CreateProvider();
    var response = await provider.GenerateAsync("Test");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("connect"))
{
    // Connection error (Ollama not running, Azure endpoint unreachable)
    logger.LogError(ex, "Cannot connect to LLM provider");
}
catch (InvalidOperationException ex) when (ex.Message.Contains("authentication"))
{
    // Authentication error (invalid API key, missing permissions)
    logger.LogError(ex, "LLM provider authentication failed");
}
catch (Exception ex)
{
    // Other errors
    logger.LogError(ex, "LLM provider error");
}
```

## Testing

### Unit Tests

```csharp
public class LLMProviderFactoryTests
{
    [Fact]
    public void CreateProvider_WithAzureOpenAI_ShouldCreateAzureProvider()
    {
        // Arrange
        var options = Options.Create(new EvoAITestCoreOptions
        {
            LLMProvider = "AzureOpenAI",
            AzureOpenAIEndpoint = "https://test.openai.azure.com",
            AzureOpenAIApiKey = "test-key",
            AzureOpenAIDeployment = "gpt-4"
        });
        
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        
        var factory = new LLMProviderFactory(options, loggerFactory.Object);
        
        // Act
        var provider = factory.CreateProvider();
        
        // Assert
        provider.Should().NotBeNull();
        provider.Name.Should().Be("Azure OpenAI");
        provider.GetModelName().Should().Be("gpt-4");
    }
    
    [Fact]
    public void CreateProvider_WithOllama_ShouldCreateOllamaProvider()
    {
        // Arrange
        var options = Options.Create(new EvoAITestCoreOptions
        {
            LLMProvider = "Ollama",
            OllamaEndpoint = "http://localhost:11434",
            OllamaModel = "qwen2.5:32b"
        });
        
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        
        var factory = new LLMProviderFactory(options, loggerFactory.Object);
        
        // Act
        var provider = factory.CreateProvider();
        
        // Assert
        provider.Should().NotBeNull();
        provider.Name.Should().Be("Ollama");
        provider.GetModelName().Should().Be("qwen2.5:32b");
    }
    
    [Fact]
    public void CreateProvider_WithInvalidProvider_ShouldThrow()
    {
        // Arrange
        var options = Options.Create(new EvoAITestCoreOptions
        {
            LLMProvider = "InvalidProvider"
        });
        
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            new LLMProviderFactory(options, loggerFactory.Object));
    }
    
    [Fact]
    public void GetProviderInfo_WithAzureOpenAI_ShouldReturnCorrectInfo()
    {
        // Arrange
        var options = Options.Create(new EvoAITestCoreOptions
        {
            LLMProvider = "AzureOpenAI",
            AzureOpenAIEndpoint = "https://test.openai.azure.com",
            AzureOpenAIApiKey = "test-key",
            AzureOpenAIDeployment = "gpt-5",
            LLMModel = "gpt-5"
        });
        
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        
        var factory = new LLMProviderFactory(options, loggerFactory.Object);
        
        // Act
        var info = factory.GetProviderInfo();
        
        // Assert
        info["Provider"].Should().Be("Azure OpenAI");
        info["Endpoint"].Should().Be("https://test.openai.azure.com");
        info["Deployment"].Should().Be("gpt-5");
        info["Model"].Should().Be("gpt-5");
        info["Authentication"].Should().Be("API Key");
    }
}
```

### Integration Tests

```csharp
[Collection("LLM Integration")]
public class LLMProviderFactoryIntegrationTests
{
    [Fact]
    public async Task EndToEnd_CreateProviderAndGenerate()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Development.json")
            .Build();
        
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLLMServices(configuration);
        
        var serviceProvider = services.BuildServiceProvider();
        var provider = serviceProvider.GetRequiredService<ILLMProvider>();
        
        // Act
        var isAvailable = await provider.IsAvailableAsync();
        
        // Assert
        isAvailable.Should().BeTrue();
        
        if (isAvailable)
        {
            var response = await provider.GenerateAsync("Say 'Hello'", maxTokens: 50);
            response.Should().NotBeNullOrEmpty();
            response.Should().Contain("Hello");
        }
    }
}
```

## Production Deployment Checklist

### ? Configuration
- [ ] Set `LLMProvider` to `"AzureOpenAI"` for production
- [ ] Configure `AZURE_OPENAI_ENDPOINT` environment variable
- [ ] Store API key in Azure Key Vault (secret: `LLMAPIKEY`)
- [ ] Or use Managed Identity (preferred)
- [ ] Set appropriate `AzureOpenAIDeployment` name
- [ ] Configure `LogLevel` to `"Information"` or `"Warning"`

### ? Security
- [ ] Never commit API keys to source control
- [ ] Use Managed Identity in production
- [ ] Enable Azure Key Vault integration
- [ ] Configure RBAC for Azure OpenAI resource
- [ ] Review logging to ensure no PII/sensitive data

### ? Monitoring
- [ ] Add Application Insights
- [ ] Monitor provider availability
- [ ] Track token usage and costs
- [ ] Set up alerts for failures
- [ ] Log provider selection at startup

### ? Testing
- [ ] Test with Ollama locally
- [ ] Verify Azure OpenAI connection
- [ ] Test failover scenarios
- [ ] Load test with production model
- [ ] Verify authentication methods

## Next Steps

1. ? **LLM Provider Factory** - Complete
2. ? **DI Registration** - Complete
3. ? **Implement Agents** - Planner, Executor, Healer
4. ? **Add Caching** - Response caching for efficiency
5. ? **Create Integration Tests** - Real provider tests
6. ? **Add Health Checks** - ASP.NET Core health checks
7. ? **Implement Retry Logic** - Transient error handling

## Build Status

- **Build**: ? Successful
- **Factory**: ? Complete
- **DI Registration**: ? Complete
- **Configuration Binding**: ? Complete
- **Documentation**: ? Complete

## Summary

The LLM Provider Factory is now production-ready and provides:

? **Configuration-based provider selection** - No code changes to switch providers
? **Automatic validation** - Fail fast with clear error messages
? **Dual authentication** - API key or Managed Identity
? **Comprehensive logging** - Full operational visibility
? **Helper methods** - Provider info and availability checks
? **DI integration** - Seamless ASP.NET Core integration
? **Error handling** - Graceful degradation
? **Testing support** - Easy to mock and test

Ready for production deployment with Azure OpenAI and development with Ollama! ??
