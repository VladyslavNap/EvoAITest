# CI/CD Pipeline Documentation

## Overview

This document describes the CI/CD pipelines configured for EvoAITest, including automated testing for the Tool Executor service with both unit tests and integration tests.

## Pipeline Structure

### GitHub Actions (`.github/workflows/build-and-test.yml`)

**Triggers:**
- Push to `main`, `develop`, or `ToolExecutor` branches
- Pull requests to `main` or `develop`
- Manual workflow dispatch

**Jobs:**

1. **build-and-test**
   - Restore dependencies
   - Build solution
   - Run unit tests (excludes integration tests)
   - Install Playwright browsers
   - Run integration tests
   - Publish test results and code coverage

2. **build-artifacts**
   - Runs only on push to main/develop
   - Publishes API Service
   - Publishes Web App
   - Uploads build artifacts

3. **code-quality**
   - Runs code analyzers
   - Checks code formatting
   - Continues on error (non-blocking)

### Azure DevOps (`azure-pipelines.yml`)

**Stages:**

1. **Build and Unit Test**
   - Restore, build, and run unit tests
   - Publish test results and code coverage

2. **Integration Tests**
   - Installs Playwright browsers
   - Runs integration tests against real browser
   - Publishes integration test results

3. **Publish Artifacts**
   - Runs only on main/develop branches
   - Publishes API Service and Web App artifacts

4. **Code Quality Analysis**
   - Runs independently
   - Performs code analysis and formatting checks

## Test Execution

### Unit Tests

**Command:**
```bash
dotnet test --filter "Category!=Integration"
```

**Coverage:**
- DefaultToolExecutor (30+ tests)
- Successful execution (5 tests)
- Retry logic (6 tests)
- Validation (4 tests)
- Cancellation (3 tests)
- Error handling (3 tests)
- Fallback strategies (3 tests)
- History tracking (3 tests)
- Logging (3 tests)

**Duration:** ~2-3 seconds

### Integration Tests

**Command:**
```bash
dotnet test --filter "Category=Integration"
```

**Prerequisites:**
- Playwright browsers installed
- Internet connection for test websites

**Coverage:**
- Real browser navigation and interaction
- Form filling sequences
- Page state capture
- Screenshot functionality
- Retry mechanisms
- Fallback strategies
- Complex multi-step workflows

**Test Sites:**
- example.com (basic navigation)
- httpbin.org (form testing)

**Duration:** ~40-60 seconds for full suite

## Running Tests Locally

### All Tests
```bash
dotnet test
```

### Unit Tests Only
```bash
dotnet test --filter "Category!=Integration"
```

### Integration Tests Only
```bash
# Install Playwright browsers first
cd EvoAITest.Tests/bin/Debug/net10.0
pwsh playwright.ps1 install chromium

# Run tests
dotnet test --filter "Category=Integration"
```

### With Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover
```

### Verbose Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

## GitHub Actions Setup

### Prerequisites

1. **Repository Secrets** (if needed):
   - `AZURE_OPENAI_ENDPOINT` (for future Azure deployments)
   - `CODECOV_TOKEN` (for code coverage)

2. **Branch Protection Rules**:
   - Require status checks to pass before merging
   - Require branches to be up to date before merging

### Configuration

The workflow file is located at:
```
.github/workflows/build-and-test.yml
```

### Viewing Results

1. Go to **Actions** tab in GitHub repository
2. Click on a workflow run
3. View job logs and test results
4. Download artifacts if needed

### Example Output

```
? build-and-test
  ?? Checkout code
  ?? Setup .NET
  ?? Restore dependencies
  ?? Build solution
  ?? Run Unit Tests (30+ passed)
  ?? Install Playwright browsers
  ?? Run Integration Tests (9 passed)
  ?? Publish Test Results
  ?? Upload Code Coverage

? build-artifacts
  ?? Publish API Service
  ?? Publish Web App
  ?? Upload Artifacts

? code-quality
  ?? Run Analyzers
  ?? Check Code Formatting
```

## Azure DevOps Setup

### Prerequisites

1. **Service Connection** to Azure (for deployments)
2. **Variable Groups** (if needed):
   - Build configuration variables
   - Secret management

### Configuration

The pipeline file is located at:
```
azure-pipelines.yml
```

### Creating the Pipeline

1. Go to **Pipelines** in Azure DevOps
2. Click **New Pipeline**
3. Select **Azure Repos Git** or **GitHub**
4. Select your repository
5. Choose **Existing Azure Pipelines YAML file**
6. Select `/azure-pipelines.yml`
7. Click **Run**

### Viewing Results

1. Go to **Pipelines** > **Pipelines**
2. Click on a pipeline run
3. View stages, jobs, and task logs
4. Check **Tests** tab for test results
5. Check **Code Coverage** tab for coverage reports

### Example Output

```
Stages:
?? Build and Unit Test ?
?  ?? Build Solution
?     ?? Install .NET SDK
?     ?? Restore NuGet packages
?     ?? Build solution
?     ?? Run Unit Tests (30+ passed)
?     ?? Publish Test Results
?
?? Integration Tests ?
?  ?? Run Integration Tests
?     ?? Install Playwright Browsers
?     ?? Run Integration Tests (9 passed)
?     ?? Publish Integration Test Results
?
?? Publish Artifacts ?
?  ?? Publish Build Artifacts
?     ?? Publish API Service
?     ?? Publish Web App
?
?? Code Quality Analysis ??
   ?? Code Quality Checks
      ?? Build with warnings as errors
      ?? Check code formatting
