# EvoAITest - Phase 1 & Phase 2 Action Plan

> **Project Status**: Day 7 LLM provider factory + Azure/Ollama providers landed (code, DI, docs). Keep the [Day 5 Implementation Checklist](DAY5_CHECKLIST.md) plus the [Implementation Summary](IMPLEMENTATION_SUMMARY.md) for baseline context and use this roadmap for Day 8+ execution. Deep dives for the new stack live in [AZURE_OPENAI_PROVIDER_SUMMARY.md](AZURE_OPENAI_PROVIDER_SUMMARY.md), [OLLAMA_PROVIDER_SUMMARY.md](OLLAMA_PROVIDER_SUMMARY.md), and [LLM_PROVIDER_FACTORY_SUMMARY.md](LLM_PROVIDER_FACTORY_SUMMARY.md).

## How to Navigate the Docs
- [README](README.md) – high-level overview, architecture, and environment setup.
- [Quick Reference](QUICK_REFERENCE.md) – API cheatsheet while implementing agents or tools.
- [Browser Tool Registry Deep Dive](BROWSER_TOOL_REGISTRY_SUMMARY.md) – complete contract for the 13 tools.
- [Automation Models Primer](AUTOMATION_TASK_MODELS_SUMMARY.md) – data models, enums, and persistence notes.

## ✅ Days 1–7 Snapshot
Day 5 artifacts still capture the foundation. Day 6 delivered the concrete Playwright browser agent (`EvoAITest.Core/Browser/PlaywrightBrowserAgent.cs`), DI wiring, and regression tests (`EvoAITest.Tests/Browser/PlaywrightBrowserAgentTests.cs`). Day 7 layered in the multi-provider LLM stack (Azure OpenAI + Ollama + `LLMProviderFactory`) along with configuration-bound DI registration. Keep the README “Latest Update (Day 7)” section handy when referencing either automation surface.

---

## 🚧 IN PROGRESS / NEXT STEPS

## PHASE 1: WEEKS 2-3 - BROWSER AUTOMATION & LLM IMPLEMENTATION (10 Days)

---

### Day 6: Playwright Browser Agent Implementation

**Goal**: Implement `PlaywrightBrowserAgent` that fulfills `IBrowserAgent` interface.

**Action 6.1: Install Playwright**
```bash
cd EvoAITest.Core
dotnet add package Microsoft.Playwright --version 1.48.0

# Install browser binaries
pwsh bin/Debug/net10.0/playwright.ps1 install
```

**Action 6.2: Create PlaywrightBrowserAgent**

