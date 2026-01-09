# LLM Integration - Complete Guide

**Version:** 2.0  
**Status:** Production Ready ?  
**Last Updated:** January 2026

> Comprehensive guide to the EvoAITest LLM integration library with intelligent routing, resilience, and security features.

---

## ?? Table of Contents

- [Overview](#overview)
- [Quick Start](#quick-start)
- [Installation & Configuration](#installation--configuration)
- [Intelligent Routing](#intelligent-routing)
- [Circuit Breaker & Resilience](#circuit-breaker--resilience)
- [Azure Key Vault Integration](#azure-key-vault-integration)
- [API Reference](#api-reference)
- [Usage Examples](#usage-examples)
- [Performance & Cost Optimization](#performance--cost-optimization)
- [Monitoring & Observability](#monitoring--observability)
- [Troubleshooting](#troubleshooting)
- [Migration Guide](#migration-guide)

---

## Overview

EvoAITest.LLM provides a production-ready, enterprise-grade abstraction layer for integrating Large Language Models into browser automation workflows. The library features intelligent routing that automatically selects the best model for each task, circuit breaker resilience for automatic failover, and comprehensive observability.

### What's New in v2.0

- **? Intelligent LLM Routing** - Automatically route requests to optimal models based on task type or cost
- **? Circuit Breaker Pattern** - Automatic failover to backup providers when primary fails
- **? Azure Key Vault Integration** - Secure API key and secret management
- **? Multi-Model Support** - Seamlessly switch between Azure OpenAI, Ollama, and local models
- **? Real-time Streaming** - Bidirectional streaming via SignalR for responsive UIs
- **? Cost Optimization** - Smart routing to minimize LLM costs while maintaining quality

### Key Features

#### ?? Intelligent Routing
- **Task-Based Routing**: Routes code generation, planning, analysis tasks to specialized models
- **Cost-Optimized Routing**: Minimizes costs by using cheaper models where appropriate
- **Latency-Aware**: Considers response time requirements for route selection
- **Custom Strategies**: Extensible routing strategy pattern

#### ?? Resilience & Reliability
- **Circuit Breaker**: Automatic failover when providers become unhealthy
- **Retry Logic**: Exponential backoff with jitter for transient failures
- **Health Monitoring**: Tracks provider availability and performance
- **Graceful Degradation**: Falls back to alternative providers seamlessly

#### ?? Security
- **Azure Key Vault**: Secure storage for API keys and secrets
- **Managed Identity**: Support for Azure managed identities
- **Secret Rotation**: Hot-reload secrets without downtime
- **No-Op Provider**: Development mode without cloud dependencies

#### ?? Observability
- **OpenTelemetry**: Built-in metrics and distributed tracing
- **Structured Logging**: Comprehensive diagnostic information
- **Cost Tracking**: Token usage and cost estimation
- **Performance Metrics**: Latency, success rates, circuit breaker states

---

## Quick Start

### 1. Add Package Reference

```bash
dotnet add reference ../EvoAITest.LLM/EvoAITest.LLM.csproj
```

### 2. Register Services

```csharp
// In Program.cs
builder.Services.AddLLMServices(builder.Configuration);
```

### 3. Basic Configuration

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "AzureOpenAIEndpoint": "https://your-resource.openai.azure.com/",
      "AzureOpenAIDeployment": "gpt-4",
      "EnableMultiModelRouting": true,
      "EnableProviderFallback": true
    }
  }
}
```

### 4. Use the Provider

```csharp
public class MyService
{
    private readonly ILLMProvider _llmProvider;

    public MyService(ILLMProvider llmProvider)
    {
        _llmProvider = llmProvider;
    }

    public async Task<string> GenerateCodeAsync(string prompt)
    {
        var request = new LLMRequest
        {
            Messages = new List<Message>
            {
                new() { Role = MessageRole.User, Content = prompt }
            }
        };

        var response = await _llmProvider.CompleteAsync(request);
        return response.Content;
    }
}
```

---

## Installation & Configuration

### Complete Configuration Example

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

### Environment-Specific Configuration

#### Development
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Ollama",
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "qwen2.5-coder:7b"
    }
  }
}
```

#### Production
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "EnableMultiModelRouting": true,
      "RoutingStrategy": "CostOptimized"
    }
  },
  "KeyVault": {
    "VaultUri": "https://prod-keyvault.vault.azure.net/",
    "Enabled": true
  }
}
```

---

## Intelligent Routing

### How It Works

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
???????????
? Closed  ? ??????
? (Normal)?      ?
???????????      ?
     ?           ?
     ? Failures  ? Success
     ? > Thresh  ?
     ?           ?
???????????      ?
?  Open   ?      ?
?(Failing)?      ?
???????????      ?
     ?           ?
     ? Timeout   ?
     ?           ?
     ?           ?
????????????     ?
?Half-Open ???????
? (Testing)?
????????????
```

### Configuration

```json
{
  "EvoAITest": {
    "Core": {
      "CircuitBreakerFailureThreshold": 5,
      "CircuitBreakerOpenDurationSeconds": 30
    }
  }
}
```

### Usage Example

```csharp
// Circuit breaker handles failures automatically
try
{
    var response = await llmProvider.CompleteAsync(request);
}
catch (CircuitBreakerOpenException ex)
{
    // Primary provider is down, automatically using fallback
    _logger.LogWarning("Circuit breaker open, using fallback provider");
}
```

---

## Azure Key Vault Integration

### Setup Steps

1. **Create Key Vault**:
```bash
az keyvault create \
  --name your-keyvault \
  --resource-group your-rg \
  --location eastus
```

2. **Add Secrets**:
```bash
az keyvault secret set \
  --vault-name your-keyvault \
  --name AzureOpenAI--ApiKey \
  --value "your-api-key"
```

3. **Configure Managed Identity**:
```bash
az webapp identity assign \
  --name your-app \
  --resource-group your-rg
```

4. **Grant Access**:
```bash
az keyvault set-policy \
  --name your-keyvault \
  --object-id <managed-identity-id> \
  --secret-permissions get list
```

### Configuration

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

### Usage

Secrets are automatically loaded from Key Vault:

```csharp
// No code changes needed - secrets are loaded transparently
var llmProvider = serviceProvider.GetRequiredService<ILLMProvider>();
// API key is retrieved from Key Vault automatically
```

---

## API Reference

### Core Interfaces

#### ILLMProvider

Main interface for LLM operations:

```csharp
public interface ILLMProvider
{
    Task<LLMResponse> CompleteAsync(
        LLMRequest request, 
        CancellationToken cancellationToken = default);
    
    IAsyncEnumerable<LLMStreamChunk> StreamCompleteAsync(
        LLMRequest request, 
        CancellationToken cancellationToken = default);
    
    Task<float[]> GenerateEmbeddingAsync(
        string text, 
        string? model = null, 
        CancellationToken cancellationToken = default);
}
```

#### IRoutingStrategy

Custom routing implementation:

```csharp
public interface IRoutingStrategy
{
    string Name { get; }
    int Priority { get; }
    
    Task<RouteInfo?> SelectRouteAsync(
        LLMRequest request,
        LLMRoutingOptions options,
        CancellationToken cancellationToken = default);
}
```

### Key Models

#### LLMRequest

```csharp
public class LLMRequest
{
    public List<Message> Messages { get; set; }
    public string? Model { get; set; }
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
    public TaskType? TaskTypeHint { get; set; }
}
```

#### LLMResponse

```csharp
public class LLMResponse
{
    public string Content { get; set; }
    public string Model { get; set; }
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public TimeSpan Duration { get; set; }
}
```

---

## Usage Examples

### Basic Completion

```csharp
var request = new LLMRequest
{
    Messages = new List<Message>
    {
        new() { Role = MessageRole.User, Content = "Explain async/await in C#" }
    }
};

var response = await _llmProvider.CompleteAsync(request);
Console.WriteLine(response.Content);
```

### Streaming Response

```csharp
var request = new LLMRequest
{
    Messages = new List<Message>
    {
        new() { Role = MessageRole.User, Content = "Write a long essay" }
    }
};

await foreach (var chunk in _llmProvider.StreamCompleteAsync(request))
{
    Console.Write(chunk.Content);
}
```

### With Task Type Hint

```csharp
var request = new LLMRequest
{
    Messages = new List<Message>
    {
        new() { Role = MessageRole.User, Content = "Generate a bubble sort function" }
    },
    TaskTypeHint = TaskType.CodeGeneration  // Routes to code-specialized model
};

var response = await _llmProvider.CompleteAsync(request);
```

### Multi-Turn Conversation

```csharp
var messages = new List<Message>
{
    new() { Role = MessageRole.System, Content = "You are a helpful coding assistant" },
    new() { Role = MessageRole.User, Content = "How do I sort an array?" }
};

var response1 = await _llmProvider.CompleteAsync(new LLMRequest { Messages = messages });

messages.Add(new Message { Role = MessageRole.Assistant, Content = response1.Content });
messages.Add(new Message { Role = MessageRole.User, Content = "Show me an example in C#" });

var response2 = await _llmProvider.CompleteAsync(new LLMRequest { Messages = messages });
```

---

## Performance & Cost Optimization

### Cost Reduction Strategies

1. **Use Local Models for Development**:
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Ollama",
      "OllamaModel": "qwen2.5-coder:7b"
    }
  }
}
```

2. **Route Simple Tasks to Cheaper Models**:
```json
{
  "LLMRouting": {
    "Routes": {
      "General": {
        "PrimaryProvider": "Ollama",
        "CostPer1KTokens": 0.0
      }
    }
  }
}
```

3. **Cache Frequently Used Responses**:
```json
{
  "LLMRouting": {
    "EnableRoutingCache": true,
    "RoutingCacheDurationMinutes": 60
  }
}
```

### Performance Tuning

1. **Adjust Timeouts**:
```json
{
  "LLMRouting": {
    "Routes": {
      "CodeGeneration": {
        "MaxLatencyMs": 3000
      }
    }
  }
}
```

2. **Use Streaming for Long Responses**:
```csharp
await foreach (var chunk in _llmProvider.StreamCompleteAsync(request))
{
    // Process chunks as they arrive
}
```

---

## Monitoring & Observability

### Metrics

Key metrics tracked by the system:

- `llm.requests.total` - Total number of requests
- `llm.requests.duration` - Request duration histogram
- `llm.requests.tokens` - Token usage
- `llm.requests.cost` - Estimated cost
- `llm.circuit_breaker.state` - Circuit breaker state changes
- `llm.routing.decisions` - Routing decisions made

### Logging

Structured logging examples:

```csharp
_logger.LogInformation(
    "LLM request completed. Provider={Provider}, Model={Model}, Tokens={Tokens}, Duration={Duration}ms",
    "AzureOpenAI", "gpt-4", 150, 1234);
```

### Tracing

OpenTelemetry traces include:

- Request routing decisions
- Provider selection
- Circuit breaker state transitions
- Token usage
- Latency breakdown

---

## Troubleshooting

### Common Issues

#### Circuit Breaker Keeps Opening

**Symptom**: Frequent circuit breaker open exceptions

**Solutions**:
1. Check provider health
2. Increase failure threshold
3. Add retry logic
4. Configure fallback provider

```json
{
  "EvoAITest": {
    "Core": {
      "CircuitBreakerFailureThreshold": 10,
      "EnableProviderFallback": true
    }
  }
}
```

#### High Costs

**Symptom**: Unexpected API costs

**Solutions**:
1. Enable cost-optimized routing
2. Use local models for development
3. Implement caching
4. Monitor token usage

```json
{
  "LLMRouting": {
    "RoutingStrategy": "CostOptimized"
  }
}
```

#### Slow Response Times

**Symptom**: Long wait times for responses

**Solutions**:
1. Use streaming for long responses
2. Adjust latency thresholds
3. Use faster models for simple tasks
4. Enable caching

---

## Migration Guide

### From v1.0 to v2.0

#### Update Configuration

**Before (v1.0)**:
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI"
    }
  }
}
```

**After (v2.0)**:
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "EnableMultiModelRouting": true,
      "EnableProviderFallback": true
    }
  }
}
```

#### Code Changes

No breaking changes - existing code continues to work:

```csharp
// v1.0 code still works in v2.0
var response = await _llmProvider.CompleteAsync(request);
```

New features are opt-in:

```csharp
// v2.0 - use streaming (optional)
await foreach (var chunk in _llmProvider.StreamCompleteAsync(request))
{
    Console.Write(chunk.Content);
}
```

---

## Additional Resources

### Documentation

- [Architecture Deep Dive](LLM_ROUTING_ARCHITECTURE.md)
- [API Design Details](LLM_ROUTING_API_DESIGN.md)
- [Feature Specification](LLM_ROUTING_SPECIFICATION.md)

### External Links

- [Azure OpenAI Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Ollama Documentation](https://ollama.ai/docs)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/instrumentation/net/)

---

**Version:** 2.0  
**Last Updated:** January 2026  
**Maintained by:** EvoAITest Team
