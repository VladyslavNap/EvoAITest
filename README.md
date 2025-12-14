# EvoAITest

**AI-Powered Browser Automation Framework with Azure OpenAI (GPT-5)**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Azure](https://img.shields.io/badge/Azure-OpenAI%20GPT--5-0078D4?logo=microsoft-azure)](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
[![Aspire](https://img.shields.io/badge/Aspire-Enabled-512BD4?logo=dotnet)](https://learn.microsoft.com/dotnet/aspire/)
[![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
[![Build Status](https://img.shields.io/github/actions/workflow/status/VladyslavNap/EvoAITest/build-and-test.yml?branch=main&label=build)](https://github.com/VladyslavNap/EvoAITest/actions)
[![Tests](https://img.shields.io/badge/tests-passing-brightgreen)](https://github.com/VladyslavNap/EvoAITest/actions)
[![Code Coverage](https://img.shields.io/badge/coverage-90%25-brightgreen)](https://github.com/VladyslavNap/EvoAITest/actions)

> 📚 **[Complete Documentation Index](DOCUMENTATION_INDEX.md)** | ⚡ **[Quick Start](docs/VisualRegressionQuickStart.md)** | 🗺️ **[Roadmap](VISUAL_REGRESSION_ROADMAP.md)** | 📖 **[User Guide](docs/VisualRegressionUserGuide.md)**

## Overview

EvoAITest is a modern, cloud-native browser automation framework that uses Azure OpenAI (GPT-5) to enable intelligent, natural language-driven web testing and automation with comprehensive **Visual Regression Testing** capabilities. Built on .NET 10 with Aspire orchestration, it combines enterprise-grade Azure AI with local development flexibility using Ollama.

### Key Features

- 🤖 **Azure OpenAI (GPT-5) Integration** - Production-ready AI-powered automation
- 🦙 **Local Ollama Support** - Offline development with open-source models
- 🔐 **Azure Key Vault** - Secure secret management with managed identity
- 💬 **Natural Language Commands** - Describe tasks in plain English
- 🌐 **Playwright Browser Agent** - Resilient automation with 25 built-in tools and accessibility-aware state capture
- 📊 **Aspire Observability** - Built-in OpenTelemetry metrics and traces
- 🔄 **Multi-Provider LLM** - Switch between Azure OpenAI, Ollama, or custom endpoints
- 📱 **Mobile Device Emulation** - Test responsive designs with 19 device presets
- 🌍 **Geolocation Testing** - GPS coordinate simulation with 6 preset locations
- 🔌 **Network Interception** - Mock APIs, block requests, and log network activity
- 📸 **Visual Regression Testing** - Automated screenshot comparison with AI-powered healing
- 🎨 **Blazor Web UI** - Modern, responsive interface

## Architecture

![EvoAITest architecture diagram](orah1borah1borah.png)

### Latest Update (Day 16)
- `EvoAITest.ApiService/Endpoints/ExecutionEndpoints.cs` exposes synchronous/background execution, healing retries, cancellation, and history/detail routes that orchestrate Planner → Executor → Healer.
- `Program.cs` now maps both task CRUD and execution endpoints, enables authentication/authorization scaffolding, and exposes `Program` internals for WebApplicationFactory testing.
- `EvoAITest.Tests/Integration/ApiIntegrationTests.cs` adds end-to-end coverage (task creation → execution → healing → history) using WebApplicationFactory + in-memory EF.
- `examples/LoginExample` is a runnable CLI sample that demonstrates natural-language login automation, tying together planning, execution, and reporting.
- `EvoAITest.LLM/Prompts` introduces the new prompt-builder toolkit (templates, injection protection, routing-aware system prompts) with 40+ dedicated unit tests and DI registration hooks.

## Project Structure

| Project | Description |
|---------|-------------|
| **EvoAITest.AppHost** | .NET Aspire orchestration and service discovery |
| **EvoAITest.ApiService** | REST API for automation tasks |
| **EvoAITest.Web** | Blazor WebAssembly frontend |
| **EvoAITest.Core** | Core abstractions, models, and browser automation |
| **EvoAITest.LLM** | LLM provider implementations (Azure OpenAI, Ollama) |
| **EvoAITest.Agents** | AI agent orchestration and planning |
| **EvoAITest.ServiceDefaults** | Shared Aspire configuration |
| **EvoAITest.Tests** | Unit and integration tests |

## Getting Started

### Prerequisites

#### Required
- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Azure Account** - [Free Account](https://azure.microsoft.com/free/)
- **Azure CLI** - [Install](https://aka.ms/azure-cli)
- **Docker** - [Download](https://www.docker.com/products/docker-desktop)

#### Optional (for local development)
- **Ollama** - [Download](https://ollama.ai) (local LLM)
- **Visual Studio 2025** or **VS Code**

### Quick Start (Azure OpenAI - Production)

#### 1. Clone the Repository
```bash
git clone https://github.com/VladyslavNap/EvoAITest.git
cd EvoAITest
```

#### 2. Azure Setup
```bash
# Login to Azure
az login

# Set your subscription
az account set --subscription "Your Subscription Name"

# Create or verify Key Vault
az keyvault create \
  --name evoai-keyvault \
  --resource-group evoaitest-rg \
  --location eastus

# Store your Azure OpenAI API key
az keyvault secret set \
  --vault-name evoai-keyvault \
  --name LLMAPIKEY \
  --value "YOUR_AZURE_OPENAI_API_KEY"
```

#### 3. Configure Environment Variables
```powershell
# PowerShell (Windows)
$env:AZURE_OPENAI_ENDPOINT = "https://youropenai.cognitiveservices.azure.com"
$env:EVOAITEST__CORE__LLMPROVIDER = "AzureOpenAI"
```

```bash
# Bash (Linux/macOS)
export AZURE_OPENAI_ENDPOINT="https://youropenai.cognitiveservices.azure.com"
export EVOAITEST__CORE__LLMPROVIDER="AzureOpenAI"
```

#### 4. Run Verification Script
```powershell
.\scripts\verify-day5.ps1
```

#### 5. Run the Application
```bash
cd EvoAITest.AppHost
dotnet run
```

Access the Aspire Dashboard at: **http://localhost:15888**

### Quick Start (Ollama - Local Development)

#### 1. Install and Start Ollama
```bash
# Install Ollama (https://ollama.ai)
# macOS/Linux: curl -fsSL https://ollama.ai/install.sh | sh
# Windows: Download installer from https://ollama.ai

# Start Ollama
ollama serve

# Pull recommended model
ollama pull qwen2.5-7b
```

#### 2. Configure for Local Development
```powershell
# PowerShell
$env:EVOAITEST__CORE__LLMPROVIDER = "Ollama"
$env:EVOAITEST__CORE__OLLAMAENDPOINT = "http://localhost:11434"
$env:EVOAITEST__CORE__OLLAMAMODEL = "qwen2.5-7b"
```

#### 3. Run the Application
```bash
cd EvoAITest.AppHost
dotnet run
```

## Database Setup

### Connection String

`AddEvoAITestCore` now registers `EvoAIDbContext` automatically when a connection string named `EvoAIDatabase` is present. Local development defaults to SQL Server LocalDB; production targets Azure SQL or any SQL Server-compatible host.

```json
{
  "ConnectionStrings": {
    "EvoAIDatabase": "Server=(localdb)\\mssqllocaldb;Database=EvoAITest;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

- **Development**: keep the LocalDB connection string above or point to Docker SQL (`Server=localhost,1433;Database=EvoAITest;User Id=sa;Password=Your_password123;Encrypt=False`).
- **Production**: supply the Azure SQL connection string (Managed Identity or SQL auth) and include retry options via Azure App Configuration if needed.
- **Apps**: ApiService and AppHost inherit the connection string automatically; no extra DI wiring is required beyond `builder.Services.AddEvoAITestCore(builder.Configuration);`.

> The EF Core data layer stores `AutomationTasks` plus `ExecutionHistory` (step results, screenshots, metadata). `dotnet ef` tooling now lives in the Core/ApiService csproj files, so you can run migrations from either project root.

### Database Provisioning & Migrations

- **Local (Aspire)**: `EvoAITest.AppHost` now orchestrates a SQL Server container and hands its connection string to ApiService automatically. Just run `dotnet run` from `EvoAITest.AppHost` and Aspire will stand up Redis + SQL + projects.
- **Development hot-reload**: `EvoAITest.ApiService/Program.cs` applies pending migrations automatically when `ASPNETCORE_ENVIRONMENT=Development`, so LocalDB/Aspire SQL stay in sync.
- **Manual migration workflow**:
  ```bash
  dotnet ef migrations add AddNewTable -p EvoAITest.Core -s EvoAITest.ApiService
  dotnet ef database update -p EvoAITest.Core -s EvoAITest.ApiService
  ```
- **Production**: run `dotnet ef database update` (or Azure SQL dacpac) during deployment. The checked-in `migration.sql` mirrors the initial schema for teams that prefer SQL scripts.

## Task Management API

The ApiService now exposes RESTful task endpoints under `/api/tasks`. Every route requires an authenticated user (falls back to a development identity if claims are missing) and includes OpenAPI metadata by default.

| Method | Route | Description | Response |
|--------|-------|-------------|----------|
| `POST` | `/api/tasks` | Create a new automation task | `201 Created` + `TaskResponse` |
| `GET` | `/api/tasks` | List tasks for the current user (optional `status=` filter) | `200 OK` + `TaskResponse[]` |
| `GET` | `/api/tasks/{id}` | Get task details | `200 OK`, `404 Not Found`, `403 Forbidden` |
| `PUT` | `/api/tasks/{id}` | Update task metadata/status | `200 OK`, `400 Bad Request`, `404 Not Found` |
| `DELETE` | `/api/tasks/{id}` | Delete a task (cascade execution history) | `204 No Content`, `404 Not Found` |
| `GET` | `/api/tasks/{id}/history` | Fetch execution history entries ordered by `StartedAt` | `200 OK` + `ExecutionHistoryResponse[]` |

### Sample Create Request

```bash
# Note: In production, include a valid Bearer token in the Authorization header.
# In development, requests without authentication fall back to "anonymous-user".
curl -X POST https://localhost:5001/api/tasks \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <your-token>" \
  -d '{
        "name": "Login journey",
        "description": "Exercise the full dashboard login",
        "naturalLanguagePrompt": "Open dashboard, log in as admin, capture KPI widgets"
      }'
```

Request/response contracts live in `EvoAITest.ApiService/Models/TaskModels.cs`, ensuring consistent casing, validation, and telemetry-friendly payloads.

## LLM Routing & Resilience

The LLM layer now supports intelligent routing, automatic fallback, and circuit breakers so production workloads can mix GPT-4 with cost-effective models safely.

- `EnableMultiModelRouting`: when `true`, `RoutingLLMProvider` sends planning tasks to GPT-4 while code/extraction prompts go to Qwen/Mistral.
- `EnableProviderFallback`: keeps requests flowing by falling back to Ollama when Azure OpenAI is rate-limited or unhealthy (circuit breaker thresholds configurable).
- `RoutingStrategy`: `"TaskBased"` (default) or `"CostOptimized"` to prioritize price/performance.
- `CircuitBreakerFailureThreshold` / `CircuitBreakerOpenDurationSeconds`: guard against cascading failures.

```json
{
  "EvoAITest": {
    "Core": {
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

### Prompt Builder Toolkit

`EvoAITest.LLM/Prompts` ships a modular `IPromptBuilder` implementation with:
- Template registry (`login-automation`, `planner`, `healer`, etc.) and variable substitution
- Versioned system instructions + reusable prompt components (context, tools, examples)
- Injection protection (detects "ignore previous instructions", special tokens, length limits)
- Build-time validation + sanitization with warning metadata

```csharp
var prompt = promptBuilder
    .CreatePrompt("browser-automation")
    .WithContext("Current URL: https://example.com")
    .WithUserInstruction("Click the login button")
    .WithVariables(new() { ["button"] = "#login" });

var buildResult = await promptBuilder.BuildAsync(prompt);
if (buildResult.Warnings.Count > 0)
{
    logger.LogWarning("Prompt warnings: {Warnings}", string.Join(", ", buildResult.Warnings));
}
```

See `EvoAITest.LLM/Prompts/README.md` for the full API reference, templates, and troubleshooting guidance.

## Configuration

### appsettings.Development.json (Local with Ollama)
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Ollama",
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "qwen2.5-7b",
      "BrowserTimeoutMs": 30000,
      "HeadlessMode": false,
      "MaxRetries": 3,
      "ScreenshotOutputPath": "C:\\temp\\screenshots",
      "LogLevel": "Debug"
    }
  }
}
```

### appsettings.Production.json (Azure OpenAI)
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "LLMModel": "gpt-5",
      "AzureOpenAIDeployment": "gpt-5",
      "AzureOpenAIApiVersion": "2025-01-01-preview",
      "BrowserTimeoutMs": 60000,
      "HeadlessMode": true,
      "MaxRetries": 5,
      "ScreenshotOutputPath": "/mnt/screenshots"
    }
  }
}
```

**Note:** `AzureOpenAIEndpoint` comes from `AZURE_OPENAI_ENDPOINT` environment variable.  
**Note:** `AzureOpenAIApiKey` comes from Azure Key Vault secret `LLMAPIKEY`.

## Browser Automation Tools

EvoAITest provides 25 pre-defined browser automation tools (14 core + 6 mobile + 5 network):

| Tool | Description | Parameters |
|------|-------------|------------|
| **navigate** | Navigate to a URL | url, wait_until |
| **click** | Click an element | selector, button, click_count, force |
| **type** | Type text into input | selector, text, delay_ms, clear_first |
| **clear_input** | Clear input field | selector |
| **extract_text** | Extract text from element | selector, all_matches, include_hidden |
| **extract_table** | Extract table data | selector, include_headers, format |
| **get_page_state** | Get page information | include_hidden, include_screenshots, include_console |
| **take_screenshot** | Capture screenshot | selector, full_page, quality |
| **wait_for_element** | Wait for element | selector, state, timeout_ms |
| **wait_for_url_change** | Wait for URL change | expected_url, url_pattern, timeout_ms |
| **select_option** | Select dropdown option | selector, value, label, index |
| **submit_form** | Submit a form | selector, wait_for_navigation, timeout_ms |
| **verify_element_exists** | Verify element presence | selector, expected_text, should_be_visible, timeout_ms |
| **visual_check** | Visual regression testing | checkpoint_name, type, selector, tolerance |
| **set_device_emulation** | Emulate mobile device | device_name, viewport_width, viewport_height, user_agent, device_scale_factor, has_touch, is_mobile |
| **set_geolocation** | Set GPS coordinates | preset, latitude, longitude, accuracy |
| **set_timezone** | Configure timezone | timezone_id |
| **set_locale** | Set browser language | locale |
| **grant_permissions** | Grant permissions | permissions (array) |
| **clear_permissions** | Revoke all permissions | (none) |
| **mock_response** | Mock HTTP responses | url_pattern, status, body, content_type, delay_ms, headers |
| **block_request** | Block network requests | url_pattern |
| **intercept_request** | Custom request interception | url_pattern, action |
| **get_network_logs** | Retrieve network activity | enable_logging |
| **clear_interceptions** | Clear network interceptions | clear_logs |

## Usage Examples

### Natural Language Automation
```csharp
var task = new AutomationTask
{
    Name = "Login to Dashboard",
    NaturalLanguagePrompt = "Navigate to example.com, enter username 'admin', enter password, click login, and verify dashboard is visible",
    UserId = "user123"
};

var result = await automationService.ExecuteAsync(task);
```

### Programmatic Automation
```csharp
var steps = new List<ExecutionStep>
{
    new(1, "navigate", "", "https://example.com", "Navigate to login page", "Page loads"),
    new(2, "type", "input#username", "admin", "Enter username", "Username entered"),
    new(3, "type", "input#password", "secret", "Enter password", "Password entered"),
    new(4, "click", "button#login", "", "Click login", "User logged in"),
    new(5, "verify_element_exists", "#dashboard", "", "Verify dashboard", "Dashboard visible")
};

task.SetPlan(steps);
var result = await executionService.ExecuteAsync(task);
```

## Development

### Build
```bash
dotnet build EvoAITest.sln --configuration Release
```

### Test
```bash
dotnet test EvoAITest.Tests --configuration Release
```

### Run Verification
```powershell
.\scripts\verify-day5.ps1
```

### Run with Hot Reload
```bash
cd EvoAITest.AppHost
dotnet watch run
```

## Testing

### Unit Tests (48+ tests)
```bash
dotnet test EvoAITest.Tests

# Run specific test class
dotnet test --filter "FullyQualifiedName~EvoAITestCoreOptionsTests"

# Prompt builder tests
dotnet test --filter "FullyQualifiedName~DefaultPromptBuilderTests"

# Run Azure OpenAI tests
dotnet test --filter "FullyQualifiedName~AzureOpenAI"

# Skip integration tests
dotnet test --filter "Category!=Integration"
```

### Integration Tests (9 tests)
```bash
# Install Playwright browsers first
cd EvoAITest.Tests/bin/Debug/net10.0
pwsh playwright.ps1 install chromium

# Run integration tests
dotnet test --filter "Category=Integration"
```

### API Integration Tests (WebApplicationFactory)
```bash
# Spin up the full ApiService stack in-memory
dotnet test --filter "FullyQualifiedName~ApiIntegrationTests"
```
These tests exercise the Task + Execution endpoints end-to-end using WebApplicationFactory, in-memory EF, and mocked planners/executors/healers.

### Test Coverage
- ? BrowserToolRegistry (13 tools)
- ? AutomationTask lifecycle
- ? Configuration validation (Azure OpenAI, Ollama, Local)
- ? PageState and models
- ? Tool call parsing
- ? Environment variable binding
- ? Key Vault security
- ? **DefaultToolExecutor (30+ unit tests)**
- ? **Tool Executor Integration (9 real browser integration tests)**
- ? **ExecutorAgent (19 orchestration-focused unit tests)**
- ? **HealerAgent (25 LLM-driven healing tests)**
- ? **EvoAIDbContext (12 EF Core data-layer tests)**
- ? **AutomationTaskRepository (30 EF-backed repository tests)**
- ? **API Integration (WebApplicationFactory)** - Task + execution flows
- ? **PromptBuilder (40+ template/injection tests)**

**All tests are fully automated in CI/CD - NO Azure credentials required for unit tests!**

### Continuous Integration

The project uses automated CI/CD pipelines for testing:

**GitHub Actions:**
- ? Automated on every push and pull request
- ? Unit tests run in ~2-3 seconds
- ? Integration tests run in ~40-60 seconds
- ? Code coverage reports automatically generated
- ?? [View CI/CD status](https://github.com/VladyslavNap/EvoAITest/actions)

**Azure DevOps:**
- ? Multi-stage pipeline (Build ? Test ? Publish)
- ? Parallel test execution
- ? Test result integration
- ? Artifact publishing for deployments

See [CI/CD Pipeline Documentation](CI_CD_PIPELINE_DOCUMENTATION.md) for detailed configuration.

## Deployment

### Azure Container Apps
```bash
# Deploy the application
az containerapp up \
  --name evoaitest-api \
  --resource-group evoaitest-rg \
  --location eastus \
  --source .

# Configure Key Vault reference
az containerapp secret set \
  --name evoaitest-api \
  --resource-group evoaitest-rg \
  --secrets llmapikey=keyvaultref:https://evoai-keyvault.vault.azure.net/secrets/LLMAPIKEY,identityref:/subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.ManagedIdentity/userAssignedIdentities/<identity>

# Set environment variables
az containerapp update \
  --name evoaitest-api \
  --resource-group evoaitest-rg \
  --set-env-vars \
    EVOAITEST__CORE__LLMPROVIDER=AzureOpenAI \
    AZURE_OPENAI_ENDPOINT=https://youropenai.cognitiveservices.azure.com
```

## Observability

### Aspire Dashboard
- **URL:** http://localhost:15888 (local development)
- **Metrics:** Real-time task execution, browser sessions, LLM calls
- **Traces:** End-to-end request tracing with OpenTelemetry
- **Logs:** Structured logging with context

### Custom Metrics
```csharp
// Task execution metrics
meter.CreateCounter<int>("evoaitest.tasks.executed");
meter.CreateHistogram<double>("evoaitest.task.duration");

// Browser session metrics
meter.CreateUpDownCounter<int>("evoaitest.browser.sessions");

// LLM usage metrics
meter.CreateCounter<int>("evoaitest.llm.tokens.input");
meter.CreateCounter<int>("evoaitest.llm.tokens.output");
meter.CreateHistogram<decimal>("evoaitest.llm.cost");
```

## Security Best Practices

### ? DO
- Store API keys in Azure Key Vault
- Use managed identity for authentication
- Use `DefaultAzureCredential()` in code
- Set environment variables for non-sensitive config
- Use User Secrets for local development
- Validate configuration on startup
- Run verification script before deployment

### ? DON'T
- Hardcode API keys in source code
- Commit secrets to Git
- Store API keys in environment variables (production)
- Use same API key for dev and production
- Skip configuration validation
- Expose sensitive data in logs

## Troubleshooting

### Common Issues

#### 1. Azure OpenAI endpoint not set
```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://youropenai.cognitiveservices.azure.com"
```

#### 2. Key Vault access denied
```bash
az role assignment create \
  --assignee <your-principal-id> \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.KeyVault/vaults/evoai-keyvault
```

#### 3. Ollama not running
```bash
ollama serve
ollama pull qwen2.5-7b
```

#### 4. Build failures
```bash
dotnet clean
dotnet restore
dotnet build
```

### Verification Script
Run the comprehensive verification script to diagnose issues:
```powershell
.\scripts\verify-day5.ps1
```

See [scripts/README-verify-day5.md](scripts/README-verify-day5.md) for detailed troubleshooting.

## Documentation

- [Day 5 Implementation Summary](IMPLEMENTATION_SUMMARY.md) - milestone highlights and doc map.
- [Day 5 Checklist](DAY5_CHECKLIST.md) - canonical list of delivered artefacts.
- [Phase 1 & 2 Action Plan](Phase1-Phase2_DetailedActions.md) - sequencing for upcoming work.
- [Quick Reference](QUICK_REFERENCE.md) - API and type cheatsheet.
- [Configuration Guide](EVOAITEST_CORE_CONFIGURATION_GUIDE.md) - Azure OpenAI, Ollama, and Key Vault setup.
- [Service Configuration Summary](EVOAITEST_CORE_SERVICE_CONFIG_SUMMARY.md) - DI helpers and Aspire integration notes.
- [Browser Tool Registry](BROWSER_TOOL_REGISTRY_SUMMARY.md) - 13 automation tools with parameter metadata.
- [Automation Models](AUTOMATION_TASK_MODELS_SUMMARY.md) - task and persistence models.
- [Unit Test Summary](EVOAITEST_CORE_UNIT_TESTS_SUMMARY.md) - suite structure and coverage pointers.
- [Verification Script Guide](scripts/README-verify-day5.md) - how to run verify-day5.ps1.
- [Verification Script Summary](VERIFY_DAY5_SCRIPT_SUMMARY.md) - shorthand for the checks performed.
- [ILLMProvider Update](ILLMPROVIDER_UPDATE_SUMMARY.md) - LLM abstraction changes.
- [Agent Implementation Summary](EvoAITest.Agents/IMPLEMENTATION_SUMMARY.md) - Planner (Day 9), Executor (Day 10), and Healer (Day 11) deliverables.
- [Executor Agent Guide](EvoAITest.Agents/Agents/ExecutorAgent_README.md) - plan execution, validation, and lifecycle controls.
- [Healer Agent Guide](EvoAITest.Agents/Agents/HealerAgent_README.md) - LLM diagnostics, healing strategies, and remediation workflows.
- [Data Persistence (EvoAITest.Core/README.md)](EvoAITest.Core/README.md#data-persistence-day-12) - EF Core DbContext, AutomationTask/ExecutionHistory entities, and SQL Server setup.
- [Repository Layer (EvoAITest.Core/README.md#repositories-day-14)](EvoAITest.Core/README.md#repositories-day-14) - AutomationTask repository API, DI registration, and query examples.
- [Task API Endpoints](EvoAITest.ApiService/Endpoints/TaskEndpoints.cs) - Minimal API routes, response codes, and inline OpenAPI metadata.
- [Execution API Guide](EvoAITest.ApiService/Endpoints/ExecutionEndpoints_README.md) - Planner/Executor/Healer orchestration routes, background status polling, and sample payloads.
- [Login Automation Example](examples/LoginExample/README.md) - CLI sample showing natural-language planning → execution → reporting.
- [Planner Chain-of-Thought Upgrade](EvoAITest.Agents/CHAIN_OF_THOUGHT_UPGRADE.md) - reasoning metadata, visualization formats, and storage guidance.
- [Prompt Builder Summary](PROMPT_BUILDER_SUMMARY.md) - template system, injection protection, and testing notes.
- [Prompt Builder Guide](EvoAITest.LLM/Prompts/README.md) - Templates, versioning, injection protection, and DI usage.
- **[Tool Executor Tests Summary](DEFAULT_TOOL_EXECUTOR_TESTS_SUMMARY.md)** - 30+ unit tests for Tool Executor.
- **[Tool Executor Integration Tests](TOOL_EXECUTOR_INTEGRATION_TESTS_SUMMARY.md)** - 9 real browser integration tests.
- **[CI/CD Pipeline Documentation](CI_CD_PIPELINE_DOCUMENTATION.md)** - Automated testing and deployment pipelines.

## Technology Stack

### Core Technologies
- **.NET 10** - Latest .NET framework
- **C# 14** - Latest language features
- **Blazor WebAssembly** - Modern web UI
- **ASP.NET Core** - High-performance web APIs

### Azure Services
- **Azure OpenAI (GPT-5)** - AI-powered automation
- **Azure Key Vault** - Secure secret management
- **Azure Container Apps** - Serverless containers
- **Azure Monitor** - Application insights

### Development Tools
- **.NET Aspire** - Cloud-native orchestration
- **OpenTelemetry** - Distributed tracing
- **Playwright** - Browser automation
- **xUnit + FluentAssertions** - Testing

### Local Development
- **Ollama** - Local LLM (qwen2.5-7b, llama2, mistral)
- **Docker** - Container development
- **PowerShell 7** - Cross-platform scripting

## Performance

### Benchmarks (Local - Ollama)
- Task planning: ~2-5 seconds
- Browser action: ~500ms-2s per step
- Screenshot capture: ~200ms
- Page state extraction: ~1s

### Benchmarks (Azure OpenAI GPT-5)
- Task planning: ~1-3 seconds
- API latency: ~200-500ms
- Token processing: 1000-2000 tokens/sec

### Resource Usage
- Memory: ~200MB per browser session
- CPU: ~10% per active session
- Network: ~1-2MB per task (LLM calls)

## Roadmap

### ✅ Phase 1: Core Framework (COMPLETE)
- [x] .NET 10 + Aspire project structure
- [x] Azure OpenAI (GPT-5) integration
- [x] Azure Key Vault integration
- [x] Ollama local development support
- [x] 13 browser automation tools
- [x] Configuration system
- [x] Unit tests (48+)
- [x] Verification script

### ✅ Phase 2: Enhanced Automation (COMPLETE)
- [x] **Visual Regression Testing** - Complete implementation
  - [x] Visual Comparison Engine (pixel-by-pixel + SSIM)
  - [x] 4 checkpoint types (FullPage, Viewport, Element, Region)
  - [x] Baseline management and approval workflow
  - [x] Diff image generation with region highlighting
  - [x] File storage service with structured organization
  - [x] Database persistence (2 new tables, 10 indexes)
  - [x] Repository extensions (8 new methods)
- [x] **Executor Integration**
  - [x] Visual check tool integration
  - [x] Browser screenshot methods (4 types)
  - [x] Tool execution context enhancement
- [x] **AI-Powered Healing**
  - [x] Visual regression failure analysis
  - [x] 4 healing strategies (tolerance, ignore regions, stability, manual approval)
  - [x] LLM-powered diagnostic prompts
- [x] **REST API**
  - [x] Visual regression endpoints
  - [x] Baseline approval workflow
  - [x] Comparison history retrieval
- [x] **Blazor Web UI**
  - [x] Visual regression viewer component
  - [x] Side-by-side comparison display
  - [x] Tolerance adjustment dialog
  - [x] Baseline approval dialog
  - [x] Difference region overlay
- [x] **Comprehensive Testing**
  - [x] Unit tests for comparison engine
  - [x] Integration tests with real browser
  - [x] End-to-end workflow tests
- [x] **Documentation**
  - [x] User guide (6,500 lines)
  - [x] API documentation (4,500 lines)
  - [x] Development guide (7,000 lines)
  - [x] Troubleshooting guide (3,500 lines)
  - [x] Quick start guide (1,000 lines)
- [x] **Mobile Device Emulation** - Complete implementation
  - [x] 19 device presets (iPhone, Android, tablets, desktop)
  - [x] Custom device profiles with viewport, user agent, touch support
  - [x] Geolocation API with 6 preset locations
  - [x] Locale and timezone configuration
  - [x] Browser permissions management
  - [x] 6 mobile tools integrated into tool registry
  - [x] Natural language automation support
- [x] **Network Interception and Mocking** - Complete implementation
  - [x] HTTP request/response interception
  - [x] Request blocking by URL patterns
  - [x] Response mocking with custom status/body/headers
  - [x] Network activity logging and tracking
  - [x] Latency simulation (delay support)
  - [x] 5 network tools integrated into tool registry
  - [x] Natural language automation support
- [x] Playwright browser implementation
- [x] Multi-browser support (Chrome, Firefox, Edge via Playwright)

**Phase 2 Statistics:**
- **Visual Regression:**
  - Production Code: 5,045 lines
  - Test Code: 1,150 lines
  - Documentation: 24,000 lines
  - Development Time: ~60 hours (66% faster than estimated!)
- **Mobile Emulation:**
  - Production Code: 650 lines (models + implementation + tools)
  - Documentation: 2,000 lines (3 completion docs)
  - Development Time: ~4 hours (60% faster than estimated!)
- **Network Interception:**
  - Production Code: 650 lines (models + interceptor + tools)
  - Documentation: 2,000 lines (progress + completion docs)
  - Development Time: ~2.5 hours (75% faster than estimated!)
- **Phase 2 Total:** 35,145 lines in ~66.5 hours
- **Build Status:** ✅ Successful (0 errors, 0 warnings)

### Phase 3: AI Enhancements (Future)
- [ ] Self-healing tests (auto-fix selector changes)
- [ ] Visual element detection (screenshot analysis)
- [ ] Smart waiting strategies
- [ ] Error recovery and retry logic
- [ ] Test generation from recordings

### Phase 4: Enterprise Features (Future)
- [ ] Role-based access control
- [ ] Audit logging
- [ ] Test scheduling and orchestration
- [ ] Parallel execution
- [ ] Results dashboard and reporting

### Phase 5: CI/CD Integration (Next Priority)
- [ ] GitHub Actions workflow for visual regression tests
- [ ] Azure DevOps pipeline integration
- [ ] Automated baseline management in CI
- [ ] Container registry setup
- [ ] Deployment automation

## Visual Regression Testing

### Overview

EvoAITest includes a **production-ready visual regression testing system** that automatically detects UI changes by comparing screenshots. This feature is powered by AI for intelligent failure diagnosis and healing.

### Key Features

✅ **4 Checkpoint Types**
- **FullPage** - Capture entire scrollable page
- **Viewport** - Capture visible area only
- **Element** - Capture specific element by selector
- **Region** - Capture rectangular region by coordinates

✅ **Intelligent Comparison**
- Pixel-by-pixel comparison with configurable tolerance
- SSIM (Structural Similarity Index) calculation
- Anti-aliasing detection
- Difference region identification with flood-fill algorithm

✅ **Baseline Management**
- Automatic baseline creation on first run
- Approval workflow with audit trail
- Branch-specific baselines (Git integration)
- Retention policy for cleanup

✅ **AI-Powered Healing**
- LLM analyzes visual regression failures
- Suggests 4 healing strategies:
  - Adjust tolerance for minor rendering differences
  - Add ignore regions for dynamic content
  - Wait for stability (animations/loading)
  - Flag for manual approval (design changes)

✅ **Enterprise-Grade UI**
- Interactive visual regression viewer
- Side-by-side image comparison
- Diff overlay with color-coded regions
- Live tolerance adjustment with pass/fail preview
- One-click baseline approval

✅ **REST API**
- Complete CRUD operations for baselines
- Comparison history with pagination
- Healing strategy recommendations
- Image serving endpoints

### Quick Start

```csharp
// Define a visual checkpoint in your automation task
var task = new AutomationTask
{
    Name = "Homepage Visual Test",
    VisualCheckpoints = new List<VisualCheckpoint>
    {
        new()
        {
            Name = "HomePage_Header",
            Type = CheckpointType.Element,
            Selector = "header.main-header",
            Tolerance = 0.01, // 1% difference allowed
            Environment = "production",
            Browser = "chromium",
            Viewport = "1920x1080"
        }
    }
};

// Execute task - visual checks run automatically
var result = await executor.ExecuteAsync(task);

// Check for visual failures
if (result.VisualComparisons.Any(v => !v.Passed))
{
    // AI-powered healing
    var strategies = await healer.HealVisualRegressionAsync(
        result.VisualComparisons.Where(v => !v.Passed).ToList(),
        context);
    
    // Apply strategies and retry
}
```

### Documentation

- 📖 **[Visual Regression User Guide](docs/VisualRegressionUserGuide.md)** - Complete usage guide
- 🔧 **[Visual Regression API](docs/VisualRegressionAPI.md)** - REST API reference
- 👨‍💻 **[Development Guide](docs/VisualRegressionDevelopment.md)** - Architecture and extension
- 🚀 **[Quick Start](docs/VisualRegressionQuickStart.md)** - Get started in 5 minutes
- 🔍 **[Troubleshooting](docs/Troubleshooting.md)** - Common issues and solutions

### Performance

- **Comparison Speed:** <3 seconds for 1920x1080 images
- **Storage:** <5MB per checkpoint
- **API Response:** <500ms for baseline retrieval
- **Test Coverage:** >90%

### Architecture

```
Visual Regression System Architecture:

┌─────────────────────────────────────────────────────────────┐
│                     Blazor Web UI                            │
│  VisualRegressionViewer │ Dialogs │ Components              │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                     REST API Layer                           │
│  Visual Endpoints │ Baseline Approval │ History             │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  Visual Comparison Service                   │
│  Orchestration │ Workflow │ Baseline Management             │
└─────────────────────────────────────────────────────────────┘
                            │
            ┌───────────────┼───────────────┐
            ↓               ↓               ↓
┌──────────────────┐ ┌─────────────┐ ┌────────────┐
│ Comparison Engine│ │   Storage   │ │ Repository │
│ Pixel + SSIM     │ │   Service   │ │   Layer    │
│ Diff Generation  │ │  File I/O   │ │  Database  │
└──────────────────┘ └─────────────┘ └────────────┘
            │
            ↓
┌─────────────────────────────────────────────────────────────┐
│                     Healer Agent (AI)                        │
│  Failure Analysis │ Strategy Generation │ LLM Integration   │
└─────────────────────────────────────────────────────────────┘

```

## Network Interception and Mocking

### Overview

EvoAITest includes a **production-ready network interception system** that allows you to mock HTTP responses, block requests, and log network activity. This feature is essential for testing APIs, simulating error states, and controlling network conditions.

### Key Features

✅ **Request Interception**
- Custom handler functions for advanced scenarios
- Modify requests on-the-fly
- Pass-through logging without modification

✅ **Response Mocking**
- Custom HTTP status codes (200, 404, 500, etc.)
- Custom response bodies (JSON, HTML, text)
- Custom headers and content types
- Latency simulation with configurable delays

✅ **Request Blocking**
- Block by URL patterns (glob syntax)
- Block ads, trackers, analytics
- Block images for faster tests
- Zero network activity for blocked requests

✅ **Network Logging**
- Capture all HTTP requests and responses
- Track timing and duration
- Identify blocked and mocked requests
- Thread-safe logging with ConcurrentBag

✅ **Natural Language Integration**
- 5 network tools for LLM automation
- Seamless integration with existing tools
- No code required for common scenarios

### Quick Start

```csharp
// Get network interceptor
var interceptor = browserAgent.GetNetworkInterceptor();

// Mock API response
await interceptor.MockResponseAsync("**/api/users", new MockResponse
{
    Status = 200,
    Body = "{\"users\": [{\"id\": 1, \"name\": \"John\"}]}",
    ContentType = "application/json",
    DelayMs = 100 // Simulate 100ms latency
});

// Block resources
await interceptor.BlockRequestAsync("**/*.{jpg,png,gif}");

// Enable logging
await interceptor.SetNetworkLoggingAsync(true);

// Navigate and test
await browserAgent.NavigateAsync("https://example.com");

// Get network logs
var logs = await interceptor.GetNetworkLogsAsync();
```

### Natural Language Example

```csharp
var task = new AutomationTask
{
    Name = "Test API Error Handling",
    NaturalLanguagePrompt = @"
        1. Mock the /api/login endpoint to return 500 error
        2. Navigate to https://example.com/login
        3. Enter credentials and submit
        4. Verify error message is displayed
        5. Get network logs to confirm 500 status
    "
};

var result = await executor.ExecuteAsync(task);
```

### Available Tools

| Tool | Description |
|------|-------------|
| **mock_response** | Mock HTTP responses with custom status, body, headers, and delay |
| **block_request** | Block requests matching URL patterns (ads, trackers, images) |
| **intercept_request** | Set up custom request interception handlers |
| **get_network_logs** | Retrieve all captured network activity |
| **clear_interceptions** | Clear all active mocks and blocks |

### Use Cases

**API Testing**
- Mock backend responses for frontend testing
- Simulate error states (404, 500, timeout)
- Test retry logic and error handling

**Performance Testing**
- Block images and stylesheets for faster tests
- Simulate network latency with delays
- Measure application behavior under load

**Offline Testing**
- Block all external requests
- Mock only critical APIs
- Test offline mode and service workers

**Security Testing**
- Block tracking and analytics
- Test CORS handling
- Validate request headers

### Documentation

- 📖 **[Network Interception Complete](NETWORK_INTERCEPTION_COMPLETE.md)** - Full implementation guide
- 📋 **[Network Interception Progress](NETWORK_INTERCEPTION_PROGRESS.md)** - Development history

### Performance

- **Interception Overhead:** <10ms per request
- **Mock Response Time:** Configurable (0ms to any delay)
- **Logging:** Thread-safe with ConcurrentBag
- **Memory Usage:** ~1KB per logged request

### Architecture

```
Network Interception Architecture:

┌─────────────────────────────────────────────────────────────┐
│                  Browser Automation                          │
│  Natural Language Prompt → Tool Calls                        │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  Tool Executor                               │
│  mock_response │ block_request │ get_network_logs           │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│             PlaywrightBrowserAgent                           │
│  GetNetworkInterceptor() → Lazy Initialization               │
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│          PlaywrightNetworkInterceptor                        │
│  InterceptRequestAsync │ BlockRequestAsync │ MockResponseAsync│
└─────────────────────────────────────────────────────────────┘
                            │
                            ↓
┌─────────────────────────────────────────────────────────────┐
│              Microsoft.Playwright.IPage                      │
│  RouteAsync() │ UnrouteAsync() │ Network Events             │
└─────────────────────────────────────────────────────────────┘
