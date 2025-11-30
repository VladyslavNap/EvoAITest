# EvoAITest Login Automation Example

A complete, working example demonstrating AI-powered browser automation using the EvoAITest framework. This example shows how to automate login workflows using natural language instructions processed by Azure OpenAI or Ollama.

## Overview

This example demonstrates the **full end-to-end workflow** of the EvoAITest framework:

1. **Task Creation** - Define automation goals in natural language
2. **AI Planning** - Generate step-by-step execution plans using LLMs
3. **Browser Execution** - Execute plans using Playwright browser automation
4. **Result Storage** - Save execution history to SQL Server database
5. **Reporting** - Display detailed execution results

## Prerequisites

### Required

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Playwright Browsers](https://playwright.dev/) (auto-installed on first run)
- SQL Server (LocalDB or full instance)

### LLM Provider (Choose One)

**Option A: Ollama (Free, Local)**
```bash
# Install Ollama
winget install Ollama.Ollama

# Start Ollama server
ollama serve

# Pull model
ollama pull qwen2.5:32b
```

**Option B: Azure OpenAI (Paid)**
- Azure OpenAI resource with GPT-4/GPT-5 deployment
- API key or Managed Identity credentials

## Quick Start

### 1. Navigate to Example Directory

```bash
cd examples/LoginExample
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Install Playwright Browsers

```bash
pwsh ../../EvoAITest.Core/bin/Debug/net10.0/playwright.ps1 install chromium
```

### 4. Configure Settings

Edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "EvoAIDatabase": "Server=.;Database=EvoAITestDB;User Id=u_EvoAi;Password=YOUR_PASSWORD;TrustServerCertificate=True"
  },
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Ollama",
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "qwen2.5:32b"
    }
  }
}
```

### 5. Run the Example

**Basic Usage (Demo Mode):**
```bash
dotnet run
```

**With Target URL:**
```bash
dotnet run https://example.com
```

**With Login Credentials:**
```bash
dotnet run https://example.com myusername mypassword
```

## Usage Examples

### Example 1: Simple Page Analysis

Analyze a webpage without logging in:

```bash
dotnet run https://example.com
```

**What happens:**
- Navigates to example.com
- Extracts page title and main heading
- Identifies interactive elements (buttons, links, inputs)
- Takes screenshot
- Displays results

**Expected Output:**
```
========================================
Starting Login Automation Example
========================================
Target URL: https://example.com

Step 1: Creating automation task...
? Task created with ID: a1b2c3d4-5678-90ab-cdef-1234567890ab

Step 2: Planning execution with AI...
? Plan generated: 5 steps

?? Execution Plan:
   Confidence: 95.0%
   Estimated Duration: 3000ms

   1. navigate
      Reasoning: Navigate to the target URL
   2. wait_for_element
      Selector: h1
      Reasoning: Wait for page to load
   3. get_text
      Selector: h1
      Reasoning: Extract main heading
   4. get_page_state
      Reasoning: Capture page structure
   5. take_screenshot
      Reasoning: Capture visual proof

Step 3: Executing automation steps...
? Execution completed: Success

Step 4: Saving execution results...
? Results saved to database

========================================
Execution Results
========================================
Status: Success
Duration: 2847ms (2.85s)
Steps Executed: 5
Successful Steps: 5
Failed Steps: 0

?? Step-by-Step Results:
   ? Step 1: navigate (250ms)
   ? Step 2: wait_for_element (500ms)
   ? Step 3: get_text (150ms)
   ? Step 4: get_page_state (1200ms)
   ? Step 5: take_screenshot (747ms)

?? Final Output:
   Page title: Example Domain
   Main heading: Example Domain
   Interactive elements: 1 link found

?? Screenshots: 1 captured

?? Automation completed successfully!
========================================
```

### Example 2: Login with Credentials

Automate login to a website:

```bash
dotnet run https://my-app.com user@example.com MySecurePass123
```

**What happens:**
- Navigates to my-app.com
- AI identifies username and password fields
- Enters credentials
- Clicks login button
- Waits for navigation
- Verifies successful login
- Takes screenshot

**Expected Output:**
```
========================================
Starting Login Automation Example
========================================
Target URL: https://my-app.com

Step 1: Creating automation task...
? Task created with ID: f9e8d7c6-5432-10ab-cdef-fedcba098765

Step 2: Planning execution with AI...
? Plan generated: 7 steps

?? Execution Plan:
   Confidence: 92.0%
   Estimated Duration: 5000ms

   1. navigate
      Reasoning: Navigate to login page
   2. wait_for_element
      Selector: input[type='email']
      Reasoning: Wait for login form
   3. type
      Selector: input[type='email']
      Value: ********
      Reasoning: Enter username
   4. type
      Selector: input[type='password']
      Value: ********
      Reasoning: Enter password
   5. click
      Selector: button[type='submit']
      Reasoning: Submit login form
   6. wait_for_element
      Selector: .user-profile
      Reasoning: Wait for successful login
   7. take_screenshot
      Reasoning: Capture logged-in state

Step 3: Executing automation steps...
? Execution completed: Success

Step 4: Saving execution results...
? Results saved to database

========================================
Execution Results
========================================
Status: Success
Duration: 4932ms (4.93s)
Steps Executed: 7
Successful Steps: 7
Failed Steps: 0

?? Step-by-Step Results:
   ? Step 1: navigate (450ms)
   ? Step 2: wait_for_element (650ms)
   ? Step 3: type (180ms)
   ? Step 4: type (170ms)
   ? Step 5: click (120ms)
   ? Step 6: wait_for_element (2500ms)
   ? Step 7: take_screenshot (862ms)

?? Final Output:
   Login successful - user profile displayed

?? Screenshots: 1 captured

?? Automation completed successfully!
========================================
```

### Example 3: Headless Mode (CI/CD)

Run without visible browser (faster, suitable for CI/CD):

Edit `appsettings.json`:
```json
{
  "EvoAITest": {
    "Core": {
      "HeadlessMode": true
    }
  }
}
```

Then run:
```bash
dotnet run
```

## Project Structure

```
examples/LoginExample/
??? LoginExample.csproj           # Project file with dependencies
??? Program.cs                    # Main entry point
??? LoginAutomationExample.cs     # Core example logic
??? appsettings.json             # Configuration
??? appsettings.Development.json # Dev configuration
??? README.md                    # This file
```

## Configuration Options

### Connection String

```json
{
  "ConnectionStrings": {
    "EvoAIDatabase": "Server=.;Database=EvoAITestDB;Integrated Security=true"
  }
}
```

Options:
- **LocalDB**: `Server=(localdb)\\mssqllocaldb;Database=EvoAITestDB;Integrated Security=true`
- **SQL Express**: `Server=.\\SQLEXPRESS;Database=EvoAITestDB;Integrated Security=true`
- **SQL Server**: `Server=.;Database=EvoAITestDB;User Id=sa;Password=YOUR_PASSWORD`

### LLM Provider

**Ollama (Recommended for Development):**
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Ollama",
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "qwen2.5:32b"
    }
  }
}
```

Available models:
- `qwen2.5:32b` - Best quality (requires 32GB RAM)
- `qwen2.5:7b` - Balanced (requires 8GB RAM)
- `mistral` - Fast and capable
- `llama3` - Good general-purpose model

**Azure OpenAI (Production):**
```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "AzureOpenAIEndpoint": "https://your-resource.openai.azure.com",
      "AzureOpenAIDeployment": "gpt-5",
      "AzureOpenAIApiKey": "YOUR_API_KEY"
    }
  }
}
```

### Browser Settings

```json
{
  "EvoAITest": {
    "Core": {
      "HeadlessMode": false,
      "BrowserTimeoutMs": 30000,
      "MaxRetries": 3
    }
  }
}
```

- `HeadlessMode`: Set to `true` for CI/CD (no visible browser)
- `BrowserTimeoutMs`: Maximum wait time for page loads
- `MaxRetries`: Number of retry attempts for failed actions

### Tool Executor Settings

```json
{
  "EvoAITest": {
    "ToolExecutor": {
      "MaxRetries": 2,
      "InitialRetryDelayMs": 500,
      "MaxRetryDelayMs": 5000,
      "UseExponentialBackoff": true,
      "TimeoutPerToolMs": 20000,
      "EnableDetailedLogging": true
    }
  }
}
```

## Troubleshooting

### Issue: "Ollama is not available"

**Solution:**
```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# Start Ollama if not running
ollama serve

