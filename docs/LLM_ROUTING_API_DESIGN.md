# Advanced LLM Provider Integration - API Design

**Document Version:** 1.0.0  
**Status:** Draft  
**Last Updated:** December 2024

---

## ?? Overview

This document defines the public API surface for the Advanced LLM Provider Integration feature, including interfaces, configuration models, and usage examples.

---

## ?? Core Interfaces

### ILLMProvider (Extended)

```csharp
namespace EvoAITest.LLM.Abstractions;

/// <summary>
/// Defines a provider for Large Language Model completions.
/// Extended to support streaming responses.
/// </summary>
public interface ILLMProvider
{
    /// <summary>
    /// Gets the provider name (e.g., "AzureOpenAI", "Ollama", "Routing").
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Gets a value indicating whether this provider supports streaming.
    /// </summary>
    bool SupportsStreaming { get; }
    
    /// <summary>
    /// Completes an LLM request and returns the full response.
    /// </summary>
    /// <param name="request">The LLM request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The complete LLM response.</returns>
    Task<LLMResponse> CompleteAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Completes an LLM request and streams the response token by token.
    /// </summary>
    /// <param name="request">The LLM request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async enumerable of response tokens.</returns>
    IAsyncEnumerable<string> CompleteStreamAsync(
        LLMRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default);
}
```

### IRoutingStrategy

```csharp
namespace EvoAITest.LLM.Routing;

/// <summary>
/// Defines a strategy for routing LLM requests to appropriate providers.
/// </summary>
public interface IRoutingStrategy
{
    /// <summary>
    /// Gets the strategy name.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Selects the appropriate route for the given task type.
    /// </summary>
    /// <param name="taskType">The detected task type.</param>
    /// <param name="options">Routing options.</param>
    /// <returns>Route information including primary and fallback providers.</returns>
    RouteInfo SelectRoute(TaskType taskType, LLMRoutingOptions options);
}
```

### ISecretProvider

```csharp
namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Provides secure access to secrets (API keys, connection strings, etc.).
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Retrieves a secret by name.
    /// </summary>
    /// <param name="secretName">The secret name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The secret value.</returns>
    Task<string> GetSecretAsync(
        string secretName,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves multiple secrets in a single call.
    /// </summary>
    /// <param name="secretNames">Array of secret names.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of secret names to values.</returns>
    Task<Dictionary<string, string>> GetSecretsAsync(
        string[] secretNames,
        CancellationToken cancellationToken = default);
}
```

---

## ?? Data Models

### LLMRequest (Extended)

```csharp
namespace EvoAITest.LLM.Models;

/// <summary>
/// Represents a request to an LLM provider.
/// Extended with metadata for routing.
/// </summary>
public sealed class LLMRequest
{
    /// <summary>
    /// The model to use (e.g., "gpt-4", "qwen2.5-7b").
    /// </summary>
    public required string Model { get; init; }
    
    /// <summary>
    /// The conversation messages.
    /// </summary>
    public required List<Message> Messages { get; init; }
    
    /// <summary>
    /// Temperature for response generation (0.0 - 2.0).
    /// </summary>
    public double Temperature { get; init; } = 0.7;
    
    /// <summary>
    /// Maximum tokens to generate.
    /// </summary>
    public int? MaxTokens { get; init; }
    
    /// <summary>
    /// Stop sequences.
    /// </summary>
    public List<string>? Stop { get; init; }
    
    /// <summary>
    /// NEW: Metadata for routing decisions.
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
    
    /// <summary>
    /// NEW: Explicit task type hint for routing.
    /// </summary>
    public TaskType? TaskType { get; init; }
}
```

### TaskType Enum

```csharp
namespace EvoAITest.LLM.Models;

/// <summary>
/// Defines the type of task for LLM routing.
/// </summary>
public enum TaskType
{
    /// <summary>General purpose task.</summary>
    General = 0,
    
    /// <summary>Planning and strategy tasks.</summary>
    Planning = 1,
    
    /// <summary>Code generation tasks.</summary>
    CodeGeneration = 2,
    
    /// <summary>Analysis and interpretation tasks.</summary>
    Analysis = 3,
    
    /// <summary>Intent detection tasks.</summary>
    IntentDetection = 4,
    
    /// <summary>Validation and verification tasks.</summary>
    Validation = 5,
    
    /// <summary>Summarization tasks.</summary>
    Summarization = 6,
    
    /// <summary>Translation tasks.</summary>
    Translation = 7,
    
    /// <summary>Classification tasks.</summary>
    Classification = 8,
    
    /// <summary>Long-form content generation.</summary>
    LongFormGeneration = 9
}
```

### RouteInfo