Create `EvoAITest.Core/Browser/PlaywrightBrowserAgent.cs`:
```csharp
namespace EvoAITest.Core.Browser;

using Microsoft.Playwright;
using Microsoft.Extensions.Logging;
using EvoAITest.Core.Models;
using EvoAITest.Core.Abstractions;

public class PlaywrightBrowserAgent : IBrowserAgent
{
    private readonly ILogger<PlaywrightBrowserAgent> _logger;
    private IPlaywright _playwright;
    private IBrowser _browser;
    private IBrowserContext _context;
    private IPage _page;

    public PlaywrightBrowserAgent(ILogger<PlaywrightBrowserAgent> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing Playwright browser");
        
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--disable-blink-features=AutomationControlled" }
        });

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
        });

        _page = await _context.NewPageAsync();
        _logger.LogInformation("Playwright browser initialized successfully");
    }

    public async Task<PageState> GetPageStateAsync()
    {
        var url = _page.Url;
        var title = await _page.TitleAsync();
        var accessibilityTree = await GetAccessibilityTreeAsync();
        var interactiveElements = await ExtractInteractiveElementsAsync();
        
        return new PageState
        {
            Url = url,
            Title = title,
            LoadState = LoadState.Load,
            InteractiveElements = interactiveElements,
            CapturedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task NavigateAsync(string url)
    {
        _logger.LogInformation("Navigating to {Url}", url);
        await _page.GotoAsync(url, new PageGotoOptions 
        { 
            WaitUntil = WaitUntilState.NetworkIdle,
            Timeout = 30000
        });
    }

    public async Task ClickAsync(string selector, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                await _page.ClickAsync(selector, new PageClickOptions { Timeout = 5000 });
                return;
            }
            catch when (i < maxRetries - 1)
            {
                _logger.LogWarning("Click failed, retry {Attempt}/{Max}", i + 1, maxRetries);
                await Task.Delay(1000);
            }
        }
        throw new InvalidOperationException($"Failed to click {selector} after {maxRetries} attempts");
    }

    public async Task TypeAsync(string selector, string text)
    {
        await _page.FillAsync(selector, text);
    }

    public async Task<string> GetTextAsync(string selector)
    {
        return await _page.TextContentAsync(selector) ?? string.Empty;
    }

    public async Task<string> TakeScreenshotAsync()
    {
        var bytes = await _page.ScreenshotAsync(new PageScreenshotOptions 
        { 
            Type = ScreenshotType.Png,
            FullPage = true
        });
        return Convert.ToBase64String(bytes);
    }

    public async Task<string> GetAccessibilityTreeAsync()
    {
        var snapshot = await _page.Accessibility.SnapshotAsync();
        return System.Text.Json.JsonSerializer.Serialize(snapshot);
    }

    public async Task WaitForElementAsync(string selector, int timeoutMs = 30000)
    {
        await _page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions 
        { 
            Timeout = timeoutMs 
        });
    }

    public async Task<string> GetPageHtmlAsync()
    {
        return await _page.ContentAsync();
    }

    private async Task<List<ElementInfo>> ExtractInteractiveElementsAsync()
    {
        var elements = await _page.EvaluateAsync<List<Dictionary<string, object>>>(@"
            () => {
                const elements = [];
                document.querySelectorAll('button, input, a, select, textarea, [role=button], [role=link]').forEach((el, index) => {
                    const rect = el.getBoundingClientRect();
                    if (rect.width > 0 && rect.height > 0) {
                        elements.push({
                            tagName: el.tagName,
                            selector: el.id ? `#${el.id}` : `${el.tagName.toLowerCase()}:nth-of-type(${index + 1})`,
                            text: el.textContent?.substring(0, 100),
                            isVisible: rect.top < window.innerHeight && rect.bottom > 0,
                            isInteractable: !el.disabled && window.getComputedStyle(el).pointerEvents !== 'none'
                        });
                    }
                });
                return elements;
            }
        ");

        return elements.Select(e => new ElementInfo
        {
            TagName = e["tagName"].ToString(),
            Selector = e["selector"].ToString(),
            Text = e["text"].ToString(),
            IsVisible = (bool)e["isVisible"],
            IsInteractable = (bool)e["isInteractable"]
        }).ToList();
    }

    public async ValueTask DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
        _logger.LogInformation("Playwright browser disposed");
    }
}
```

**Action 6.3: Register in DI**

Update `EvoAITest.Core/Extensions/ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IBrowserAgent, PlaywrightBrowserAgent>();
```

**Action 6.4: Create Unit Tests**

Create `EvoAITest.Tests/Browser/PlaywrightBrowserAgentTests.cs`:
```csharp
public class PlaywrightBrowserAgentTests
{
    [Fact]
    public async Task InitializeAsync_ShouldSucceed()
    {
        var logger = new Mock<ILogger<PlaywrightBrowserAgent>>();
        var agent = new PlaywrightBrowserAgent(logger.Object);
        
        await agent.InitializeAsync();
        
        await agent.DisposeAsync();
    }

    [Fact]
    public async Task NavigateAsync_ShouldLoadPage()
    {
        var agent = CreateAgent();
        await agent.InitializeAsync();
        
        await agent.NavigateAsync("https://example.com");
        var state = await agent.GetPageStateAsync();
        
        state.Url.Should().Contain("example.com");
        state.Title.Should().NotBeNullOrEmpty();

        await agent.DisposeAsync();
    }

