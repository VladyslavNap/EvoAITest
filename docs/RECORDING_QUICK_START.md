# Test Recording Quick Start Guide

Get started with the Test Generation from Recordings feature in under 5 minutes!

## Prerequisites

- ? .NET 10 SDK installed
- ? SQL Server running (or SQL Server LocalDB)
- ? Azure OpenAI API key OR Ollama running locally

## Step 1: Clone and Build

```bash
# Clone the repository
git clone https://github.com/VladyslavNap/EvoAITest.git
cd EvoAITest

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

## Step 2: Configure Settings

Edit `EvoAITest.ApiService/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "EvoAIDatabase": "Server=(localdb)\\mssqllocaldb;Database=EvoAITest;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Recording": {
    "CaptureScreenshots": true,
    "AutoGenerateAssertions": true,
    "UseAiAnalysis": true,
    "DefaultTestFramework": "xUnit",
    "MinimumConfidenceThreshold": 0.7,
    "TargetAccuracyPercentage": 90.0
  },
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "AzureOpenAI": {
        "Endpoint": "https://your-resource.openai.azure.com/",
        "ApiKey": "your-api-key-here",
        "DeploymentName": "gpt-4"
      }
    }
  }
}
```

### Alternative: Using Ollama (Local)

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Ollama",
      "Ollama": {
        "BaseUrl": "http://localhost:11434",
        "Model": "llama2"
      }
    }
  }
}
```

## Step 3: Apply Database Migrations

```bash
# Navigate to API project
cd EvoAITest.ApiService

# Apply migrations
dotnet ef database update --project ../EvoAITest.Core

# Verify database created
# Check SQL Server for 'EvoAITest' database
```

## Step 4: Run the Application

```bash
# Run with Aspire orchestration
cd ..
dotnet run --project EvoAITest.AppHost

# OR run directly
dotnet run --project EvoAITest.Web
```

The application will start at:
- **Web UI**: https://localhost:5001
- **API**: https://localhost:5002
- **Aspire Dashboard**: https://localhost:15888

## Step 5: Record Your First Test

### Via Blazor UI (Recommended)

1. **Open your browser** to `https://localhost:5001`

2. **Navigate to Test Recorder**:
   - Click "Test Recorder" in the left navigation menu

3. **Configure your recording**:
   ```
   Test Name: My First Login Test
   Starting URL: https://demo.testim.io/
   Description: Test the login functionality
   ```

4. **Start Recording**:
   - Click the "? Start Recording" button
   - A new browser window opens

5. **Perform your test actions**:
   - Click on elements
   - Fill in forms
   - Navigate between pages
   - Everything is captured automatically!

6. **Stop Recording**:
   - Click "? Stop Recording" in the UI
   - Review the captured actions

7. **Analyze with AI**:
   - Actions are automatically analyzed
   - Wait for AI to detect intents (or click "Analyze" if manual)

8. **Generate Test Code**:
   - Select framework (xUnit/NUnit/MSTest)
   - Enable/disable options:
     - ? Include Comments
     - ? Auto-Generate Assertions
     - ? Generate Page Objects
   - Click "?? Generate Test with AI"

9. **Export Your Test**:
   - Click "?? Copy" to copy to clipboard
   - OR click "?? Download" to save as .cs file

### Via API (Programmatic)

```bash
# 1. Start recording
SESSION_ID=$(curl -X POST "https://localhost:5002/api/recordings/start" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "API Test Recording",
    "startUrl": "https://example.com",
    "autoGenerateAssertions": true
  }' | jq -r '.id')

echo "Recording session: $SESSION_ID"

# 2. Perform your actions (interact with browser)
# ...

# 3. Stop recording
curl -X POST "https://localhost:5002/api/recordings/$SESSION_ID/stop"

# 4. Analyze recording
curl -X POST "https://localhost:5002/api/recordings/$SESSION_ID/analyze"

# 5. Generate test code
curl -X POST "https://localhost:5002/api/recordings/$SESSION_ID/generate" \
  -H "Content-Type: application/json" \
  -d '{
    "testFramework": "xUnit",
    "includeComments": true,
    "autoGenerateAssertions": true
  }' | jq -r '.code' > MyTest.cs

echo "Test code saved to MyTest.cs"
```

## Example: Complete Recording Session

Let's record a login test for a demo site:

### 1. Start Recording
```
Test Name: Demo Login Test
Starting URL: https://demo.testim.io/
```

### 2. Captured Actions (Example)
The UI will show real-time actions like:

```
1. ? Navigation to https://demo.testim.io/
   Intent: Navigation | Confidence: 100%

2. ? Click on "Login" button
   Intent: Authentication | Confidence: 95%

3. ? Input in username field: "testuser@example.com"
   Intent: DataEntry | Confidence: 92%

4. ? Input in password field: "********"
   Intent: Authentication | Confidence: 98%

5. ? Click on "Sign In" button
   Intent: FormSubmission | Confidence: 96%

6. ? Wait for dashboard to load
   Intent: Validation | Confidence: 88%
```