```csharp
namespace EvoAITest.LLM.Routing;

/// <summary>
/// Contains routing information for an LLM request.
/// </summary>
public sealed class RouteInfo
{
    /// <summary>
    /// The primary provider name.
    /// </summary>
    public required string PrimaryProvider { get; init; }
    
    /// <summary>
    /// The primary model name.
    /// </summary>
    public required string PrimaryModel { get; init; }
    
    /// <summary>
    /// The fallback provider name (if circuit breaker opens).
    /// </summary>
    public string? FallbackProvider { get; init; }
    
    /// <summary>
    /// The fallback model name.
    /// </summary>
    public string? FallbackModel { get; init; }
    
    /// <summary>
    /// The routing strategy used.
    /// </summary>
    public required string Strategy { get; init; }
    
    /// <summary>
    /// The detected or specified task type.
    /// </summary>
    public required TaskType TaskType { get; init; }
    
    /// <summary>
    /// Estimated cost per 1K tokens (for cost-optimized routing).
    /// </summary>
    public double? EstimatedCostPer1KTokens { get; init; }
}
```

### CircuitBreakerState

```csharp
namespace EvoAITest.LLM.CircuitBreaker;

/// <summary>
/// Represents the state of a circuit breaker.
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>Circuit is closed, requests flow normally.</summary>
    Closed = 0,
    
    /// <summary>Circuit is open, requests go to fallback.</summary>
    Open = 1,
    
    /// <summary>Circuit is half-open, testing recovery.</summary>
    HalfOpen = 2
}

/// <summary>
/// Contains circuit breaker status information.
/// </summary>
public sealed class CircuitBreakerStatus
{
    /// <summary>
    /// The provider name.
    /// </summary>
    public required string ProviderName { get; init; }
    
    /// <summary>
    /// Current circuit state.
    /// </summary>
    public CircuitBreakerState State { get; init; }
    
    /// <summary>
    /// Number of consecutive failures.
    /// </summary>
    public int FailureCount { get; init; }
    
    /// <summary>
    /// Time of last failure.
    /// </summary>
    public DateTimeOffset? LastFailureTime { get; init; }
    
    /// <summary>
    /// Time when circuit will attempt to half-open.
    /// </summary>
    public DateTimeOffset? NextRetryTime { get; init; }
}
```

---

## ?? Configuration Models

### LLMRoutingOptions

```csharp
namespace EvoAITest.Core.Options;

/// <summary>
/// Configuration options for LLM routing.
/// </summary>
public sealed class LLMRoutingOptions
{
    /// <summary>
    /// Gets or sets the routing strategy name.
    /// Valid values: "TaskBased", "CostOptimized", "PerformanceOptimized".
    /// </summary>
    public string RoutingStrategy { get; set; } = "TaskBased";
    
    /// <summary>
    /// Gets or sets a value indicating whether multi-model routing is enabled.
    /// </summary>
    public bool EnableMultiModelRouting { get; set; } = true;
    
    /// <summary>
    /// Gets or sets a value indicating whether provider fallback is enabled.
    /// </summary>
    public bool EnableProviderFallback { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the circuit breaker failure threshold.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    
    /// <summary>
    /// Gets or sets the circuit breaker open duration in seconds.
    /// </summary>
    public int CircuitBreakerOpenDurationSeconds { get; set; } = 30;
    
    /// <summary>
    /// Gets or sets route configurations by task type.
    /// </summary>
    public Dictionary<string, RouteConfiguration> Routes { get; set; } = new();
    
    /// <summary>
    /// Gets or sets the default route for unmapped task types.
    /// </summary>
    public RouteConfiguration DefaultRoute { get; set; } = new()
    {
        PrimaryProvider = "AzureOpenAI",
        PrimaryModel = "gpt-4"
    };
}

/// <summary>
/// Configuration for a specific route.
/// </summary>
public sealed class RouteConfiguration
{
    /// <summary>
    /// Primary provider name.
    /// </summary>
    public required string PrimaryProvider { get; init; }
    
    /// <summary>
    /// Primary model name.
    /// </summary>
    public required string PrimaryModel { get; init; }
    
    /// <summary>
    /// Fallback provider name.
    /// </summary>
    public string? FallbackProvider { get; init; }
    
    /// <summary>
    /// Fallback model name.
    /// </summary>
    public string? FallbackModel { get; init; }
    
    /// <summary>
    /// Maximum allowed latency in milliseconds before fallback.
    /// </summary>
    public int? MaxLatencyMs { get; init; }
    
    /// <summary>
    /// Cost per 1K tokens (for cost optimization).
    /// </summary>
    public double? CostPer1KTokens { get; init; }
}
```

