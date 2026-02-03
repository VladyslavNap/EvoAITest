# EvoAITest Documentation Index

**Version:** 1.1  
**Last Updated:** January 2025

> 📚 Central hub for all EvoAITest documentation and guides

---

## 🚀 Quick Start Guides

Start here if you're new to EvoAITest:

| Guide | Description | Time |
|-------|-------------|------|
| [**Main README**](README.md) | Project overview, architecture, and setup | 10 min |
| [**Test Recording Quick Start**](docs/RECORDING_QUICK_START.md) | Record browser interactions and generate test code | 5 min |
| [**Visual Regression Quick Start**](docs/VisualRegressionQuickStart.md) | Set up visual regression testing | 10 min |
| [**Dashboard Analytics Guide**](docs/DASHBOARD_ANALYTICS.md) | Real-time monitoring and analytics | 15 min |
| [**Login Example**](examples/LoginExample/README.md) | Complete working example with natural language automation | 15 min |

---

## ✨ Core Features

### 🎬 Test Recording & Generation

AI-powered test generation from recorded browser interactions:

| Document | Description |
|----------|-------------|
| [**Feature Guide**](docs/RECORDING_FEATURE.md) | Complete feature documentation, architecture, and usage |
| [**Quick Start**](docs/RECORDING_QUICK_START.md) | 5-minute setup and first recording |
| [**API Reference**](docs/API_REFERENCE.md) | All 24+ REST endpoints documented |
| [**Architecture**](docs/ARCHITECTURE.md) | Technical implementation details |
| [**Changelog**](docs/RECORDING_CHANGELOG.md) | Release notes and version history |

**Key Capabilities:**
- 🎥 Record 15 action types (Click, Input, Navigation, etc.)
- 🤖 AI intent detection with 90%+ accuracy
- 🧪 Generate tests for xUnit, NUnit, MSTest
- ✅ 16 assertion types auto-generated
- 📦 Optional Page Object Model generation

---

### 📊 Dashboard Analytics **NEW!**

Real-time monitoring and performance analytics:

| Document | Description |
|----------|-------------|
| [**Dashboard Analytics Guide**](docs/DASHBOARD_ANALYTICS.md) | Complete feature guide with examples |
| [**API Reference**](docs/API_REFERENCE.md) | 11 new analytics endpoints |
| [**Changelog**](CHANGELOG.md) | Version 1.1.0 release notes |

**Key Capabilities:**
- 🏃 Real-time execution tracking with progress bars
- 📈 Success rate trends (hourly/daily)
- ⚡ Performance metrics and duration analysis
- 🔧 Self-healing analytics
- 💚 System health monitoring
- 📋 Top executed/failing/slowest tasks
- 🔄 SignalR live updates

**Dashboard Views:**
- `/execution-dashboard` - Real-time execution monitoring
- `/analytics/dashboard` - Test analytics and flaky tests

---

### 🤖 LLM Integration

Intelligent multi-model routing and AI provider management:

| Document | Description |
|----------|-------------|
| [**LLM Integration Guide**](docs/LLM_INTEGRATION_GUIDE.md) | **Complete guide with examples (START HERE)** |
| [LLM Library README](EvoAITest.LLM/README.md) | Library overview and quick start |
| [Configuration Guide](docs/LLM_ROUTING_CONFIGURATION.md) | Setup guide for routing and Key Vault |
| [Architecture Deep Dive](docs/LLM_ROUTING_ARCHITECTURE.md) | Technical design and component details |
| [API Design Reference](docs/LLM_ROUTING_API_DESIGN.md) | Interfaces, models, and usage patterns |
| [Feature Specification](docs/LLM_ROUTING_SPECIFICATION.md) | Original feature specification |

**Alternative Guides:**
- [LLM Routing Complete Guide](docs/LLM_ROUTING_COMPLETE_GUIDE.md) - Alternative comprehensive guide

**Key Capabilities:**
- 🎯 Intelligent task-based routing
- 🔄 Circuit breaker with automatic failover
- ☁️ Azure OpenAI + Ollama support
- 🔐 Azure Key Vault integration
- 💰 Cost optimization (40-60% reduction)

---

### 📸 Visual Regression Testing

Automated screenshot comparison with AI-powered healing:

| Document | Description |
|----------|-------------|
| [**Quick Start Guide**](docs/VisualRegressionQuickStart.md) | Get started in 10 minutes |
| [**User Guide**](docs/VisualRegressionUserGuide.md) | Complete feature documentation |

**Key Capabilities:**
- 📸 Automated screenshot capture
- 🔍 Pixel-perfect comparison
- 🤖 AI-powered healing
- 🎨 Diff visualization

---

### 🌐 Browser Automation

Playwright-based automation agents:

| Document | Description |
|----------|-------------|
| [**Agents Library README**](EvoAITest.Agents/README.md) | Overview of agent orchestration |
| [**Core Library README**](EvoAITest.Core/README.md) | Core services and abstractions |
| [**Prompts README**](EvoAITest.LLM/Prompts/README.md) | Prompt builder toolkit |

**Key Capabilities:**
- 🛠️ 25 built-in browser tools
- 💬 Natural language commands
- ? Self-healing capabilities
- ? Mobile device emulation
- ? Network interception

