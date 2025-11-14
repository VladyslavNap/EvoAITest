# EvoAITest.Core Service Configuration - Summary

The service extensions focus on wiring EvoAITest.Core into an Aspire-friendly host without repeating the guidance from the configuration guide. Use this note as a quick pointer and fall back to [EVOAITEST_CORE_CONFIGURATION_GUIDE.md](EVOAITEST_CORE_CONFIGURATION_GUIDE.md) for environment-specific values or validation messages.

## Key Extension Methods
- `AddEvoAITestCore(configuration)` binds the `EvoAITest:Core` section, registers `IBrowserToolRegistry`, and adds the project's OpenTelemetry meter/activity source.
- `AddBrowserAgent<TAgent>()` (helper) keeps browser agents scoped and reusable.

The full source lives in `EvoAITest.Core/Extensions/ServiceCollectionExtensions.cs`.

## When to Reference This File
- You are wiring EvoAITest.Core into a new service and need a reminder of the available helpers.
- You want to check which services the core library registers and which ones the consuming app must supply (for example an `IBrowserAgent`).

## Related Documentation
- [EVOAITEST_CORE_CONFIGURATION_GUIDE.md](EVOAITEST_CORE_CONFIGURATION_GUIDE.md) - configuration keys, environment variables, and validation rules.
- [EVOAITEST_CORE_UNIT_TESTS_SUMMARY.md](EVOAITEST_CORE_UNIT_TESTS_SUMMARY.md) - tests covering the configuration binding logic.
- [README.md](README.md) - high-level architecture and link to the documentation map.

Keeping the summary lightweight prevents drift between this file and the configuration guide while still giving newcomers the jumping-off point they need.
