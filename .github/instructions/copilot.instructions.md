# EvoAITest - GitHub Copilot Instructions

## Project Overview

EvoAITest is an AI-powered browser automation framework built with .NET 10 Aspire, designed to convert natural language instructions into automated browser actions using AI agents. The system uses Azure OpenAI (GPT-5) for production and Ollama for local development.

**Primary Purpose:** Enable non-technical users to automate web tasks by describing them in plain English (e.g., "Login to example.com with these credentials and download the report").

---

## Architecture & Technology Stack

### Core Technologies
- **.NET 10** with C# 12 (latest preview)
- **Aspire Framework** for cloud-native distributed applications
- **Azure Container Apps** for deployment
- **Playwright** for browser automation
- **Entity Framework Core 9** for data persistence
- **Azure OpenAI GPT-5** (production) / **Ollama** (local development)

### Project Structure

```
EvoAITest/
├── EvoAITest.Core/              # Browser automation core & abstractions
│   ├── Abstractions/            # Interfaces (IBrowserAgent, IToolExecutor, etc.)
│   ├── Browser/                 # PlaywrightBrowserAgent implementation
│   ├── Models/                  # Domain models (PageState, AutomationTask, etc.)
│   ├── Services/                # DefaultToolExecutor, BrowserToolRegistry
│   └── Options/                 # Configuration classes
│
├── EvoAITest.LLM/               # LLM provider integrations
│   ├── Abstractions/            # ILLMProvider interface
│   ├── Providers/               # AzureOpenAIProvider, OllamaProvider
│   └── Factory/                 # LLMProviderFactory
│
├── EvoAITest.Agents/            # AI agent orchestration
│   ├── Agents/                  # PlannerAgent, ExecutorAgent, HealerAgent
│   ├── Abstractions/            # IPlanner, IExecutor, IHealer
│   └── Models/                  # AgentTask, AgentStep, AgentTaskResult
│
├── EvoAITest.ApiService/        # REST API endpoints
│   └── Program.cs               # Minimal API endpoints
│
├── EvoAITest.Web/               # Blazor frontend
│   └── Components/              # Razor components
│
├── EvoAITest.AppHost/           # Aspire orchestration
│   └── AppHost.cs               # Service topology definition
│
├── EvoAITest.ServiceDefaults/   # Shared Aspire configuration
│   └── Extensions.cs            # Common service setup
│
└── EvoAITest.Tests/             # Unit & integration tests
    ├── Agents/                  # Agent tests
    ├── Browser/                 # Browser automation tests
    └── Services/                # Service layer tests
```

---

## Key Concepts & Patterns

### 1. Three-Agent System

The framework uses three specialized AI agents that work together:

**PlannerAgent** (`EvoAITest.Agents/Agents/PlannerAgent.cs`)
- Converts natural language to structured execution plans
- Uses Azure OpenAI GPT-5 with tool calling
- Returns `List<ExecutionStep>` with browser actions
- Example: "Login to site" → [navigate, wait_for_element, type, click]

**ExecutorAgent** (To be implemented - Day 10)
- Executes browser automation steps sequentially
- Uses `IBrowserAgent` (Playwright wrapper)
- Captures results, screenshots, and errors
- Returns `ExecutionResult` with step outcomes

**HealerAgent** (To be implemented - Day 11)
- Analyzes execution failures
- Uses LLM to diagnose root causes
- Suggests alternative approaches (different selectors, timing adjustments)
- Re-plans and retries with modifications

### 2. Browser Tool Registry

13 predefined browser automation tools available to AI agents:

| Tool | Purpose | Parameters |
|------|---------|------------|
| `navigate` | Navigate to URL | url (string) |
| `click` | Click element | selector (string) |
| `type` | Type text into input | selector, text |
| `clear_input` | Clear input field | selector |
| `extract_text` | Get element text | selector |
| `extract_table` | Extract table data | selector |
| `get_page_state` | Get page info | none |
| `take_screenshot` | Capture screenshot | none |
| `wait_for_element` | Wait for element | selector, timeout_ms |
| `wait_for_url_change` | Wait for URL change | expected_url |
| `select_option` | Select dropdown option | selector, value |
| `submit_form` | Submit form | selector |
| `verify_element_exists` | Check element exists | selector |