---

## ?? Configuration & Setup

### Azure Key Vault Setup

| Document | Description |
|----------|-------------|
| [**Key Vault Setup Guide**](docs/KEY_VAULT_SETUP.md) | Step-by-step Azure Key Vault configuration |

### Environment Configuration

Configuration files by environment:

```
appsettings.json              # Base configuration
appsettings.Development.json  # Local development
appsettings.Staging.json      # Staging environment
appsettings.Production.json   # Production settings
```

---

## ??? Architecture & Development

### Project Structure

| Project | Purpose | Documentation |
|---------|---------|---------------|
| **EvoAITest.Core** | Core models, services, database | [README](EvoAITest.Core/README.md) |
| **EvoAITest.Agents** | AI agent orchestration | [README](EvoAITest.Agents/README.md) |
| **EvoAITest.LLM** | LLM provider abstraction | [README](EvoAITest.LLM/README.md) |
| **EvoAITest.Web** | Blazor UI components | - |
| **EvoAITest.ApiService** | REST API endpoints | - |
| **EvoAITest.Tests** | Integration tests | - |
| **EvoAITest.AppHost** | Aspire orchestration | - |

### Architecture Documents

| Document | Description |
|----------|-------------|
| [**Recording Architecture**](docs/ARCHITECTURE.md) | Test recording feature architecture |
| [**LLM Routing Architecture**](docs/LLM_ROUTING_ARCHITECTURE.md) | Multi-model routing architecture |

---

## ?? Changelog & History

| Document | Description |
|----------|-------------|
| [**CHANGELOG.md**](docs/CHANGELOG.md) | Project-wide changelog |
| [**Recording Changelog**](docs/RECORDING_CHANGELOG.md) | Recording feature versions |

---

## ??? Roadmap

Future development plans:

| Document | Description |
|----------|-------------|
| **VISUAL_REGRESSION_ROADMAP.md** | Visual regression testing roadmap (if exists) |

---

## ?? Reference Documents

### LLM Routing Feature Documentation

Complete documentation for the LLM routing feature:

- [**Feature Complete Summary**](docs/LLM_ROUTING_FEATURE_COMPLETE.md) - Summary of LLM routing implementation
- [**Documentation Summary**](docs/LLM_ROUTING_DOCUMENTATION_SUMMARY.md) - Documentation overview
- [**Specification**](docs/LLM_ROUTING_SPECIFICATION.md) - Original specification
- [**Checklist**](docs/LLM_ROUTING_CHECKLIST.md) - Implementation checklist

### Implementation History (Archived)

Step-by-step implementation notes from feature development have been organized in the docs/archive/ directory:

- `docs/STEP_*.md` files document the implementation progress of major features

---

## ?? Examples

Working examples demonstrating key features:

| Example | Description | Technologies |
|---------|-------------|--------------|
| [**Login Example**](examples/LoginExample/README.md) | Complete authentication flow | Natural language, Playwright |

---

## ?? Additional Resources

### External Documentation

- [.NET 10 Documentation](https://learn.microsoft.com/dotnet/)
- [Playwright .NET Documentation](https://playwright.dev/dotnet/)
- [Azure OpenAI Service](https://learn.microsoft.com/azure/ai-services/openai/)
- [.NET Aspire Documentation](https://learn.microsoft.com/dotnet/aspire/)
- [Blazor Documentation](https://learn.microsoft.com/aspnet/core/blazor/)

### Development Scripts

| Script | Purpose |
|--------|---------|
| `scripts/README-verify-day5.md` | Day 5 verification guide |

---

## ?? Finding Information

### By Task

**I want to...**

- **Record and generate tests** ? [Recording Quick Start](docs/RECORDING_QUICK_START.md)
- **Set up visual testing** ? [Visual Regression Quick Start](docs/VisualRegressionQuickStart.md)
- **Configure Azure Key Vault** ? [Key Vault Setup](docs/KEY_VAULT_SETUP.md)
- **Understand LLM routing** ? [LLM Complete Guide](docs/LLM_ROUTING_COMPLETE_GUIDE.md)
- **See a working example** ? [Login Example](examples/LoginExample/README.md)
- **Configure the system** ? [LLM Configuration](docs/LLM_ROUTING_CONFIGURATION.md)
- **Understand architecture** ? [Architecture Docs](#architecture-documents)

### By Audience

**For...**

- **New Users** ? Start with [Quick Start Guides](#-quick-start-guides)
- **Developers** ? Review [Project Structure](#project-structure) and [Architecture](#architecture-documents)
- **DevOps** ? Check [Configuration & Setup](#-configuration--setup)
- **QA Engineers** ? Explore [Core Features](#-core-features)
- **Architects** ? Read [Architecture Documents](#architecture-documents)

---

## ?? Support

For issues, questions, or contributions:

- **GitHub Issues**: [Report issues](https://github.com/VladyslavNap/EvoAITest/issues)
- **GitHub Repository**: [Source code](https://github.com/VladyslavNap/EvoAITest)

---

**Last Updated:** January 2026  
**Documentation Version:** 1.0