    [Fact]
    public async Task TakeScreenshot_ShouldReturnBase64String()
    {
        var agent = CreateAgent();
        await agent.InitializeAsync();
        await agent.NavigateAsync("https://example.com");

        var screenshot = await agent.TakeScreenshotAsync();
        
        screenshot.Should().NotBeNullOrEmpty();
        // Validate Base64 string
        var data = Convert.FromBase64String(screenshot);
        data.Should().NotBeEmpty();

        await agent.DisposeAsync();
    }
}
```

**Daily Commit:**
```
feat: implement Playwright browser agent with page state extraction
```

**Status**: ✅ Complete — PlaywrightBrowserAgent now powers default browser automation (DI + unit tests).

---

### Day 7: LLM Provider Implementation (OpenAI + Ollama)

**Goal**: Implement concrete LLM providers for Azure OpenAI and Ollama.

Day 7 shipped the full stack:
- `EvoAITest.LLM/Providers/AzureOpenAIProvider.cs` (Azure.AI.OpenAI 2.x + Entra ID fallback, streaming, embeddings, tool-call parsing, usage tracking)
- `EvoAITest.LLM/Providers/OllamaProvider.cs` (local completions, streaming, embeddings, availability checks)
- `EvoAITest.LLM/Factory/LLMProviderFactory.cs` (configuration-driven provider selection, availability probe helpers)
- `EvoAITest.LLM/Extensions/ServiceCollectionExtensions.cs` (`AddLLMServices` binds `EvoAITestCoreOptions`, registers the factory, and exposes `ILLMProvider`)
- Updated appsettings + options for provider-specific knobs (`EvoAITest.Core/Options/EvoAITestCoreOptions.cs`)

**Action 7.1: Install Azure OpenAI SDK**
```bash
cd EvoAITest.LLM
dotnet add package Azure.AI.OpenAI --version 2.1.0
```

**Action 7.2: Create OpenAI Provider**

Create `EvoAITest.LLM/Providers/AzureOpenAIProvider.cs`:
```csharp
namespace EvoAITest.LLM.Providers;

using Azure.AI.OpenAI;
using Azure;
using EvoAITest.LLM.Abstractions;
using EvoAITest.Core.Models;
using System.ClientModel;
using Microsoft.Extensions.Logging;

public class AzureOpenAIProvider : ILLMProvider
{
    private readonly AzureOpenAIClient _client;
    private readonly string _deploymentName;
    private readonly ILogger<AzureOpenAIProvider> _logger;
    private TokenUsage _lastUsage;

    public AzureOpenAIProvider(
        string endpoint,
        string apiKey,
        string deploymentName,
        ILogger<AzureOpenAIProvider> logger)
    {
        _deploymentName = deploymentName;
        _logger = logger;
        _client = new AzureOpenAIClient(
            new Uri(endpoint),
            new AzureKeyCredential(apiKey)
        );
    }

    public async Task<string> GenerateAsync(
        string prompt,
        Dictionary<string, string> variables = null,
        List<BrowserTool> tools = null,
        int maxTokens = 2000)
    {
        var finalPrompt = SubstituteVariables(prompt, variables);
        
        var chatClient = _client.GetChatClient(_deploymentName);
        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("You are an AI browser automation assistant."),
            new UserChatMessage(finalPrompt)
        };

        var options = new ChatCompletionOptions
        {
            MaxTokens = maxTokens,
            Temperature = 0.7f
        };

        var response = await chatClient.CompleteChatAsync(messages, options);
        
        _lastUsage = new TokenUsage(
            response.Value.Usage.InputTokens,
            response.Value.Usage.OutputTokens,
            CalculateCost(response.Value.Usage)
        );