**Implementation:** `EvoAITest.Core/Models/BrowserToolRegistry.cs`

### 3. Configuration Strategy

**Local Development:**
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

**Production (Azure):**
```bash
# Environment Variables
EVOAITEST__CORE__LLMPROVIDER=AzureOpenAI
AZURE_OPENAI_ENDPOINT=https://.cognitiveservices.azure.com
# API key from Key Vault: -keyvault/LLMAPIKEY
```

### 4. Aspire Service Discovery

Services communicate via Aspire's built-in service discovery:

```csharp
// In EvoAITest.AppHost/AppHost.cs
var sql = builder.AddSqlServer("sql").AddDatabase("evoaidb");
var cache = builder.AddRedis("cache");

var apiService = builder.AddProject<Projects.EvoAITest_ApiService>("api")
    .WithReference(sql)
    .WithReference(cache);

var webService = builder.AddProject<Projects.EvoAITest_Web>("web")
    .WithReference(apiService);
```

Services reference each other by name (e.g., "api", "cache") - Aspire handles DNS resolution.

---

## Coding Standards & Conventions

### Naming Conventions

**Namespaces:**
- Follow project structure: `EvoAITest.{ProjectName}.{Folder}`
- Examples: `EvoAITest.Core.Models`, `EvoAITest.Agents.Agents`, `EvoAITest.LLM.Providers`

**Classes:**
- Use descriptive names: `PlaywrightBrowserAgent`, `AzureOpenAIProvider`, `DefaultToolExecutor`
- Suffix interfaces with `I`: `IBrowserAgent`, `ILLMProvider`, `IPlanner`

**Methods:**
- Async methods end with `Async`: `PlanAsync`, `ExecuteAsync`, `NavigateAsync`
- Use verb-first naming: `GetPageStateAsync`, `CreateTaskAsync`, `ValidateConfiguration`

**Configuration:**
- Options classes end with `Options`: `EvoAITestCoreOptions`, `BrowserOptions`, `NavigationOptions`
- Use hierarchical configuration: `EvoAITest:Core:LLMProvider`

### Code Quality Standards

**1. Async/Await Pattern:**
```csharp
// ✅ ALWAYS include CancellationToken for Aspire graceful shutdown
public async Task<ExecutionResult> ExecuteAsync(
    List<ExecutionStep> plan, 
    CancellationToken cancellationToken = default)
{
    foreach (var step in plan)
    {
        // Pass cancellation token through call chain
        await ExecuteStepAsync(step, cancellationToken);
    }
}
```

**2. Error Handling:**
```csharp
// ✅ Catch specific exceptions, log with context, re-throw or wrap
try 
{
    var result = await _llmProvider.GenerateAsync(prompt, cancellationToken);
}
catch (HttpRequestException ex)
{
    _logger.LogError(ex, "LLM API call failed for task {TaskId}", taskId);
    throw new InvalidOperationException("Failed to generate execution plan", ex);
}
```

**3. Logging with Structured Data:**
```csharp
// ✅ Use structured logging with named properties
_logger.LogInformation(
    "Executing task {TaskId} with {StepCount} steps for user {UserId}",
    task.Id, plan.Count, userId);

// ❌ Don't use string interpolation
_logger.LogInformation($"Executing task {task.Id}");
```

**4. Dependency Injection:**
```csharp
// ✅ Constructor injection for all dependencies
public class PlannerAgent
{
    private readonly ILLMProvider _llmProvider;
    private readonly ILogger<PlannerAgent> _logger;
    
    public PlannerAgent(ILLMProvider llmProvider, ILogger<PlannerAgent> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

**5. XML Documentation:**
```csharp
/// <summary>
/// Converts natural language instructions into structured execution plans using GPT-5.
/// </summary>
/// <param name="prompt">Natural language task description from user</param>
/// <param name="cancellationToken">Cancellation token for graceful shutdown</param>
/// <returns>List of execution steps with browser tool calls</returns>
/// <exception cref="InvalidOperationException">Thrown when LLM call fails or returns invalid JSON</exception>
public async Task<List<ExecutionStep>> PlanAsync(
    string prompt, 
    CancellationToken cancellationToken = default)
```

**6. Nullable Reference Types:**
```csharp
#nullable enable