# Verify model is installed
ollama list

# Pull model if missing
ollama pull qwen2.5:32b
```

### Issue: "Login failed for user 'u_EvoAi'"

**Solution:**
```sql
-- Enable SQL Server authentication
USE master;
GO
EXEC xp_instance_regwrite N'HKEY_LOCAL_MACHINE', 
    N'Software\Microsoft\MSSQLServer\MSSQLServer', 
    N'LoginMode', REG_DWORD, 2;
GO

-- Restart SQL Server service
-- Then create user:
CREATE LOGIN [u_EvoAi] WITH PASSWORD = 'YOUR_PASSWORD';
CREATE USER [u_EvoAi] FOR LOGIN [u_EvoAi];
ALTER ROLE [db_owner] ADD MEMBER [u_EvoAi];
```

### Issue: "Playwright browsers not installed"

**Solution:**
```bash
# Install Playwright browsers
pwsh ../../EvoAITest.Core/bin/Debug/net10.0/playwright.ps1 install chromium

# Or globally
pwsh -Command "& {Import-Module Microsoft.Playwright; Install-Playwright}"
```

### Issue: "Task failed with timeout"

**Solutions:**
1. Increase timeout in `appsettings.json`:
   ```json
   {
     "EvoAITest": {
       "Core": {
         "BrowserTimeoutMs": 60000
       }
     }
   }
   ```

2. Check if website is accessible:
   ```bash
   curl -I https://example.com
   ```

3. Use headless mode for better performance:
   ```json
   {
     "EvoAITest": {
       "Core": {
         "HeadlessMode": true
       }
     }
   }
   ```

### Issue: "Database migration failed"

**Solution:**
```bash
# Apply migrations manually
cd ../../EvoAITest.Core
dotnet ef database update --startup-project ../EvoAITest.ApiService