### KeyVaultOptions

```csharp
namespace EvoAITest.Core.Options;

/// <summary>
/// Configuration options for Azure Key Vault.
/// </summary>
public sealed class KeyVaultOptions
{
    /// <summary>
    /// Gets or sets the Key Vault URI.
    /// </summary>
    public string? VaultUri { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether Key Vault is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the secret cache duration in minutes.
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 5;
    
    /// <summary>
    /// Gets or sets a value indicating whether to use managed identity.
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the tenant ID (for service principal authentication).
    /// </summary>
    public string? TenantId { get; set; }
    
    /// <summary>
    /// Gets or sets the client ID (for service principal authentication).
    /// </summary>
    public string? ClientId { get; set; }
}
```

---

## ?? Usage Examples

### Example 1: Basic Usage (Existing Code - No Changes)

```csharp
// Existing code works without modification
public class TestGeneratorService : ITestGenerator
{
    private readonly ILLMProvider _llmProvider;
    
    public async Task<GeneratedTest> GenerateTestAsync(
        RecordingSession session,
        CancellationToken cancellationToken)
    {
        var request = new LLMRequest
        {
            Model = "gpt-4", // Routing handles model selection
            Messages = new List<Message>
            {
                new() { Role = MessageRole.System, Content = "Generate test code..." },
                new() { Role = MessageRole.User, Content = session.ToString() }
            }
        };
        
        // Routing happens automatically
        var response = await _llmProvider.CompleteAsync(request, cancellationToken);
        
        return ParseTest(response.Content);
    }
}
```

### Example 2: Explicit Task Type Routing

```csharp
public class ActionAnalyzerService : IActionAnalyzer
{
    private readonly ILLMProvider _llmProvider;
    
    public async Task<AnalyzedInteraction> AnalyzeInteractionAsync(
        UserInteraction interaction,
        CancellationToken cancellationToken)
    {
        var request = new LLMRequest
        {
            Model = "gpt-4",
            Messages = BuildAnalysisMessages(interaction),
            
            // NEW: Explicit task type for optimal routing
            TaskType = TaskType.Analysis,
            
            // NEW: Additional metadata
            Metadata = new Dictionary<string, object>
            {
                ["InteractionId"] = interaction.Id,
                ["Priority"] = "High"
            }
        };
        
        var response = await _llmProvider.CompleteAsync(request, cancellationToken);
        
        return ParseAnalysis(response.Content);
    }
}
```

### Example 3: Streaming Responses

```csharp
public class TestPreviewService
{
    private readonly ILLMProvider _llmProvider;
    
    public async IAsyncEnumerable<string> GenerateTestStreamAsync(
        RecordingSession session,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var request = new LLMRequest
        {
            Model = "gpt-4",
            Messages = BuildTestGenerationMessages(session),
            TaskType = TaskType.CodeGeneration // Routes to fast model
        };
        
        // Stream tokens as they're generated
        await foreach (var token in _llmProvider.CompleteStreamAsync(request, cancellationToken))
        {
            yield return token;
        }
    }
}
```

### Example 4: Blazor Component with Streaming

```razor
@inject ILLMProvider LLMProvider

<div>
    <button @onclick="GenerateTest">Generate Test</button>
    <pre>@generatedCode</pre>
</div>

@code {
    private string generatedCode = "";
    
    private async Task GenerateTest()
    {
        generatedCode = "";
        
        var request = new LLMRequest
        {
            Model = "gpt-4",
            Messages = BuildMessages(),
            TaskType = TaskType.CodeGeneration
        };
        
        await foreach (var token in LLMProvider.CompleteStreamAsync(request))
        {
            generatedCode += token;
            StateHasChanged(); // Update UI in real-time
        }
    }
}
```

### Example 5: Manual Route Selection

```csharp
public class CustomRoutingService
{
    private readonly ILLMProvider _llmProvider;
    private readonly IRoutingStrategy _strategy;
    
    public async Task<LLMResponse> ExecuteWithCustomRouting(
        LLMRequest request,
        CancellationToken cancellationToken)
    {
        // Manually select route based on custom logic
        var route = _strategy.SelectRoute(request.TaskType ?? TaskType.General, options);
        
        // Log routing decision
        _logger.LogInformation(
            "Using {Provider}/{Model} for {TaskType}",
            route.PrimaryProvider,
            route.PrimaryModel,
            request.TaskType);
        
        return await _llmProvider.CompleteAsync(request, cancellationToken);
    }
}
```

---

## ?? API Endpoints

### Streaming Endpoint (SSE)