// ✅ Explicit nullability
public record PageState(
    string Url,                          // Non-nullable
    string Title,                        // Non-nullable
    string? AccessibilityTree,           // Nullable
    Dictionary<string, string>? Metadata // Nullable
);
```

**7. Configuration Validation:**
```csharp
public void ValidateConfiguration()
{
    if (LLMProvider == "AzureOpenAI")
    {
        if (string.IsNullOrEmpty(AzureOpenAIEndpoint))
            throw new InvalidOperationException(
                "AzureOpenAIEndpoint required when using AzureOpenAI provider. " +
                "Set AZURE_OPENAI_ENDPOINT environment variable.");
        
        if (string.IsNullOrEmpty(AzureOpenAIApiKey))
            throw new InvalidOperationException(
                "AzureOpenAIApiKey required. Store in Key Vault: evoai-keyvault/LLMAPIKEY");
    }
}
```

---

## Common Patterns & Examples

### Pattern 1: Implementing a New Agent

When creating an agent (ExecutorAgent, HealerAgent), follow this structure:

```csharp
namespace EvoAITest.Agents.Agents;

/// <summary>
/// [Agent purpose and responsibility]
/// </summary>
public class ExecutorAgent : IExecutor
{
    private readonly IBrowserAgent _browserAgent;
    private readonly ILogger<ExecutorAgent> _logger;

    public ExecutorAgent(
        IBrowserAgent browserAgent,
        ILogger<ExecutorAgent> logger)
    {
        _browserAgent = browserAgent;
        _logger = logger;
    }

    public async Task<ExecutionResult> ExecuteAsync(
        List<ExecutionStep> plan,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting execution of plan with {StepCount} steps", plan.Count);

        try
        {
            // 1. Initialize browser
            await _browserAgent.InitializeAsync(cancellationToken);

            // 2. Execute steps
            var stepResults = new List<StepResult>();
            foreach (var step in plan.OrderBy(s => s.Order))
            {
                var result = await ExecuteStepAsync(step, cancellationToken);
                stepResults.Add(result);
                
                if (!result.Success) break; // Stop on failure
            }

            // 3. Build result
            return new ExecutionResult(/* ... */);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execution failed");
            throw;
        }
    }

    private async Task<StepResult> ExecuteStepAsync(
        ExecutionStep step,
        CancellationToken cancellationToken)
    {
        // Map step.Action to IBrowserAgent method
        // Implement retry logic
        // Capture errors and screenshots
    }
}
```

### Pattern 2: Implementing a New LLM Provider

```csharp
namespace EvoAITest.LLM.Providers;

public class CustomLLMProvider : ILLMProvider
{
    private readonly HttpClient _httpClient;
    private readonly EvoAITestCoreOptions _options;
    private readonly ILogger<CustomLLMProvider> _logger;

    public CustomLLMProvider(
        HttpClient httpClient,
        IOptions<EvoAITestCoreOptions> options,
        ILogger<CustomLLMProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LLMResponse> GenerateAsync(
        LLMRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Build request payload
        var payload = BuildRequestPayload(request);

        // 2. Call API
        var response = await _httpClient.PostAsJsonAsync(
            _options.CustomLLMEndpoint,
            payload,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        // 3. Parse response
        var result = await response.Content.ReadFromJsonAsync<CustomLLMResponse>(cancellationToken);

        // 4. Convert to standard format
        return MapToLLMResponse(result);
    }
}
```

### Pattern 3: Adding a New Browser Tool

```csharp
// In DefaultBrowserToolRegistry.cs
public static List<BrowserTool> GetAllTools()
{
    return new List<BrowserTool>
    {
        // Existing tools...
        
        new BrowserTool(
            Name: "scroll_to_element",
            Description: "Scroll the page until element is in viewport",
            Parameters: new Dictionary<string, ParameterDef>
            {
                ["selector"] = new ParameterDef(
                    Type: "string",
                    Required: true,
                    Description: "CSS selector of element to scroll to",
                    DefaultValue: null
                )
            }
        )
    };
}

// Then implement in DefaultToolExecutor.cs
private async Task<ToolExecutionResult> ExecuteToolAsync(
    string toolName,
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken)
{
    return toolName switch
    {
        "scroll_to_element" => await ScrollToElementAsync(parameters, cancellationToken),
        // Other tools...
        _ => throw new InvalidOperationException($"Unknown tool: {toolName}")
    };
}

private async Task<ToolExecutionResult> ScrollToElementAsync(
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken)
{
    var selector = parameters["selector"].ToString()!;
    
    // Use IBrowserAgent to perform action
    await _browserAgent.ScrollToElementAsync(selector, cancellationToken);
    
    return ToolExecutionResult.Success($"Scrolled to element: {selector}");
}
```

### Pattern 4: Database Entity with EF Core

```csharp
namespace EvoAITest.Core.Models;

[Table("AutomationTasks")]
[Index(nameof(UserId), nameof(Status))]
public class AutomationTask
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    [MaxLength(100)]
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(500)]
    public string Name { get; set; } = string.Empty;
    
