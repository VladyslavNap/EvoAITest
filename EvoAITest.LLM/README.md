# EvoAITest.LLM - Advanced LLM Integration Library

**Version:** 2.0 with Intelligent Routing & Resilience  
**Status:** Production Ready ✅

LLM provider abstractions for AI-powered browser automation with intelligent multi-model routing, circuit breaker pattern, and secure secret management.

> 📚 **[Complete Documentation](../docs/LLM_INTEGRATION_GUIDE.md)** | 🚀 **[Quick Start](#quick-start)** | 🏗️ **[Architecture](../docs/LLM_ROUTING_ARCHITECTURE.md)**

---

## 🌟 What's New in v2.0

- **✅ Intelligent LLM Routing** - Automatically route requests to optimal models based on task type or cost
- **✅ Circuit Breaker Pattern** - Automatic failover to backup providers when primary fails
- **✅ Azure Key Vault Integration** - Secure API key and secret management
- **✅ Multi-Model Support** - Seamlessly switch between Azure OpenAI, Ollama, and local models
- **✅ Real-time Streaming** - Bidirectional streaming via SignalR for responsive UIs
- **✅ Cost Optimization** - Smart routing to minimize LLM costs while maintaining quality

---

## Overview

EvoAITest.LLM provides a production-ready, enterprise-grade abstraction layer for integrating Large Language Models into browser automation workflows. The library features intelligent routing that automatically selects the best model for each task, circuit breaker resilience for automatic failover, and comprehensive observability.

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
      "EnableMultiModelRouting": true
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

## Key Features

### 🎯 Intelligent Routing
- **Task-Based Routing**: Routes code generation, planning, analysis tasks to specialized models
- **Cost-Optimized Routing**: Minimizes costs by using cheaper models where appropriate
- **Latency-Aware**: Considers response time requirements for route selection
- **Custom Strategies**: Extensible routing strategy pattern

### 🔄 Resilience & Reliability
- **Circuit Breaker**: Automatic failover when providers become unhealthy
- **Retry Logic**: Exponential backoff with jitter for transient failures
- **Health Monitoring**: Tracks provider availability and performance
- **Graceful Degradation**: Falls back to alternative providers seamlessly

### 🔐 Security
- **Azure Key Vault**: Secure storage for API keys and secrets
- **Managed Identity**: Support for Azure managed identities
- **Secret Rotation**: Hot-reload secrets without downtime
- **No-Op Provider**: Development mode without cloud dependencies

### 📊 Observability
- **OpenTelemetry**: Built-in metrics and distributed tracing
- **Structured Logging**: Comprehensive diagnostic information
- **Cost Tracking**: Token usage and cost estimation
- **Performance Metrics**: Latency, success rates, circuit breaker states

---

## Architecture

```
┌─────────────────┐
│ Client Request  │
└────────┬────────┘
         │
         ▼
┌─────────────────────────┐
│  RoutingLLMProvider     │  ◄─── Task Type Detection
│  (Intelligent Router)   │  ◄─── Cost Optimization
└──────────┬──────────────┘
           │
           ▼
┌───────────────────────────┐
│ CircuitBreakerLLMProvider │  ◄─── Health Monitoring
│ (Resilience Layer)        │  ◄─── Automatic Failover
└──────────┬────────────────┘
           │
     ┌─────┴─────┐
     ▼           ▼
┌─────────┐  ┌──────────┐
│ Primary │  │ Fallback │
│Provider │  │ Provider │
└─────────┘  └──────────┘
     │            │
     ▼            ▼
  Azure         Ollama
  OpenAI       (Local)
```

---

## Key Components

### Abstractions

#### ILLMProvider
Unified interface for all LLM providers:

```csharp
public interface ILLMProvider
{
    Task<LLMResponse> CompleteAsync(LLMRequest request, CancellationToken cancellationToken = default);
    IAsyncEnumerable<LLMStreamChunk> StreamCompleteAsync(LLMRequest request, CancellationToken cancellationToken = default);
    Task<float[]> GenerateEmbeddingAsync(string text, string? model = null, CancellationToken cancellationToken = default);
}
```

#### IPromptBuilder
Construct and manage conversation prompts:

```csharp
var conversation = promptBuilder.BuildConversation(
    promptBuilder.BuildSystemPrompt("You are a browser automation assistant"),
    promptBuilder.BuildUserMessage("Navigate to example.com and click the login button")
);
```

### Models

#### LLMRequest
Configure LLM requests:

```csharp
var request = new LLMRequest
{
    Model = "gpt-4",
    Messages = conversation.Messages,
    Temperature = 0.7,
    MaxTokens = 1000,
    ResponseFormat = new ResponseFormat { Type = "json_object" }
};
```

#### LLMResponse
Process LLM responses:

```csharp
var response = await llmProvider.CompleteAsync(request);
Console.WriteLine($"Content: {response.Content}");
Console.WriteLine($"Tokens used: {response.Usage?.TotalTokens}");
```

## Installation

Add to your project:

```bash
dotnet add reference ../EvoAITest.LLM/EvoAITest.LLM.csproj
```

Register services:

```csharp
builder.Services.AddLLMServices(builder.Configuration);
builder.Services.AddPromptBuilder<DefaultPromptBuilder>();
```

The registration binds `EvoAITestCoreOptions` from configuration, wires `LLMProviderFactory`, and exposes `ILLMProvider`. Set `EvoAITest:Core:LLMProvider` to `AzureOpenAI`, `Ollama`, or `Local` (plus the corresponding endpoint/model inputs) in `appsettings*.json` or environment variables to pick the provider without code changes.

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

### Streaming Responses

```csharp
await foreach (var chunk in llmProvider.StreamCompleteAsync(request))
{
    Console.Write(chunk.Delta);
    
    if (chunk.IsComplete)
    {
        Console.WriteLine($"\n\nFinished: {chunk.FinishReason}");
    }
}
```

### Function Calling

```csharp
var request = new LLMRequest
{
    Model = "gpt-4",
    Messages = messages,
    Functions = new List<FunctionDefinition>
    {
        new()
        {
            Name = "click_element",
            Description = "Click an element on the page",
            Parameters = new
            {
                type = "object",
                properties = new
                {
                    selector = new { type = "string", description = "CSS selector" }
                },
                required = new[] { "selector" }
            }
        }
    }
};

var response = await llmProvider.CompleteAsync(request);
if (response.Choices[0].Message?.FunctionCall != null)
{
    var functionCall = response.Choices[0].Message.FunctionCall;
    Console.WriteLine($"Function: {functionCall.Name}");
    Console.WriteLine($"Arguments: {functionCall.Arguments}");
}
```

### Embeddings

```csharp
var text = "Browser automation with AI";
var embedding = await llmProvider.GenerateEmbeddingAsync(text);
Console.WriteLine($"Embedding dimensions: {embedding.Length}");
```

## Provider Support

Ready to integrate:

- **Azure OpenAI** (`EvoAITest.LLM/Providers/AzureOpenAIProvider.cs`) – Azure.AI.OpenAI 2.x SDK with Entra ID (Managed Identity) and API key auth, streaming completions, tool call parsing, embeddings, and token/cost usage.
- **Ollama / Local HTTP** (`EvoAITest.LLM/Providers/OllamaProvider.cs`) – Local completions, streaming, embeddings, and availability checks against any model exposed via the Ollama API.
- **Routing Provider** (`EvoAITest.LLM/Providers/RoutingLLMProvider.cs`) – Multi-model router that chooses GPT-5 vs Qwen/Mistral based on task type, cost, and configured strategy, with built-in fallback logic.

## Advanced Routing & Resilience

### Multi-Model Routing

When `EnableMultiModelRouting = true`, `RoutingLLMProvider` inspects each `LLMRequest` and chooses the ideal model:

- Planning / reasoning → GPT-5 (Azure OpenAI)
- Code / extraction → Qwen2.5-7b (Ollama) or other configured local models
- Task-based vs cost-optimized strategies control prioritization

### Automatic Fallback & Circuit Breakers

- `EnableProviderFallback` keeps requests flowing by falling back to the secondary provider when the primary fails or hits rate limits.
- Circuit breaker settings (`CircuitBreakerFailureThreshold`, `CircuitBreakerOpenDurationSeconds`) ensure unhealthy providers are paused before they cause cascading failures.
- `LLMRequestTimeoutSeconds` enforces per-request limits so callers can abort long-running prompts cleanly.

### Configuration Example

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "LLMModel": "gpt-5",
      "EnableMultiModelRouting": true,
      "RoutingStrategy": "TaskBased",
      "EnableProviderFallback": true,
      "CircuitBreakerFailureThreshold": 5,
      "CircuitBreakerOpenDurationSeconds": 30,
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "qwen2.5-7b"
    }
  }
}
```

See `EVOAITEST_CORE_CONFIGURATION_GUIDE.md` for the full option matrix.

### Tests

The new routing/fallback behaviors are covered by `EvoAITest.Tests/LLM/*`, and the API surface is exercised end-to-end via `ApiIntegrationTests` to ensure planner/executor flows work regardless of provider.
- **Custom Local Endpoints** – Configure `LLMProvider = "Local"` with `LocalLLMEndpoint` to reuse the Ollama provider against compatible HTTP surfaces while a bespoke provider is built.

## Provider Implementations

### Azure OpenAI Provider

Path: `EvoAITest.LLM/Providers/AzureOpenAIProvider.cs`

- Uses `AzureOpenAIClient` + `ChatClient` for GPT-4/5 deployments.
- Dual authentication: API key (Dev/Test) or `DefaultAzureCredential` (Managed Identity) when `AzureOpenAIApiKey` is empty.
- Supports streaming completions, embeddings, JSON tool-call parsing, availability probes, and token/cost tracking.

**Configuration (`appsettings.Development.json` example):**

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "AzureOpenAIEndpoint": "https://your-resource.openai.azure.com",
      "AzureOpenAIDeployment": "gpt-5",
      "AzureOpenAIApiKey": "<use Key Vault/environment in production>"
    }
  }
}
```

### Ollama Provider

Path: `EvoAITest.LLM/Providers/OllamaProvider.cs`

- Talks to the Ollama HTTP API for completions, streaming, embeddings, and model availability checks.
- Automatically estimates token usage and logs helpful diagnostics when the local server/model is missing.

**Configuration (local dev):**

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Ollama",
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "qwen2.5-7b"
    }
  }
}
```

### LLM Provider Factory

Path: `EvoAITest.LLM/Factory/LLMProviderFactory.cs`

- Validates configuration on startup and logs provider creation details.
- Exposes helper APIs like `GetConfiguredProviderName()`, `GetProviderInfo()`, and `IsProviderAvailableAsync()`.
- Registered via `AddLLMServices` so consumers can simply inject `ILLMProvider`.

## Features

- ? Unified provider interface
- ? Streaming support
- ? Function calling / tool-call parsing
- ? Vision inputs (provider-dependent)
- ? Embeddings generation
- ? Conversation management
- ? Response format control
- ? Token usage tracking

## Next Steps

- Implement `ILLMProvider` for your LLM service
- Use with EvoAITest.Agents for intelligent automation
- Build custom prompt templates
