# CI/CD Quick Reference

## Quick Commands

### Run All Tests Locally
```bash
dotnet test
```

### Run Unit Tests Only
```bash
dotnet test --filter "Category!=Integration"
```

### Run Integration Tests Only
```bash
# First, install Playwright browsers
cd EvoAITest.Tests/bin/Debug/net10.0
pwsh playwright.ps1 install chromium

# Then run integration tests
dotnet test --filter "Category=Integration"
```

### Run With Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## GitHub Actions Status

### View Pipeline Status
- Go to: https://github.com/VladyslavNap/EvoAITest/actions
- Or click the build badge in README

### Workflow Triggers
- ? Push to `main`, `develop`, or `ToolExecutor` branches
- ? Pull requests to `main` or `develop`
- ? Manual workflow dispatch

### Jobs
1. **build-and-test** (~5-7 mins)
   - Unit tests (~3s)
   - Integration tests (~60s)
2. **build-artifacts** (~2 mins)
3. **code-quality** (~1 min)

## Azure DevOps Status

### View Pipeline
- Go to Azure DevOps ? Pipelines
- Select **EvoAITest** pipeline

### Stages
1. **Build** (~3 mins)
2. **Integration Test** (~2 mins)
3. **Publish** (~2 mins)
4. **Code Quality** (~1 min)

## Test Results

### GitHub Actions
- View in **Actions** tab
- Download `test-results` artifact
- TRX files contain detailed results

### Azure DevOps
- **Tests** tab shows summary
- **Code Coverage** tab shows coverage
- Click test name for details

## Common Issues

### Integration Tests Fail - Playwright Not Installed
```bash
cd EvoAITest.Tests/bin/Debug/net10.0
pwsh playwright.ps1 install chromium --with-deps
```

### Tests Pass Locally But Fail in CI
```bash
# Run in Release configuration like CI
dotnet test --configuration Release
```

### Code Coverage Not Showing
```bash
# Ensure coverage collector is specified
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

## Key Files

| File | Purpose |
|------|---------|
| `.github/workflows/build-and-test.yml` | GitHub Actions pipeline |
| `azure-pipelines.yml` | Azure DevOps pipeline |
| `CI_CD_PIPELINE_DOCUMENTATION.md` | Full documentation |

## Test Metrics

| Metric | Value |
|--------|-------|
| Total Tests | 57+ (48 unit + 9 integration) |
| Unit Test Duration | ~2-3 seconds |
| Integration Test Duration | ~40-60 seconds |
| Code Coverage | ~90-95% |
| CI/CD Pipeline Duration | ~5-10 minutes |

## Pipeline Status Badges

Add to README.md:
```markdown
[![Build Status](https://img.shields.io/github/actions/workflow/status/VladyslavNap/EvoAITest/build-and-test.yml?branch=main)](https://github.com/VladyslavNap/EvoAITest/actions)
[![Tests](https://img.shields.io/badge/tests-57+-brightgreen)](https://github.com/VladyslavNap/EvoAITest/actions)
[![Coverage](https://img.shields.io/badge/coverage-90%25-brightgreen)](https://github.com/VladyslavNap/EvoAITest/actions)
```

## Notifications

### GitHub Actions
- Configure in repository settings
- Options: Email, Slack, Teams

### Azure DevOps
- Pipeline ? Edit ? Triggers ? Build completion
- Options: Email, Slack, Teams, ServiceNow

## Next Steps

1. ? Pipelines configured
2. ? Tests running automatically
3. ? Monitor test results
4. ? Add deployment stages
5. ? Configure notifications

---

**Quick Links:**
- [Full CI/CD Documentation](CI_CD_PIPELINE_DOCUMENTATION.md)
- [Unit Tests Summary](DEFAULT_TOOL_EXECUTOR_TESTS_SUMMARY.md)
- [Integration Tests Summary](TOOL_EXECUTOR_INTEGRATION_TESTS_SUMMARY.md)