    [Column(TypeName = "nvarchar(max)")]
    public string Plan { get; set; } = "[]"; // JSON serialized List<ExecutionStep>
    
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    
    // Navigation property
    public ICollection<ExecutionHistory> Executions { get; set; } = new List<ExecutionHistory>();
}
```

### Pattern 5: Minimal API Endpoint

```csharp
// In EvoAITest.ApiService/Program.cs

app.MapPost("/api/tasks", async (
    CreateTaskRequest request,
    IAutomationTaskRepository repository,
    ClaimsPrincipal user,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
    
    logger.LogInformation("Creating task {TaskName} for user {UserId}", request.Name, userId);
    
    var task = new AutomationTask
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = request.Name,
        Description = request.Description,
        NaturalLanguagePrompt = request.Prompt,
        Status = TaskStatus.Pending
    };
    
    await repository.CreateAsync(task, cancellationToken);
    
    return Results.Created($"/api/tasks/{task.Id}", task);
})
.RequireAuthorization()
.WithName("CreateTask")
.WithOpenApi();
```

---

## Testing Guidelines

### Unit Test Structure

```csharp
namespace EvoAITest.Tests.Agents;

public class PlannerAgentTests
{
    private readonly Mock<ILLMProvider> _mockLLMProvider;
    private readonly Mock<ILogger<PlannerAgent>> _mockLogger;
    private readonly PlannerAgent _plannerAgent;

    public PlannerAgentTests()
    {
        _mockLLMProvider = new Mock<ILLMProvider>();
        _mockLogger = new Mock<ILogger<PlannerAgent>>();
        _plannerAgent = new PlannerAgent(_mockLLMProvider.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task PlanAsync_WithValidPrompt_ReturnsExecutionPlan()
    {
        // Arrange
        var prompt = "Login to example.com with test@example.com";
        _mockLLMProvider
            .Setup(x => x.GenerateAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LLMResponse { Content = CreateMockPlanJson() });

        // Act
        var plan = await _plannerAgent.PlanAsync(prompt);

        // Assert
        plan.Should().NotBeEmpty();
        plan.Should().HaveCountGreaterThan(2);
        plan.First().Action.Should().Be("navigate");
    }
}
```

### Integration Test Structure

```csharp
public class ExecutionFlowIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ExecutionFlowIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real services with test doubles
                services.AddSingleton<ILLMProvider, MockLLMProvider>();
                services.AddScoped<IBrowserAgent, MockBrowserAgent>();
            });
        });
        
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ExecuteTask_EndToEnd_CreatesTaskAndExecutes()
    {
        // Arrange
        var request = new CreateTaskRequest
        {
            Name = "Test Login",
            Prompt = "Login to example.com"
        };

        // Act - Create task
        var createResponse = await _client.PostAsJsonAsync("/api/tasks", request);
        createResponse.EnsureSuccessStatusCode();
        var task = await createResponse.Content.ReadFromJsonAsync<AutomationTask>();

        // Act - Execute task
        var executeResponse = await _client.PostAsync($"/api/tasks/{task!.Id}/execute", null);
        executeResponse.EnsureSuccessStatusCode();
        var result = await executeResponse.Content.ReadFromJsonAsync<ExecutionResult>();

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ExecutionStatus.Success);
    }
}
```

---

## Azure Resources & Deployment

### Key Azure Resources

**Azure OpenAI:**
- Endpoint: `https://.cognitiveservices.azure.com`
- Deployment: `gpt-5`
- API Version: `2025-01-01-preview`

**Key Vault:**
- Name: `keyvault`
- URL: `https://keyvault.vault.azure.net/`
- Secret: `LLMAPIKEY` (Azure OpenAI API key)