```http
POST /api/llm/complete/stream
Content-Type: application/json
Accept: text/event-stream

{
  "model": "gpt-4",
  "messages": [
    {"role": "system", "content": "You are a test generator"},
    {"role": "user", "content": "Generate a login test"}
  ],
  "taskType": "CodeGeneration"
}

Response:
data: using
data:  System
data: ;
data:  using
...
data: [DONE]
```

### Get Circuit Breaker Status

```http
GET /api/llm/circuit-breaker/status

Response: 200 OK
[
  {
    "providerName": "AzureOpenAI",
    "state": "Closed",
    "failureCount": 0,
    "lastFailureTime": null,
    "nextRetryTime": null
  },
  {
    "providerName": "Ollama",
    "state": "Open",
    "failureCount": 5,
    "lastFailureTime": "2024-12-20T10:30:00Z",
    "nextRetryTime": "2024-12-20T10:30:30Z"
  }
]
```

### Get Routing Statistics

```http
GET /api/llm/routing/stats

Response: 200 OK
{
  "totalRequests": 1000,
  "routingDecisions": {
    "Planning": { "provider": "AzureOpenAI", "count": 200 },
    "CodeGeneration": { "provider": "Ollama", "count": 400 },
    "Analysis": { "provider": "AzureOpenAI", "count": 300 },
    "Other": { "provider": "AzureOpenAI", "count": 100 }
  },
  "fallbackUsage": {
    "AzureOpenAI": 5,
    "Ollama": 0
  },
  "averageLatencyMs": {
    "AzureOpenAI": 1200,
    "Ollama": 300
  }
}
```

---

## ?? Configuration Examples

### Minimal Configuration (Defaults)

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Routing"
    }
  }
}
```

### Full Configuration

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Routing",
      "EnableMultiModelRouting": true,
      "RoutingStrategy": "TaskBased",
      "EnableProviderFallback": true,
      "CircuitBreakerFailureThreshold": 5,
      "CircuitBreakerOpenDurationSeconds": 30,
      
      "Routing": {
        "Planning": {
          "PrimaryProvider": "AzureOpenAI",
          "PrimaryModel": "gpt-5",
          "FallbackProvider": "Ollama",
          "FallbackModel": "qwen2.5-7b",
          "MaxLatencyMs": 5000,
          "CostPer1KTokens": 0.03
        },
        "CodeGeneration": {
          "PrimaryProvider": "Ollama",
          "PrimaryModel": "qwen2.5-7b",
          "FallbackProvider": "AzureOpenAI",
          "FallbackModel": "gpt-3.5-turbo",
          "MaxLatencyMs": 2000,
          "CostPer1KTokens": 0.0
        },
        "Analysis": {
          "PrimaryProvider": "AzureOpenAI",
          "PrimaryModel": "gpt-4",
          "FallbackProvider": "Ollama",
          "FallbackModel": "qwen2.5-7b",
          "MaxLatencyMs": 3000,
          "CostPer1KTokens": 0.03
        }
      },
      
      "AzureOpenAI": {
        "Endpoint": "@Microsoft.KeyVault(SecretUri=https://evoai-kv.vault.azure.net/secrets/OpenAIEndpoint)",
        "ApiKey": "@Microsoft.KeyVault(SecretUri=https://evoai-kv.vault.azure.net/secrets/OpenAIApiKey)",
        "DeploymentName": "gpt-4"
      },
      
      "Ollama": {
        "BaseUrl": "http://localhost:11434",
        "Model": "qwen2.5-7b"
      }
    },
    
    "KeyVault": {
      "VaultUri": "https://evoai-kv.vault.azure.net",
      "Enabled": true,
      "CacheDurationMinutes": 5,
      "UseManagedIdentity": true
    }
  }
}
```

---

## ?? Service Registration

### DI Registration Example

```csharp
// In Program.cs or ServiceCollectionExtensions.cs

builder.Services.AddLLMRouting(builder.Configuration, options =>
{
    options.EnableMultiModelRouting = true;
    options.RoutingStrategy = "TaskBased";
    options.EnableProviderFallback = true;
});

// Or with explicit configuration
builder.Services.Configure<LLMRoutingOptions>(
    builder.Configuration.GetSection("EvoAITest:Core:Routing"));

builder.Services.Configure<KeyVaultOptions>(
    builder.Configuration.GetSection("EvoAITest:KeyVault"));

// Register services
builder.Services.AddSingleton<ISecretProvider, KeyVaultSecretProvider>();
builder.Services.AddSingleton<IRoutingStrategy, TaskBasedRoutingStrategy>();
builder.Services.AddScoped<ILLMProvider, RoutingLLMProvider>();
```

---

**Document Status:** ? Complete  
**Next:** Configuration Guide  
**Owner:** API Team