### 3. Generated Test Code (xUnit)

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace EvoAITest.Generated;

/// <summary>
/// Demo Login Test
/// </summary>
public class DemoLoginTestTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    /// <summary>
    /// Demo Login Test
    /// </summary>
    [Fact]
    public async Task DemoLoginTest()
    {
        // Arrange
        await _page!.GotoAsync("https://demo.testim.io/");

        // Act
        // User clicked login button to initiate authentication
        await _page!.Locator("button:has-text('Login')").ClickAsync();
        
        // User entered email for authentication
        await _page!.Locator("#username").FillAsync("testuser@example.com");
        
        // User entered password for authentication
        await _page!.Locator("#password").FillAsync("********");
        
        // User submitted login form
        await _page!.Locator("button[type='submit']").ClickAsync();

        // Assert
        await Expect(_page!.Locator(".dashboard-header")).ToBeVisibleAsync();
        Assert.Equal("https://demo.testim.io/dashboard", _page!.Url);
    }
}
```

### 4. Use Your Test

```bash
# Copy the generated code to your test project
cp MyTest.cs ../MyTestProject/Tests/

# Install required packages (if not already)
cd ../MyTestProject
dotnet add package Microsoft.Playwright
dotnet add package xunit

# Run the test
dotnet test --filter DemoLoginTest
```

## Understanding the UI

### Recording Control Panel

```
??????????????????????????????????????????
?  Test Recording                        ?
?  Status: ? Recording                   ?
??????????????????????????????????????????
?  Session: My First Test                ?
?  Duration: 00:02:15                    ?
?  Actions: 12                           ?
??????????????????????????????????????????
?  [? Pause] [? Stop Recording]         ?
??????????????????????????????????????????
```

### Action Feed (Real-time)

Each action shows:
- **?? Green Border**: High confidence (90%+) - Reliable
- **?? Yellow Border**: Medium confidence (70-89%) - Review recommended
- **?? Red Border**: Low confidence (<70%) - Needs attention

### Test Preview Tabs

- **Test Code**: Complete test class with setup/teardown
- **Methods**: Individual test methods
- **Page Objects**: POM classes (if enabled)
- **Action Mapping**: See which code maps to each action

## Troubleshooting

### Issue: Recording doesn't start

**Solution**:
```bash
# Install Playwright browsers
dotnet tool install --global Microsoft.Playwright.CLI
playwright install
```

### Issue: Low confidence scores

**Solutions**:
1. Check LLM provider is configured correctly
2. Ensure `UseAiAnalysis: true` in settings
3. Lower `MinimumConfidenceThreshold` temporarily

### Issue: Database connection error

**Solutions**:
```bash
# Check SQL Server is running
# Verify connection string in appsettings.json
# Run migrations again
dotnet ef database update --project EvoAITest.Core --startup-project EvoAITest.ApiService
```

### Issue: No test code generated

**Solutions**:
1. Ensure at least one action has confidence above threshold
2. Check logs for LLM errors
3. Verify LLM API key/connection

## Next Steps

- ?? Read the [Complete Documentation](../docs/RECORDING_FEATURE.md)
- ?? Explore the [API Reference](../docs/API_REFERENCE.md)
- ??? Learn about the [Architecture](../docs/ARCHITECTURE.md)
- ?? Check out [Advanced Features](../docs/RECORDING_FEATURE.md#advanced-topics)

## Example Scenarios

### Scenario 1: E-Commerce Checkout Flow

Record a complete shopping experience:
1. Search for product
2. Add to cart
3. Proceed to checkout
4. Fill shipping information
5. Complete payment (test mode)

### Scenario 2: Form Validation Testing

Record different validation scenarios:
1. Submit empty form
2. Enter invalid email
3. Enter mismatched passwords
4. Submit valid data

### Scenario 3: Multi-Step Wizard

Record wizard navigation:
1. Complete step 1
2. Navigate to step 2
3. Go back and modify
4. Complete wizard

## Tips for Better Recordings

1. **Use stable selectors**: IDs and data-testid attributes work best
2. **Meaningful names**: Give descriptive names to your tests
3. **One flow per recording**: Keep recordings focused
4. **Review confidence**: Check low-confidence actions before generating
5. **Add manual assertions**: Enhance generated assertions if needed

## Getting Help

- **GitHub Issues**: https://github.com/VladyslavNap/EvoAITest/issues
- **Documentation**: Check `/docs` folder
- **Examples**: See recording examples in `/examples`

---

**Ready to start recording?** ??

Visit `https://localhost:5001/test-recorder` and create your first automated test!