**Container Registry:**
- Name: `evoaiacr` (or similar)
- Images: `evoaitest-api`, `evoaitest-web`

**SQL Database:**
- Managed via Aspire SQL resource
- Local: localdb during development
- Production: Azure SQL Database

### Deployment Commands

```bash
# Local development
dotnet run --project EvoAITest.AppHost

# Deploy to Azure Container Apps
azd up

# Build and push container images
az acr build --registry evoaiacr --image evoaitest-api:latest .

# View container logs
az containerapp logs show -g evoai-rg -n evoai-api --follow

# Update container environment variables
az containerapp update -n evoai-api -g evoai-rg \
  --set-env-vars EVOAITEST__CORE__LLMPROVIDER=AzureOpenAI
```

---

## Development Workflow

### Setting Up Local Development

1. **Install Prerequisites:**
   ```bash
   # .NET 10 SDK
   winget install Microsoft.DotNet.SDK.Preview
   
   # Ollama for local LLM
   winget install Ollama.Ollama
   
   # Docker Desktop for Aspire
   winget install Docker.DockerDesktop
   
   # Azure CLI (optional)
   winget install Microsoft.AzureCLI
   ```

2. **Pull Ollama Model:**
   ```bash
   ollama pull qwen2.5-7b
   ```

3. **Run Aspire AppHost:**
   ```bash
   cd EvoAITest.AppHost
   dotnet run
   ```
   
   Aspire dashboard: https://localhost:8080

4. **Run Tests:**
   ```bash
   dotnet test
   ```

### Daily Commit Pattern

Following the 16-week development plan:

```bash
# Example: Day 9 (Planner Agent)
git add EvoAITest.Agents/Agents/PlannerAgent.cs
git add EvoAITest.Tests/Agents/PlannerAgentTests.cs
git commit -m "feat(agents): implement PlannerAgent with GPT-5 integration

- Convert natural language to execution plans
- Use Azure OpenAI tool calling
- Add comprehensive unit tests with mocked LLM
- Support cancellation tokens for Aspire shutdown"

git push origin main
```

**Commit Message Format:**
- `feat(scope): description` - New feature
- `fix(scope): description` - Bug fix
- `test(scope): description` - Test additions
- `docs(scope): description` - Documentation
- `refactor(scope): description` - Code refactoring

### Branch Strategy

- `main` - Production-ready code
- `develop` - Integration branch (if using feature branches)
- `feature/day-X-description` - Feature branches for larger work

---

## Common Tasks & Solutions

### Task: Add a New Configuration Option

1. **Update Options Class:**
   ```csharp
   // EvoAITest.Core/Options/EvoAITestCoreOptions.cs
   public int MaxConcurrentExecutions { get; set; } = 5;
   ```

2. **Add Validation:**
   ```csharp
   public void ValidateConfiguration()
   {
       if (MaxConcurrentExecutions < 1)
           throw new InvalidOperationException("MaxConcurrentExecutions must be >= 1");
   }
   ```

3. **Update appsettings.json:**
   ```json
   {
     "EvoAITest": {
       "Core": {
         "MaxConcurrentExecutions": 5
       }
     }
   }
   ```

4. **Use in Code:**
   ```csharp
   var maxExecutions = _options.Value.MaxConcurrentExecutions;
   ```

### Task: Debug LLM Integration Issues

**Check Configuration:**
```bash
# Verify environment variables
dotnet user-secrets list --project EvoAITest.ApiService

# Test Ollama availability
curl http://localhost:11434/api/tags

# Check Azure OpenAI endpoint
curl -H "api-key: YOUR_KEY" \
  https://.cognitiveservices.azure.com/openai/deployments/gpt-5/chat/completions?api-version=2025-01-01-preview
```

**Enable Verbose Logging:**
```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "EvoAITest.LLM": "Debug",
      "EvoAITest.Agents": "Debug"
    }
  }
}
```

### Task: Add Migration for Database Change

```bash
# Add migration
dotnet ef migrations add AddExecutionHistoryTable --project EvoAITest.Core

# Review generated migration
# Edit if needed in Migrations/YYYYMMDD_AddExecutionHistoryTable.cs

# Apply migration locally
dotnet ef database update --project EvoAITest.Core

# Generate SQL script for production
dotnet ef migrations script --output migration.sql --project EvoAITest.Core
```

