# Tool Executor Integration Tests - Quick Reference

## Running Tests

### Run All Integration Tests
```bash
dotnet test --filter "Category=Integration"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~ExecuteRealNavigationAndClick"
```

### Skip Integration Tests (Run Unit Tests Only)
```bash
dotnet test --filter "Category!=Integration"
```

### Verbose Output
```bash
dotnet test --filter "Category=Integration" --logger "console;verbosity=detailed"
```

## Prerequisites

### 1. Install Playwright Browsers
```powershell
cd EvoAITest.Tests
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

### 2. Verify Internet Connection
Tests access:
- example.com
- httpbin.org

## Test List (9 Tests)

| Test | What It Tests | Duration |
|------|--------------|----------|
| **ExecuteRealNavigationAndClick** | Basic navigation + wait | ~2-3s |
| **ExecuteFormFillSequence** | Sequential form filling (5 steps) | ~4-5s |
| **ExecuteWithPageStateCapture** | Data extraction (text, state, HTML) | ~3-4s |
| **ExecuteRetryOnTransientFailure** | Retry mechanism (stable site) | ~2-3s |
| **ExecuteSequenceWithScreenshots** | Screenshot capture workflow | ~4-5s |
| **ExecuteComplexWorkflow** | 7-step multi-tool workflow | ~5-7s |
| **ExecuteWithFallback** | Fallback strategy execution | ~3-4s |
| **ExecuteGetAccessibilityTree** | Accessibility tree capture | ~2-3s |

**Total Suite Duration**: ~40-60 seconds

## Quick Test Example

```csharp
[Fact]
[Trait("Category", "Integration")]
public async Task ExecuteRealNavigationAndClick_OnExampleCom_Succeeds()
{
    // Arrange
    var navigateCall = new ToolCall(
        "navigate",
        new Dictionary<string, object> { ["url"] = "https://example.com" },
        "Navigate to example.com",
        Guid.NewGuid().ToString());

    // Act
    var result = await _executor!.ExecuteToolAsync(navigateCall);

    // Assert
    result.Success.Should().BeTrue();
    result.AttemptCount.Should().Be(1);
}
```

## Troubleshooting

### Playwright Not Found
```powershell
pwsh bin/Debug/net10.0/playwright.ps1 install chromium
```

### Tests Timeout
- Check internet connection
- Increase timeout in test settings
- Verify example.com/httpbin.org accessible

### Browser Not Cleaning Up
```powershell
taskkill /F /IM chromium.exe
```

## Test Output Example

```
[12:34:56.123] [Information] PlaywrightBrowserAgent: Initializing Playwright
[12:34:57.456] [Information] PlaywrightBrowserAgent: Browser initialized
[12:34:57.500] [Information] DefaultToolExecutor: Starting tool execution: navigate
[12:34:58.234] [Information] PlaywrightBrowserAgent: Navigating to https://example.com
[12:34:59.100] [Information] DefaultToolExecutor: Tool execution succeeded: navigate
Test correlation ID: abc-123
Total execution time: 1234ms
Navigate time: 866ms
```

## CI/CD Quick Setup

### GitHub Actions
```yaml
- name: Install Playwright
  run: |
    cd EvoAITest.Tests
    pwsh bin/Debug/net10.0/playwright.ps1 install chromium --with-deps

- name: Run Integration Tests
  run: dotnet test --filter "Category=Integration" --logger "trx"
```

### Azure DevOps
```yaml
- task: PowerShell@2
  displayName: 'Install Playwright'
  inputs:
    targetType: 'inline'
    script: |
      cd EvoAITest.Tests/bin/Release/net10.0
      pwsh playwright.ps1 install chromium --with-deps

- task: DotNetCoreCLI@2
  displayName: 'Run Integration Tests'
  inputs:
    command: 'test'
    arguments: '--filter "Category=Integration"'
```

## Test Configuration

```csharp
_options = new ToolExecutorOptions
{
    MaxRetries = 2,
    InitialRetryDelayMs = 500,
    TimeoutPerToolMs = 30000,
    EnableDetailedLogging = true
};
```

## Key Features

? Real browser execution  
? Automatic setup/cleanup  
? Test output logging  
? No test isolation issues  
? Comprehensive assertions  
? Performance tracking  
? Fallback testing  
? Screenshot validation  

## Files
- **Tests**: `EvoAITest.Tests/Integration/ToolExecutorIntegrationTests.cs`
- **Docs**: `TOOL_EXECUTOR_INTEGRATION_TESTS_SUMMARY.md`

## Next Steps
1. Run: `dotnet test --filter "Category=Integration"`
2. Review test output
3. Add more scenarios as needed
4. Integrate into CI/CD pipeline
