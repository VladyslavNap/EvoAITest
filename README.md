# EvoAITest

**AI-Powered Browser Automation Framework with Test Recording**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Azure](https://img.shields.io/badge/Azure-OpenAI%20GPT--5-0078D4?logo=microsoft-azure)](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
[![Aspire](https://img.shields.io/badge/Aspire-Enabled-512BD4?logo=dotnet)](https://learn.microsoft.com/dotnet/aspire/)
[![Blazor](https://img.shields.io/badge/Blazor-Interactive-512BD4?logo=blazor)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)

> 🎬 **[Test Recording](docs/RECORDING_QUICK_START.md)** | 📸 **[Visual Regression](docs/VisualRegressionQuickStart.md)** | 📚 **[Complete Docs](DOCUMENTATION_INDEX.md)**

---

## Overview

EvoAITest is a modern, cloud-native browser automation framework that uses Azure OpenAI to enable intelligent, natural language-driven web testing and automation. Built on .NET 10 with Aspire orchestration, it features **AI-powered test generation from recordings**, **visual regression testing**, and **self-healing capabilities**.

---

## 🚀 Quick Start

### Prerequisites

- .NET 10 SDK
- Azure OpenAI or Ollama (for AI features)
- SQL Server (for recording persistence)

### Get Started in 3 Steps

1. **Clone and Build**
   ```bash
   git clone https://github.com/VladyslavNap/EvoAITest.git
   cd EvoAITest
   dotnet build
   ```

2. **Configure Azure OpenAI** (or use Ollama for local development)
   ```json
   {
     "EvoAITest": {
       "Core": {
         "LLMProvider": "AzureOpenAI",
         "AzureOpenAIEndpoint": "your-endpoint",
         "AzureOpenAIDeployment": "gpt-4"
       }
     }
   }
   ```

3. **Run the Application**
   ```bash
   dotnet run --project EvoAITest.AppHost
   ```

**📖 Detailed guides:** [Test Recording Quick Start](docs/RECORDING_QUICK_START.md) | [Visual Regression Quick Start](docs/VisualRegressionQuickStart.md)

---

## ✨ Key Features

### 🎬 Test Recording & Generation

Record browser interactions and automatically generate production-ready test code with AI analysis.

**What it does:**
- Records user interactions in real-time (15 action types)
- AI detects intent with 90%+ accuracy (15 intent types)
- Generates test code for xUnit, NUnit, or MSTest
- Auto-generates 16 types of smart assertions
- Optional Page Object Model generation

**How it works:**

```
Start Recording → Interact with App → Stop Recording
        ↓
AI Analyzes → Generate Test Code → Export & Use
```

**Example output:**

```csharp
[Fact]
public async Task LoginFlowTest()
{
    // Arrange
    await _page!.GotoAsync("https://example.com/login");

    // Act
    await _page!.Locator("#username").FillAsync("user@example.com");
    await _page!.Locator("#password").FillAsync("password123");
    await _page!.Locator("button[type='submit']").ClickAsync();

    // Assert
    await Expect(_page!.Locator(".dashboard")).ToBeVisibleAsync();
}
```

**📖 Learn more:** [Recording Feature Guide](docs/RECORDING_FEATURE.md) | [API Reference](docs/API_REFERENCE.md) | [Architecture](docs/ARCHITECTURE.md)

---

### 🤖 AI-Powered Automation

Intelligent browser automation powered by LLMs.

- **Azure OpenAI Integration** - Production-ready with GPT-4
- **Local Ollama Support** - Offline development with open-source models
- **Natural Language Commands** - Describe tasks in plain English
- **Intelligent Routing** - Automatic model selection based on task type
- **Circuit Breaker** - Automatic failover to backup providers
- **Cost Optimization** - Smart routing reduces costs by 40-60%

**📖 Learn more:** [LLM Integration Guide](docs/LLM_INTEGRATION_GUIDE.md)

---

### 🌐 Browser Automation

Playwright-based browser automation with AI-powered agents.

- **25 Built-in Tools** - Click, type, navigate, screenshot, and more
- **Self-Healing** - Automatic recovery from failures
- **Mobile Emulation** - Test responsive designs (19 device presets)
- **Network Interception** - Mock APIs and monitor traffic
- **Geolocation Testing** - Simulate GPS coordinates

**📖 Learn more:** [Agents Library](EvoAITest.Agents/README.md) | [Core Library](EvoAITest.Core/README.md)

---

### 📸 Visual Regression Testing

Automated screenshot comparison with AI-powered healing.

- **Automated Screenshot Capture** - Baseline and comparison images
- **Pixel-Perfect Comparison** - Detect visual differences
- **AI-Powered Healing** - Self-correcting visual tests
- **Diff Visualization** - Highlight changes and anomalies

**📖 Learn more:** [Visual Regression Quick Start](docs/VisualRegressionQuickStart.md) | [User Guide](docs/VisualRegressionUserGuide.md)

---

### 🔐 Enterprise Ready

Production-ready features for enterprise deployments.

- **Azure Key Vault** - Secure secret management with managed identity
- **OpenTelemetry** - Built-in metrics, traces, and logging
- **.NET Aspire** - Cloud-native orchestration and observability
- **.NET 10** - Latest runtime and framework features
- **SQL Server** - Persistent storage for recordings and history

**📖 Learn more:** [Key Vault Setup](docs/KEY_VAULT_SETUP.md) | [Configuration](docs/LLM_ROUTING_CONFIGURATION.md)

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [**Documentation Index**](DOCUMENTATION_INDEX.md) | Central hub for all documentation |
| [Test Recording Quick Start](docs/RECORDING_QUICK_START.md) | Get started with test recording in 5 minutes |
| [Visual Regression Quick Start](docs/VisualRegressionQuickStart.md) | Set up visual testing in 10 minutes |
| [LLM Integration Guide](docs/LLM_INTEGRATION_GUIDE.md) | Complete guide to LLM integration |
| [API Reference](docs/API_REFERENCE.md) | REST API documentation (13 endpoints) |
| [Architecture](docs/ARCHITECTURE.md) | Technical architecture details |

**📖 [Browse all documentation](DOCUMENTATION_INDEX.md)**

---

## 🏗️ Architecture

```
┌─────────────────────────────────────────────┐
│           Blazor Web UI                     │
│     (Recording, Monitoring, Control)        │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────┴──────────────────────────┐
│            REST API Service                 │
│    (Recording, Execution, Visual Testing)   │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────┴──────────────────────────┐
│          AI Agents & LLM Layer              │
│  (Planner, Executor, Healer, Test Gen)      │
└──────────────────┬──────────────────────────┘
                   │
┌──────────────────┴──────────────────────────┐
│         Browser Automation Core             │
│      (Playwright, Recording, Analysis)      │
└─────────────────────────────────────────────┘
```

**Projects:**
- **EvoAITest.Core** - Core models, services, database
- **EvoAITest.Agents** - AI agent orchestration
- **EvoAITest.LLM** - LLM provider abstraction
- **EvoAITest.Web** - Blazor UI components
- **EvoAITest.ApiService** - REST API endpoints
- **EvoAITest.AppHost** - Aspire orchestration

---

## 🎯 Use Cases

### For QA Engineers
- Record manual test flows and generate automated tests
- Set up visual regression testing for UI changes
- Monitor test execution with built-in observability

### For Developers
- Use natural language to automate browser tasks
- Integrate AI-powered automation into CI/CD pipelines
- Leverage self-healing capabilities to reduce maintenance

### For DevOps
- Deploy with Azure Aspire for cloud-native orchestration
- Secure secrets with Azure Key Vault
- Monitor with OpenTelemetry metrics and traces

---

## 🛠️ Technology Stack

- **.NET 10** - Latest C# and runtime features
- **Blazor** - Interactive web UI
- **Playwright** - Cross-browser automation
- **Azure OpenAI** - GPT-4 for AI capabilities
- **Ollama** - Local open-source models
- **Entity Framework Core** - Database access
- **SQL Server** - Data persistence
- **Azure Aspire** - Cloud-native orchestration
- **OpenTelemetry** - Observability

---

## 📈 Status

- **Build:** Passing ✅
- **Tests:** Passing ✅
- **Coverage:** 90%+ ✅
- **Version:** 1.0.0 (Recording Feature)
- **Last Updated:** January 2026

---

## 🤝 Contributing

Contributions are welcome! Please see our [Documentation Index](DOCUMENTATION_INDEX.md) for guides on:
- Project structure
- Development setup
- Architecture patterns
- Testing strategies

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🔗 Links

- **GitHub Repository:** [VladyslavNap/EvoAITest](https://github.com/VladyslavNap/EvoAITest)
- **Documentation:** [Complete Documentation Index](DOCUMENTATION_INDEX.md)
- **Issues:** [Report Issues](https://github.com/VladyslavNap/EvoAITest/issues)

---

**Built with ❤️ using .NET 10, Blazor, and Azure OpenAI**
