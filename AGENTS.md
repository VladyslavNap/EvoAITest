# Repository Guidelines

## Project Structure & Module Organization
- `EvoAITest.Core` hosts automation primitives, LLM abstractions, and EF models shared by every service.  
- `EvoAITest.ApiService` exposes task CRUD/execution APIs, `EvoAITest.AppHost` orchestrates Aspire (API, Web, SQL, Redis), and `EvoAITest.Web` renders the Blazor UI.  
- Supporting code lives in `EvoAITest.Agents`, `EvoAITest.LLM`, and `EvoAITest.ServiceDefaults`, with tests in `EvoAITest.Tests` and reference material in `docs/`, `examples/`, `infra/`, and `scripts/`.

## Build, Test, and Development Commands
- `dotnet restore && dotnet build EvoAITest.slnx` - validates package graph plus analyzers.  
- `dotnet run --project EvoAITest.AppHost` - launches the full stack and Aspire dashboard at http://localhost:15888.  
- `dotnet test` (add `--configuration Release` before push) and `dotnet test --filter "Category=Integration"` for the Playwright-backed journeys.  
- `dotnet format --verify-no-changes` and `dotnet ef migrations add <Name> -p EvoAITest.Core -s EvoAITest.ApiService && dotnet ef database update` keep style/schema aligned; run `pwsh ./scripts/verify-day5.ps1` for the Day 5 smoke.

## Coding Style & Naming Conventions
- Target `net10.0`, nullable enabled, implicit usings on; prefer file-scoped namespaces, 4-space indentation, and braces on new lines.  
- Use PascalCase for types/methods, camelCase for locals, `_camelCase` for private fields, and keep appsettings in sync with `EVOAITEST__SECTION__SETTING` environment variables.  
- Maintain explicit `using` directives, keep regions purposeful, and rely on `dotnet format`; never commit generated Aspire artifacts or `TestResults/` output.

## Testing Guidelines
- `EvoAITest.Tests` combines MSTest and xUnit with FluentAssertions; integration suites bootstrap `CustomWebApplicationFactory` plus in-memory EF/Redis fakes.  
- Name tests `Method_Scenario_Result`, keep coverage near the 90% README badge target, and store heavy fixtures under `TestResults/` only when debugging.  
- Re-run flaky cases with `dotnet test --filter "FullyQualifiedName~<Test>"`, attach logs/screenshots to the PR, then clean the artifacts.

## Commit & Pull Request Guidelines
- Follow the existing history: imperative subjects such as "Optimize regex caching."  
- Every commit must pass `dotnet test` + `dotnet format`; the GitHub/Azure pipeline (build -> integration -> quality) blocks merges otherwise.  
- PRs explain the change, reference affected projects/issues, summarize validation (commands, screenshots, schema diffs), and remain Draft until Aspire diagnostics and integration suites pass.

## Security & Configuration Tips
- Manage secrets via Azure Key Vault or `dotnet user-secrets`; configure `AZURE_OPENAI_ENDPOINT`, `EVOAITEST__CORE__LLMPROVIDER`, and optional Ollama variables through environment settings.  
- Call out new infrastructure requirements (Key Vault, SQL, storage) in the PR and never commit credentials or raw connection strings; update `infra/` and `migration.sql` together when schema or role changes land.