# Or drop and recreate database
dotnet ef database drop --force
dotnet ef database update
```

## Code Walkthrough

### 1. Task Creation

```csharp
var task = new AutomationTask
{
    Id = Guid.NewGuid(),
    UserId = "example-user",
    Name = "Login to Example.com",
    Description = "Automated login test",
    NaturalLanguagePrompt = @"
        Navigate to https://example.com and perform login:
        1. Enter username: user@example.com
        2. Enter password: SecurePass123
        3. Click login button
        4. Verify successful login
    ",
    Status = TaskStatus.Pending
};

await _repository.CreateAsync(task);
```

### 2. AI Planning

```csharp
var plan = await _planner.PlanAsync(task.NaturalLanguagePrompt);

// Plan contains:
// - Step-by-step instructions
// - Selectors for elements
// - Expected results
// - Confidence score
```

### 3. Execution

```csharp
var result = await _executor.ExecuteAsync(plan.Steps);

// Result contains:
// - Success/failure status
// - Duration
// - Step-by-step results
// - Screenshots
// - Error messages (if any)
```

### 4. Result Storage

```csharp
var history = new ExecutionHistory
{
    TaskId = task.Id,
    ExecutionStatus = result.Status,
    DurationMs = result.TotalDurationMs,
    FinalOutput = result.FinalOutput,
    StepResults = JsonSerializer.Serialize(result.Steps)
};

await _repository.AddExecutionHistoryAsync(history);
```

## Advanced Usage

### Custom Prompts

```csharp
var example = scope.ServiceProvider.GetRequiredService<LoginAutomationExample>();

await example.RunLoginExampleAsync(
    targetUrl: "https://my-app.com",
    username: "test@example.com",
    password: "MyPassword123"
);
```

### Programmatic Usage

```csharp
using EvoAITest.Examples;
using Microsoft.Extensions.DependencyInjection;

var example = serviceProvider.GetRequiredService<LoginAutomationExample>();

var result = await example.RunLoginExampleAsync(
    "https://example.com",
    "user@test.com",
    "password123"
);

if (result.Status == ExecutionStatus.Success)
{
    Console.WriteLine("Login successful!");
}
```

### Multiple Runs

```bash
# Run multiple scenarios
dotnet run https://site1.com user1 pass1
dotnet run https://site2.com user2 pass2
dotnet run https://site3.com user3 pass3
```

## Performance

### Typical Execution Times

| Scenario | Steps | Duration | Notes |
|----------|-------|----------|-------|
| Page analysis | 5 | 2-3 sec | Navigate + extract data |
| Simple login | 7 | 4-5 sec | Navigate + fill form + submit |
| Complex workflow | 15+ | 10-15 sec | Multiple pages + verification |

### Optimization Tips

1. **Use headless mode** - 20-30% faster
2. **Reduce retries** - Faster failures
3. **Increase timeouts** - For slow sites
4. **Use local Ollama** - No network latency

## Integration with CI/CD

### GitHub Actions

```yaml
name: Login Automation Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    
    services:
      sqlserver:
        image: mcr.microsoft.com/mssql/server:2022-latest
        env:
          ACCEPT_EULA: Y
          SA_PASSWORD: YourStrong!Passw0rd
        ports:
          - 1433:1433
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Install Ollama
        run: |
          curl -fsSL https://ollama.com/install.sh | sh
          ollama serve &
          sleep 5
          ollama pull qwen2.5:7b
      
      - name: Run Login Example
        run: |
          cd examples/LoginExample
          dotnet run https://example.com
```

### Azure DevOps

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
- task: UseDotNet@2
  inputs:
    version: '10.0.x'

- script: |
    curl -fsSL https://ollama.com/install.sh | sh
    ollama serve &
    sleep 5
    ollama pull qwen2.5:7b
  displayName: 'Install Ollama'

- script: |
    cd examples/LoginExample
    dotnet restore
    dotnet run
  displayName: 'Run Example'
```

## Next Steps

1. **Modify the prompt** in `LoginAutomationExample.cs` to match your target website
2. **Add error handling** for specific scenarios
3. **Customize logging** output format
4. **Integrate with test framework** (xUnit, NUnit, MSTest)
5. **Add screenshot comparison** for visual regression testing
6. **Implement retry logic** for flaky scenarios
7. **Add reporting** with HTML/JSON output

## Additional Resources

- [EvoAITest Documentation](../../README.md)
- [Playwright Documentation](https://playwright.dev/dotnet/docs/intro)
- [Ollama Documentation](https://ollama.ai)
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [Browser Tool Registry](../../BROWSER_TOOL_REGISTRY_SUMMARY.md)

## Support

For issues or questions:
- Open an issue on GitHub
- Check existing documentation
- Review example logs for diagnostic information

## License

This example is part of the EvoAITest project and follows the same license.
