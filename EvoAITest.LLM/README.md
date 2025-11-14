# BrowserAI LLM Library

LLM provider abstractions for AI-powered browser automation.

## Overview

EvoAITest.LLM provides unified interfaces for integrating Large Language Models into browser automation workflows. It supports multiple LLM providers with a consistent API.

## Key Components

### Abstractions

#### ILLMProvider
Unified interface for LLM providers:

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
builder.Services.AddLLMServices();
builder.Services.AddLLMProvider<OpenAIProvider>();
builder.Services.AddPromptBuilder<DefaultPromptBuilder>();
```

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

- **OpenAI** - GPT-4, GPT-3.5-Turbo
- **Azure OpenAI** - Enterprise-grade OpenAI
- **Anthropic** - Claude 3 family
- **Google** - Gemini models
- **Local Models** - Ollama, LM Studio

## Features

- ? Unified provider interface
- ? Streaming support
- ? Function calling
- ? Vision inputs (provider-dependent)
- ? Embeddings generation
- ? Conversation management
- ? Response format control
- ? Token usage tracking

## Next Steps

- Implement `ILLMProvider` for your LLM service
- Use with EvoAITest.Agents for intelligent automation
- Build custom prompt templates
