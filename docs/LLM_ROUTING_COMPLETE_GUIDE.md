# EvoAITest.LLM v2.0 - Complete Feature Documentation

## ?? Table of Contents
- [Installation & Configuration](#installation--configuration)
- [Intelligent Routing](#intelligent-routing)
- [Circuit Breaker & Resilience](#circuit-breaker--resilience)
- [Azure Key Vault Integration](#azure-key-vault-integration)
- [Usage Examples](#usage-examples)
- [Performance & Cost Optimization](#performance--cost-optimization)
- [Monitoring & Observability](#monitoring--observability)
- [Migration Guide](#migration-guide)

---

## Installation & Configuration

### 1. Add Package Reference

```bash
dotnet add reference ../EvoAITest.LLM/EvoAITest.LLM.csproj
```

### 2. Register Services

```csharp
// In Program.cs or Startup.cs
builder.Services.AddLLMServices(builder.Configuration);
```

### 3. Configuration (appsettings.json)

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "AzureOpenAIEndpoint": "https://your-resource.openai.azure.com/",
      "AzureOpenAIDeployment": "gpt-4",
      "EnableMultiModelRouting": true,
      "EnableProviderFallback": true,
      "RoutingStrategy": "TaskBased",
      "CircuitBreakerFailureThreshold": 5,
      "CircuitBreakerOpenDurationSeconds": 30,
      
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "qwen2.5-coder:7b"
    }
  },
  "LLMRouting": {
    "RoutingStrategy": "TaskBased",
    "EnableMultiModelRouting": true,
    "EnableProviderFallback": true,
    "DefaultRoute": {
      "PrimaryProvider": "AzureOpenAI",
      "PrimaryModel": "gpt-4",
      "FallbackProvider": "Ollama",
      "FallbackModel": "qwen2.5-coder:7b",
      "MaxLatencyMs": 5000,
      "CostPer1KTokens": 0.03
    },
    "Routes": {
      "CodeGeneration": {
        "PrimaryProvider": "Ollama",
        "PrimaryModel": "qwen2.5-coder:7b",
        "FallbackProvider": "AzureOpenAI",
        "FallbackModel": "gpt-4",
        "MaxLatencyMs": 3000,
        "CostPer1KTokens": 0.0
      },
      "Planning": {
        "PrimaryProvider": "AzureOpenAI",
        "PrimaryModel": "gpt-4",
        "MaxLatencyMs": 10000,
        "CostPer1KTokens": 0.03
      }
    }
  },
  "KeyVault": {
    "VaultUri": "https://your-keyvault.vault.azure.net/",
    "Enabled": true,
    "EnableCaching": true,
    "CacheDuration": "01:00:00"
  }
}
```

---

## Intelligent Routing

### Overview

The routing system automatically directs LLM requests to the most appropriate model based on:
- **Task Type**: Code generation, planning, analysis, etc.
- **Cost**: Optimize for lowest cost while maintaining quality
- **Latency**: Meet response time requirements
- **Availability**: Route around unhealthy providers

### Task-Based Routing

Automatically detects task type from prompt content:

```csharp
// This request is automatically routed to a code-specialized model
var request = new LLMRequest
{
    Messages = new List<Message>
    {
        new() { Role = MessageRole.User, Content = "Write a C# function to sort an array" }
    }
};

var response = await llmProvider.CompleteAsync(request);
// Routed to: Ollama (qwen2.5-coder:7b) - specialized for code
```

**Supported Task Types:**
- `CodeGeneration` - Programming, scripts, test code
- `Planning` - Strategy, workflows, step-by-step plans
- `Analysis` - Code review, pattern recognition
- `IntentDetection` - Understanding user goals
- `Validation` - Checking correctness
- `Summarization` - Condensing content
- `General` - Default for other tasks

### Cost-Optimized Routing

Minimizes costs while maintaining quality:

```csharp
// Configuration for cost optimization
{
  "LLMRouting": {
    "RoutingStrategy": "CostOptimized",
    "Routes": {
      "General": {
        "PrimaryProvider": "Ollama",
        "PrimaryModel": "llama3:8b",
        "CostPer1KTokens": 0.0  // Free local model
      },
      "Planning": {
        "PrimaryProvider": "AzureOpenAI",
        "PrimaryModel": "gpt-4",
        "CostPer1KTokens": 0.03,  // Use premium model when needed
        "FallbackProvider": "Ollama",
        "FallbackModel": "llama3:8b"
      }
    }
  }
}
```

### Custom Routing Strategy

Implement your own routing logic:

```csharp
public class CustomRoutingStrategy : IRoutingStrategy
{
    public string Name => "Custom";
    public int Priority => 100;

    public Task<RouteInfo?> SelectRouteAsync(
        LLMRequest request,
        LLMRoutingOptions options,
        CancellationToken cancellationToken = default)
    {
        // Your custom routing logic
        var taskType = AnalyzeRequest(request);
        var route = options.Routes.GetValueOrDefault(taskType.ToString());
        
        return Task.FromResult(new RouteInfo
        {
            PrimaryProvider = route.PrimaryProvider,
            PrimaryModel = route.PrimaryModel,
            TaskType = taskType,
            Strategy = Name
        });
    }
}

// Register
builder.Services.AddSingleton<IRoutingStrategy, CustomRoutingStrategy>();
```

---

## Circuit Breaker & Resilience

### Overview

Circuit breaker pattern prevents cascading failures by:
1. **Monitoring** provider health
2. **Opening** circuit when failures exceed threshold
3. **Routing** to fallback provider while circuit is open
4. **Testing** recovery periodically
5. **Closing** circuit when provider recovers

### States

```
???????????  Failures    ????????  Timeout   ????????????
? CLOSED  ???????????????? OPEN ??????????????HALF-OPEN ?
?(Normal) ?              ?(Failed)?           ?(Testing) ?
???????????              ????????            ????????????
     ?                       ?                     ?
     ?                       ?                     ?
     ?      Success          ?    Success          ?
     ???????????????????????????????????????????????
```

### Configuration

```json
{
  "CircuitBreaker": {
    "FailureThreshold": 5,
    "OpenDuration": "00:00:30",
    "SuccessThresholdInHalfOpen": 2,
    "EmitTelemetryEvents": true
  }
}
```

### Usage Example

```csharp
// Circuit breaker is transparent - no code changes needed!
var response = await llmProvider.CompleteAsync(request);

// Behind the scenes:
// 1. If primary provider healthy ? use primary
// 2. If primary failing ? circuit opens ? use fallback
// 3. After timeout ? test primary ? close circuit if recovered
```

### Monitoring Circuit State

```csharp
// If using CircuitBreakerLLMProvider directly
var circuitProvider = serviceProvider.GetRequiredService<CircuitBreakerLLMProvider>();
var status = circuitProvider.GetStatus();

Console.WriteLine($"State: {status.State}");
Console.WriteLine($"Failures: {status.FailureCount}");
Console.WriteLine($"Total Requests: {status.TotalRequests}");
Console.WriteLine($"Fallback Usage: {status.FallbackRate:P}");
```

---

## Azure Key Vault Integration

### Overview

Securely manage API keys and secrets using Azure Key Vault:
- **No secrets in code** or configuration files
- **Managed Identity** support for Azure environments
- **Automatic caching** for performance
- **Secret rotation** without application restart

### Setup

See [KEY_VAULT_SETUP.md](../docs/KEY_VAULT_SETUP.md) for complete setup guide.

**Quick Start:**

1. **Create Key Vault**
```bash
az keyvault create \
  --name your-keyvault \
  --resource-group your-rg \
  --location eastus
```

2. **Add Secrets**
```bash
az keyvault secret set \
  --vault-name your-keyvault \
  --name "AzureOpenAI-ApiKey" \
  --value "your-api-key"
```

3. **Grant Access**
```bash
# For local development (Azure CLI)
az login

# For production (Managed Identity)
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee <managed-identity-principal-id> \
  --scope <keyvault-resource-id>
```

4. **Configure Application**
```json
{
  "KeyVault": {
    "VaultUri": "https://your-keyvault.vault.azure.net/",
    "Enabled": true,
    "EnableCaching": true,
    "CacheDuration": "01:00:00"
  }
}
```

### Usage in Code

```csharp
// Inject ISecretProvider
public class MyService
{
    private readonly ISecretProvider _secretProvider;
    
    public MyService(ISecretProvider secretProvider)
    {
        _secretProvider = secretProvider;
    }
    
    public async Task InitializeAsync()
    {
        // Retrieve secret from Key Vault
        var apiKey = await _secretProvider.GetSecretAsync("AzureOpenAI-ApiKey");
        
        // Or batch retrieval
        var secrets = await _secretProvider.GetSecretsAsync(new[]
        {
            "AzureOpenAI-ApiKey",
            "Database-ConnectionString"
        });
    }
}
```

### Development Without Key Vault

For local development without Azure dependencies:

```json
{
  "KeyVault": {
    "Enabled": false
  }
}
```

Use **User Secrets** instead:
```bash
dotnet user-secrets set "EvoAITest:Core:AzureOpenAIApiKey" "your-key"
```

---

## Usage Examples

### Basic Completion

```csharp
var llmProvider = serviceProvider.GetRequiredService<ILLMProvider>();

var request = new LLMRequest
{
    Model = "gpt-4",
    Messages = new List<Message>
    {
        new() { Role = MessageRole.System, Content = "You are helpful" },
        new() { Role = MessageRole.User, Content = "Explain browser automation" }
    }
};

var response = await llmProvider.CompleteAsync(request);
Console.WriteLine(response.Content);
```

### Streaming with SignalR

```csharp
// Server-side (LLMStreamingHub)
public async Task StreamCompletion(LLMRequest request)
{
    await foreach (var chunk in _llmProvider.StreamCompleteAsync(request))
    {
        await Clients.Caller.SendAsync("ReceiveChunk", chunk);
    }
}

// Client-side (Blazor)
@code {
    private HubConnection? _hubConnection;
    
    protected override async Task OnInitializedAsync()
    {
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(NavigationManager.ToAbsoluteUri("/llmhub"))
            .Build();
            
        _hubConnection.On<LLMStreamChunk>("ReceiveChunk", chunk =>
        {
            responseText += chunk.Delta;
            StateHasChanged();
        });
        
        await _hubConnection.StartAsync();
    }
    
    private async Task SendRequest()
    {
        await _hubConnection!.InvokeAsync("StreamCompletion", request);
    }
}
```

### Multi-Provider with Automatic Failover

```csharp
// No special code needed - routing and failover are automatic!

// Request 1: Code generation ? Routed to Ollama (fast, local)
var codeRequest = new LLMRequest
{
    Messages = new List<Message>
    {
        new() { Role = MessageRole.User, Content = "Write a sorting algorithm in C#" }
    }
};
var codeResponse = await llmProvider.CompleteAsync(codeRequest);

// Request 2: Strategic planning ? Routed to Azure OpenAI (high quality)
var planRequest = new LLMRequest
{
    Messages = new List<Message>
    {
        new() { Role = MessageRole.User, Content = "Create a test automation strategy" }
    }
};
var planResponse = await llmProvider.CompleteAsync(planRequest);

// If primary provider fails, circuit breaker automatically uses fallback
```

### Tool Calling / Function Calling

```csharp
var tools = new List<BrowserTool>
{
    new()
    {
        Name = "click",
        Description = "Click an element",
        Parameters = new { selector = "string" }
    },
    new()
    {
        Name = "type",
        Description = "Type text into an input",
        Parameters = new { selector = "string", text = "string" }
    }
};

var request = new LLMRequest
{
    Messages = messages,
    Tools = tools
};

var response = await llmProvider.CompleteAsync(request);
var toolCalls = await llmProvider.ParseToolCallsAsync(response.Content);

foreach (var call in toolCalls)
{
    Console.WriteLine($"Tool: {call.ToolName}");
    Console.WriteLine($"Parameters: {call.Parameters}");
}
```

---

## Performance & Cost Optimization

### Cost Tracking

```csharp
var response = await llmProvider.CompleteAsync(request);

// Get token usage
var usage = llmProvider.GetLastTokenUsage();
Console.WriteLine($"Input tokens: {usage.InputTokens}");
Console.WriteLine($"Output tokens: {usage.OutputTokens}");
Console.WriteLine($"Estimated cost: ${usage.EstimatedCostUSD:F4}");
```

### Latency Optimization

Configure timeouts and latency requirements:

```json
{
  "LLMRouting": {
    "Routes": {
      "RealTime": {
        "PrimaryProvider": "Ollama",
        "MaxLatencyMs": 1000,  // Require fast response
        "FallbackProvider": "AzureOpenAI"
      }
    }
  }
}
```

### Caching Strategies

**Key Vault Caching:**
```json
{
  "KeyVault": {
    "EnableCaching": true,
    "CacheDuration": "01:00:00"  // Cache secrets for 1 hour
  }
}
```

**Response Caching** (in your application):
```csharp
public class CachedLLMProvider : ILLMProvider
{
    private readonly ILLMProvider _inner;
    private readonly IMemoryCache _cache;
    
    public async Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken ct)
    {
        var key = ComputeKey(request);
        
        if (_cache.TryGetValue<LLMResponse>(key, out var cached))
            return cached;
            
        var response = await _inner.CompleteAsync(request, ct);
        _cache.Set(key, response, TimeSpan.FromMinutes(10));
        return response;
    }
}
```

---

## Monitoring & Observability

### OpenTelemetry Integration

Automatic metrics and tracing:

```csharp
// Metrics emitted:
// - llm.request.duration
// - llm.request.tokens
// - llm.request.cost
// - circuit_breaker.state
// - circuit_breaker.failures

// Traces include:
// - LLM provider selection
// - Routing decisions
// - Circuit breaker state changes
// - Token usage
```

### Structured Logging

```csharp
// Logs automatically include:
// - Routing strategy used
// - Task type detected
// - Provider selected
// - Circuit breaker state
// - Token usage
// - Error details

// Example log output:
// [Information] LLM request routed: TaskType=CodeGeneration, Provider=Ollama, Model=qwen2.5-coder:7b, Strategy=TaskBased
// [Warning] Circuit breaker opened for AzureOpenAI after 5 failures, using fallback Ollama
// [Information] LLM request completed: Duration=1.2s, InputTokens=150, OutputTokens=500, Cost=$0.0195
```

### Health Checks

```csharp
// Built-in health checks
builder.Services.AddHealthChecks()
    .AddCheck<LLMProviderHealthCheck>("llm_provider")
    .AddCheck<KeyVaultHealthCheck>("key_vault");

app.MapHealthChecks("/health");
```

---

## Migration Guide

### From Direct Provider to Routing

**Before (v1.x):**
```csharp
// Direct provider usage
var azureProvider = new AzureOpenAIProvider(options, logger);
var response = await azureProvider.CompleteAsync(request);
```

**After (v2.0):**
```csharp
// Automatic routing and failover
var llmProvider = serviceProvider.GetRequiredService<ILLMProvider>();
var response = await llmProvider.CompleteAsync(request);
```

### Configuration Migration

**Before:**
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "AzureOpenAIApiKey": "sk-...",  // ? Secret in config
      "AzureOpenAIEndpoint": "https://...",
      "AzureOpenAIDeployment": "gpt-4"
    }
  }
}
```

**After:**
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "AzureOpenAIEndpoint": "https://...",
      "AzureOpenAIDeployment": "gpt-4",
      "EnableMultiModelRouting": true,
      "EnableProviderFallback": true
    }
  },
  "KeyVault": {
    "VaultUri": "https://your-vault.vault.azure.net/",
    "Enabled": true  // ? Secrets in Key Vault
  }
}
```

### Breaking Changes

**None!** The v2.0 release is backward compatible. New features are opt-in via configuration.

---

## Additional Resources

- [LLM Routing Architecture](../docs/LLM_ROUTING_ARCHITECTURE.md)
- [LLM Routing Specification](../docs/LLM_ROUTING_SPECIFICATION.md)
- [Azure Key Vault Setup Guide](../docs/KEY_VAULT_SETUP.md)
- [API Design Documentation](../docs/LLM_ROUTING_API_DESIGN.md)
- [Configuration Reference](../docs/LLM_ROUTING_CONFIGURATION.md)

---

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for development guidelines.

## License

MIT License - see [LICENSE](../LICENSE.txt)