        return response.Value.Content[0].Text;
    }

    public Task<List<ToolCall>> ParseToolCallsAsync(string response)
    {
        // Parse JSON response for tool calls
        var calls = new List<ToolCall>();
        try
        {
            var json = System.Text.Json.JsonDocument.Parse(response);
            // Implementation...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse tool calls");
        }
        return Task.FromResult(calls);
    }

    public string GetModelName() => _deploymentName;
    
    public TokenUsage GetLastTokenUsage() => _lastUsage;
    
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var chatClient = _client.GetChatClient(_deploymentName);
            await chatClient.CompleteChatAsync(new[] { new UserChatMessage("test") });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string SubstituteVariables(string prompt, Dictionary<string, string> variables)
    {
        if (variables == null) return prompt;
        
        foreach (var (key, value) in variables)
        {
            prompt = prompt.Replace($"{{{key}}}", value);
        }
        return prompt;
    }

    private decimal CalculateCost(ChatTokenUsage usage)
    {
        // GPT-5 pricing (adjust as needed)
        const decimal inputCost = 0.01m / 1000;
        const decimal outputCost = 0.03m / 1000;
        return (usage.InputTokens * inputCost) + (usage.OutputTokens * outputCost);
    }
}
```

**Action 7.3: Create Ollama Provider**

Create `EvoAITest.LLM/Providers/OllamaProvider.cs`:
```csharp
namespace EvoAITest.LLM.Providers;

using EvoAITest.LLM.Abstractions;
using EvoAITest.Core.Models;
using System.Net.Http.Json;

public class OllamaProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly string _baseUrl;

    public OllamaProvider(string baseUrl, string model)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _model = model;
        _httpClient = new HttpClient();
    }

    public async Task<string> GenerateAsync(
        string prompt,
        Dictionary<string, string> variables = null,
        List<BrowserTool> tools = null,
        int maxTokens = 2000)
    {
        var finalPrompt = SubstituteVariables(prompt, variables);
        
        var request = new
        {
            model = _model,
            prompt = finalPrompt,
            stream = false,
            options = new { num_predict = maxTokens }
        };

        var response = await _httpClient.PostAsJsonAsync(
            $"{_baseUrl}/api/generate",
            request
        );

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>();
        return result.Response;
    }

    public Task<List<ToolCall>> ParseToolCallsAsync(string response)
    {
        // Ollama JSON parsing
        return Task.FromResult(new List<ToolCall>());
    }

    public string GetModelName() => _model;
    
    public TokenUsage GetLastTokenUsage() => new(0, 0, 0);
    
    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/tags");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string SubstituteVariables(string prompt, Dictionary<string, string> variables)
    {
        if (variables == null) return prompt;
        foreach (var (key, value) in variables)
        {
            prompt = prompt.Replace($"{{{key}}}", value);
        }
        return prompt;
    }

    private record OllamaResponse(string Response);
}
```

**Action 7.4: Create LLM Provider Factory**

Create `EvoAITest.LLM/Factory/LLMProviderFactory.cs`:
```csharp
namespace EvoAITest.LLM.Factory;

using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Providers;
using EvoAITest.Core.Options;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

public class LLMProviderFactory
{
    private readonly EvoAITestCoreOptions _options;
    private readonly ILoggerFactory _loggerFactory;

    public LLMProviderFactory(
        IOptions<EvoAITestCoreOptions> options,
        ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _loggerFactory = loggerFactory;
    }

