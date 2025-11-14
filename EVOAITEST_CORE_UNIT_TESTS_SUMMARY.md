# EvoAITest.Core Unit Tests – Summary

The EvoAITest.Tests project delivers 48+ mocked unit tests that lock down the Day 5 baseline without requiring Azure credentials or network access. Detailed execution instructions live in [scripts/README-verify-day5.md](scripts/README-verify-day5.md); this document captures the structure of the suite and links to the owning artefacts.

## Test Suites
- **BrowserToolRegistryTests** – validates tool metadata, lookup helpers, and JSON serialization for the 13 browser tools ([BROWSER_TOOL_REGISTRY_SUMMARY.md](BROWSER_TOOL_REGISTRY_SUMMARY.md)).
- **AutomationTaskTests** – exercises lifecycle transitions, timestamp updates, and plan management for automation tasks ([AUTOMATION_TASK_MODELS_SUMMARY.md](AUTOMATION_TASK_MODELS_SUMMARY.md)).
- **PageState & ToolCall tests** – ensure immutable records stay consistent with the quick-reference API ([QUICK_REFERENCE.md](QUICK_REFERENCE.md)).
- **EvoAITestCoreOptions validation** – covers configuration binding and error messaging for Azure/OpenAI/Ollama scenarios ([EVOAITEST_CORE_CONFIGURATION_GUIDE.md](EVOAITEST_CORE_CONFIGURATION_GUIDE.md)).
- **Integration-style configuration checks** – verify Aspire-friendly service registration and OpenTelemetry hooks ([EVOAITEST_CORE_SERVICE_CONFIG_SUMMARY.md](EVOAITEST_CORE_SERVICE_CONFIG_SUMMARY.md)).

## Tooling
- **xUnit + FluentAssertions** for expressive tests.
- **Moq** for dependency seams.
- Runs with `dotnet test` (no additional setup).

## How to Run
```bash
dotnet test EvoAITest.Tests/EvoAITest.Tests.csproj --configuration Release
```
or execute `pwsh ./scripts/verify-day5.ps1` for the full verification flow.

## CI Integration
- GitHub Actions and Azure Pipelines snippets are provided in [scripts/README-verify-day5.md](scripts/README-verify-day5.md); reuse those templates rather than duplicating YAML here.

Keeping the suites green ensures the Day 5 abstractions remain stable while Phase 1 features land.