---

## Troubleshooting

### Issue: Aspire AppHost Won't Start

**Symptoms:** `dotnet run` fails with port conflicts or missing dependencies

**Solutions:**
```bash
# 1. Clean and rebuild
dotnet clean
dotnet build

# 2. Check Docker is running
docker ps

# 3. Kill conflicting processes
netstat -ano | findstr :8080
taskkill /PID <process_id> /F

# 4. Reset Aspire configuration
rm -rf ~/.aspire
```

### Issue: Browser Automation Fails in Container

**Symptoms:** Playwright throws "Executable not found" in Azure Container Apps

**Solution:** Ensure Dockerfile includes Playwright dependencies
```dockerfile
# Install Playwright browsers
RUN apt-get update && apt-get install -y \
    wget gnupg ca-certificates fonts-liberation \
    libnss3 libxss1 libasound2 libatk-bridge2.0-0

RUN pwsh -Command "Install-Playwright chromium"
```

### Issue: Key Vault Access Denied

**Symptoms:** "Access denied" when retrieving LLMAPIKEY secret

**Solutions:**
```bash
# 1. Check you're logged in
az account show

# 2. Grant yourself access
az keyvault set-policy --name evoai-keyvault \
  --upn your-email@domain.com \
  --secret-permissions get list

# 3. For Managed Identity in Container Apps
az containerapp identity assign -g evoai-rg -n evoai-api
# Then grant Key Vault access to the managed identity
```

---

## File Templates

### New Agent Template

```csharp
namespace EvoAITest.Agents.Agents;

/// <summary>
/// [Agent description and purpose]
/// </summary>
public class NewAgent : IAgent
{
    private readonly ILLMProvider _llmProvider;
    private readonly ILogger<NewAgent> _logger;

    public NewAgent(
        ILLMProvider llmProvider,
        ILogger<NewAgent> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentResult> ProcessAsync(
        AgentTask task,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing task {TaskId}", task.Id);

        try
        {
            // Implementation
            throw new NotImplementedException();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process task {TaskId}", task.Id);
            throw;
        }
    }
}
```

### New API Endpoint Template

```csharp
// In Program.cs
app.MapPost("/api/endpoint-name", async (
    RequestModel request,
    IDependency dependency,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("Endpoint called with {Parameter}", request.Parameter);

    try
    {
        var result = await dependency.ProcessAsync(request, cancellationToken);
        return Results.Ok(result);
    }
    catch (ValidationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
})
.RequireAuthorization()
.WithName("EndpointName")
.WithOpenApi()
.Produces<ResponseModel>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest);
```

---

## Resources & References

### Internal Documentation
- `README.md` - Project overview and getting started
- `IMPLEMENTATION_SUMMARY.md` - Current implementation status
- `Phase1-Phase2_DetailedActions.md` - Detailed development plan
- `EvoAITest.Days_9-20_Prompts.txt` - Claude prompts for Days 9-20
- `QUICK_REFERENCE.md` - Quick command reference

### External Links
- [.NET Aspire Documentation](https://learn.microsoft.com/en-us/dotnet/aspire/)
- [Playwright for .NET](https://playwright.dev/dotnet/)
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

### Azure Resources
- Azure Portal: https://portal.azure.com
- Key Vault: https://.vault.azure.net/
- Azure OpenAI Endpoint: https://.cognitiveservices.azure.com

---

## Quick Command Reference

```bash
# Development
dotnet run --project EvoAITest.AppHost              # Run Aspire locally
dotnet test                                          # Run all tests
dotnet build                                         # Build solution

# Database
dotnet ef migrations add MigrationName --project EvoAITest.Core
dotnet ef database update --project EvoAITest.Core
dotnet ef migrations script --output migration.sql

# Azure
azd up                                              # Deploy to Azure
azd down                                            # Tear down Azure resources
az containerapp logs show -g evoai-rg -n evoai-api --follow

# Ollama
ollama list                                         # List installed models
ollama pull qwen2.5-7b                             # Pull model
ollama run qwen2.5-7b                              # Test model

# Docker
docker build -t evoaitest-api .                    # Build image
docker run -p 8080:8080 evoaitest-api              # Run container
docker ps                                           # List running containers
```