    public ILLMProvider CreateProvider()
    {
        return _options.LLMProvider switch
        {
            "AzureOpenAI" => new AzureOpenAIProvider(
                _options.AzureOpenAIEndpoint,
                _options.AzureOpenAIApiKey,
                _options.AzureOpenAIDeployment,
                _loggerFactory.CreateLogger<AzureOpenAIProvider>()
            ),
            "Ollama" => new OllamaProvider(
                _options.OllamaEndpoint,
                _options.OllamaModel
            ),
            _ => throw new InvalidOperationException($"Unknown LLM provider: {_options.LLMProvider}")
        };
    }
}
```

**Action 7.5: Register in DI**

Update `EvoAITest.LLM/Extensions/ServiceCollectionExtensions.cs`:
```csharp
public static IServiceCollection AddLLMServices(
    this IServiceCollection services,
    IConfiguration configuration)
{
    services.Configure<EvoAITestCoreOptions>(
        configuration.GetSection("EvoAITest:Core"));

    services.TryAddSingleton<LLMProviderFactory>();

    services.TryAddScoped<ILLMProvider>(sp =>
    {
        var factory = sp.GetRequiredService<LLMProviderFactory>();
        return factory.CreateProvider();
    });

    return services;
}
```

**Daily Commit:**
```
feat: implement Azure OpenAI and Ollama providers with factory pattern
```

**Status**: ✅ Complete — Azure OpenAI + Ollama providers, the configurable factory, and the DI extension now ship from `EvoAITest.LLM`.

---

### Day 8-15: Agent Implementation (Planner, Executor, Healer)

*(Continue with agent implementations following the same pattern...)*

---

## 📝 SUMMARY OF REMAINING WORK

### Phase 1 Remaining (Days 6-21)
- [x] Day 6: Playwright browser implementation
- [x] Day 7: LLM provider implementations
- [ ] Day 8: Tool executor service
- [ ] Day 9: Planner agent (natural language → execution plan)
- [ ] Day 10: Executor agent (run automation steps)
- [ ] Day 11: Healer agent (error recovery)
- [ ] Day 12: EF Core database models
- [ ] Day 13: Database migrations
- [ ] Day 14: Repository pattern
- [ ] Day 15: API endpoints (Task CRUD)
- [ ] Day 16: API endpoints (Execution)
- [ ] Day 17: Docker containerization
- [ ] Day 18: CI/CD pipeline
- [ ] Day 19: Integration tests
- [ ] Day 20: Example automation (login)
- [ ] Day 21: Documentation

### Phase 2 Focus (Days 22-49)
- Multi-agent coordination
- Advanced error recovery
- Dashboard UI
- Performance optimization
- Production deployment

---

## 🎯 IMMEDIATE NEXT STEPS (Day 8)

1. **Design Tool Executor service**: Define inputs/outputs, error model, and telemetry hooks for coordinating `BrowserTool` invocations.
2. **Implement ToolExecutor**: Leverage `IBrowserAgent` + registry metadata to run tool sequences with retry/backoff semantics.
3. **Wire into DI + options**: Expose the executor via `EvoAITest.Core` extensions and add configuration flags for max concurrency/timeouts.
4. **Add unit tests**: Cover successful runs, retry paths, cancellation, and logging breadcrumbs.
5. **Commit**: `feat: add tool executor service`

---

## 📊 PROGRESS TRACKING

| Component | Status | Tests | Integration |
|-----------|--------|-------|-------------|
| Project Setup | ✅ Complete | ✅ | ✅ |
| Azure Configuration | ✅ Complete | ✅ | ✅ |
| Core Models | ✅ Complete | ✅ | ✅ |
| Tool Registry | ✅ Complete | ✅ | ✅ |
| Configuration | ✅ Complete | ✅ | ✅ |
| **Playwright Agent** | ✅ Complete | ✅ | ✅ |
| LLM Providers | ✅ Complete | ✅ | ✅ |
| Planner Agent | ⏳ Pending | ⏳ | ⏳ |
| Executor Agent | ⏳ Pending | ⏳ | ⏳ |
| Healer Agent | ⏳ Pending | ⏳ | ⏳ |
| Database | ⏳ Pending | ⏳ | ⏳ |
| API Endpoints | ⏳ Pending | ⏳ | ⏳ |

---

**Current Status**: ✅ Day 7 Complete | 🚧 Day 8 Starting  
**Next Milestone**: Tool Executor Service  
**Target**: Complete Phase 1 (21 days) by end of month