```

## Test Result Artifacts

### GitHub Actions

Artifacts are uploaded after each run:
- `test-results` - TRX files and code coverage reports

Download from:
1. Workflow run page
2. Scroll to **Artifacts** section
3. Click to download

### Azure DevOps

Test results are automatically integrated:
- **Tests** tab shows pass/fail summary
- **Code Coverage** tab shows coverage reports
- Build artifacts contain published applications

## Code Coverage

### Viewing in GitHub

1. Code coverage is uploaded to Codecov (if configured)
2. View reports at: `https://codecov.io/gh/YourOrg/EvoAITest`
3. Coverage badge in README

### Viewing in Azure DevOps

1. Go to pipeline run
2. Click **Code Coverage** tab
3. View line, branch, and method coverage
4. Drill down to file level

### Expected Coverage

- **Tool Executor**: ~90-95%
- **Unit Tests**: 100% for tested paths
- **Integration Tests**: Focus on real scenarios

## Troubleshooting

### Tests Failing Locally But Passing in CI

**Possible Causes:**
- Environment differences
- Timezone issues with DateTimeOffset
- File path differences (Windows vs Linux)

**Solution:**
```bash
# Run tests in same configuration as CI
dotnet test --configuration Release
```

### Integration Tests Timing Out

**Possible Causes:**
- Slow network connection
- Playwright browsers not installed
- Test websites unreachable

**Solution:**
```powershell
# Install browsers
cd EvoAITest.Tests/bin/Debug/net10.0
pwsh playwright.ps1 install chromium --with-deps

# Increase timeout
dotnet test --filter "Category=Integration" -- RunConfiguration.TestSessionTimeout=600000
```

### GitHub Actions Workflow Not Triggering

**Check:**
- YAML syntax (use online validator)
- Branch names match trigger configuration
- Workflow file is in `.github/workflows/`

### Azure DevOps Pipeline Failing

**Check:**
- Service connections are valid
- Agent has required permissions
- NuGet packages can be restored

## Performance Optimization

### Caching Dependencies

**GitHub Actions:**
```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
    restore-keys: |
      ${{ runner.os }}-nuget-
```

**Azure DevOps:**
```yaml
- task: Cache@2
  inputs:
    key: 'nuget | "$(Agent.OS)" | **/packages.lock.json'
    path: $(NUGET_PACKAGES)
  displayName: Cache NuGet packages
```

### Parallel Test Execution

Unit tests already run in parallel by default. For integration tests:

```bash
dotnet test --parallel --filter "Category=Integration"
```

## Best Practices

### ? DO

- Run unit tests on every commit
- Run integration tests on pull requests
- Fail the build if tests fail
- Monitor code coverage trends
- Keep test execution time under 5 minutes
- Use test result artifacts for debugging
- Tag integration tests with `[Trait("Category", "Integration")]`

### ? DON'T

- Skip tests in CI/CD
- Ignore failing tests
- Run integration tests in parallel without isolation
- Hardcode secrets in pipeline YAML
- Commit without running tests locally
- Disable test reporting

## Future Enhancements

1. **Add Performance Tests**
   - Benchmark tool execution speed
   - Monitor regression over time

2. **Add Security Scanning**
   - Dependency vulnerability scanning
   - Code security analysis

3. **Add Deployment Stages**
   - Deploy to staging environment
   - Run smoke tests
   - Deploy to production

4. **Add Notification**
   - Slack/Teams integration
   - Email on build failures

5. **Add Test Flakiness Detection**
   - Track test reliability
   - Automatic retry of flaky tests

## Support

For issues with CI/CD pipelines:

1. Check pipeline logs for error details
2. Verify test execution locally
3. Review [GitHub Actions documentation](https://docs.github.com/actions)
4. Review [Azure DevOps documentation](https://docs.microsoft.com/azure/devops/pipelines/)
5. Open an issue in the repository

## Related Documentation

- [Tool Executor Tests Summary](DEFAULT_TOOL_EXECUTOR_TESTS_SUMMARY.md)
- [Integration Tests Summary](TOOL_EXECUTOR_INTEGRATION_TESTS_SUMMARY.md)
- [Integration Tests Quick Reference](TOOL_EXECUTOR_INTEGRATION_TESTS_QUICK_REFERENCE.md)
- [README.md](README.md)

---

**Last Updated:** [Current Date]  
**Pipeline Status:** ? Configured and Ready  
**Test Coverage:** ~90-95% for Tool Executor
