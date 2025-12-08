# Visual Regression Testing - Troubleshooting Guide

## Table of Contents

1. [Common Issues](#common-issues)
2. [Installation Problems](#installation-problems)
3. [Runtime Errors](#runtime-errors)
4. [Comparison Issues](#comparison-issues)
5. [Browser Issues](#browser-issues)
6. [Database Issues](#database-issues)
7. [Performance Issues](#performance-issues)
8. [Debugging Tips](#debugging-tips)

---

## Common Issues

### Issue: Tests Always Fail (False Positives)

**Symptoms:**
- Visual comparisons consistently fail
- Difference percentage is low (0.1% - 1%)
- Images look identical to the eye

**Possible Causes:**
1. Font rendering differences across environments
2. Anti-aliasing variations
3. Browser rendering differences
4. Animations not fully complete

**Solutions:**

**1. Increase Tolerance**
```bash
curl -X PUT https://localhost:7001/api/visual/tasks/{taskId}/checkpoints/{name}/tolerance \
  -H "Content-Type: application/json" \
  -d '{"newTolerance": 0.02, "applyToAll": false}'
```

**2. Add Stabilization Wait**
```csharp
// In your test setup
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
await Task.Delay(1000); // Additional buffer
```

**3. Disable Animations**
```javascript
// Add to page load
await page.EvaluateAsync(@"
    (function() {
        var style = document.createElement('style');
        style.innerHTML = '* { animation: none !important; transition: none !important; }';
        document.head.appendChild(style);
    })()
");
```

**4. Use Consistent Browser Configuration**
```json
{
  "Browser": "chromium",
  "Viewport": "1920x1080",
  "DeviceScaleFactor": 1.0
}
```

---

### Issue: Baseline Not Found

**Symptoms:**
- Error: "No baseline found for checkpoint"
- Every execution creates a new baseline
- Comparisons never run

**Possible Causes:**
1. Environment mismatch (dev vs staging vs prod)
2. Browser mismatch (chromium vs firefox)
3. Viewport mismatch (different resolutions)
4. Checkpoint name changed
5. Baseline not approved

**Solutions:**

**1. Verify Configuration Matches**
```bash
# Check what configuration was used for baseline
curl https://localhost:7001/api/visual/tasks/{taskId}/checkpoints/{name}/baseline?environment=dev&browser=chromium&viewport=1920x1080
```

**2. Check Database**
```sql
SELECT * FROM VisualBaselines 
WHERE TaskId = '{taskId}' 
  AND CheckpointName = '{name}'
  AND Environment = 'dev'
  AND Browser = 'chromium'
  AND Viewport = '1920x1080';
```

**3. List All Baselines for Task**
```bash
curl https://localhost:7001/api/visual/tasks/{taskId}/checkpoints
```

**4. Create Baseline Manually**
- Navigate to Visual Regression Viewer
- Run the test once to create baseline
- Approve the baseline

---

### Issue: Flaky Tests (Pass/Fail Randomly)

**Symptoms:**
- Test passes sometimes, fails other times
- Same checkpoint, same environment
- Differences in timing-dependent content

**Possible Causes:**
1. Loading indicators visible
2. Animations not complete
3. Lazy-loaded images
4. Timestamps or dynamic content
5. Race conditions

**Solutions:**

**1. Add Ignore Regions**
```json
{
  "name": "HomePage",
  "type": "FullPage",
  "tolerance": 0.01,
  "ignoreSelectors": [
    ".loading-spinner",
    ".timestamp",
    "#user-count",
    ".advertisement"
  ]
}
```

**2. Wait for Specific Elements**
```csharp
// Wait for all images to load
await page.WaitForFunctionAsync(@"
    () => Array.from(document.images).every(img => img.complete)
");

// Wait for specific element to be stable
await page.WaitForSelectorAsync("#main-content", new() {
    State = WaitForSelectorState.Visible
});
await Task.Delay(500); // Stabilization buffer
```

**3. Disable Network Activity**
```csharp
// Block ads and analytics
await page.RouteAsync("**/*.{png,jpg,jpeg,gif,svg}", route => {
    if (route.Request.Url.Contains("ad")) {
        route.Abort();
    } else {
        route.Continue();
    }
});
```

---

### Issue: Large Diff Percentage (Entire Page Different)

**Symptoms:**
- 100% or near-100% difference
- Entire page highlighted red in diff
- SSIM score very low (<0.5)

**Possible Causes:**
1. Page didn't load completely
2. Wrong URL loaded
3. Authentication required but not provided
4. JavaScript errors prevented rendering
5. CSS not loaded

**Solutions:**

**1. Verify Page Load**
```csharp
var response = await page.GotoAsync(url, new() {
    WaitUntil = WaitUntilState.NetworkIdle,
    Timeout = 60000
});

if (!response.Ok) {
    throw new Exception($"Page load failed: {response.Status} {response.StatusText}");
}
```

**2. Check Console Errors**
```csharp
page.Console += (_, msg) => {
    if (msg.Type == "error") {
        _logger.LogError("Browser console error: {Message}", msg.Text);
    }
};
```

**3. Add Authentication**
```csharp
// Set authentication cookie
await context.AddCookiesAsync(new[] {
    new Cookie {
        Name = "auth_token",
        Value = "your-token",
        Domain = "example.com",
        Path = "/"
    }
});
```

**4. Take Debug Screenshots**
```csharp
await page.ScreenshotAsync(new() {
    Path = $"debug_{DateTime.Now:yyyyMMdd_HHmmss}.png",
    FullPage = true
});
```

---

## Installation Problems

### Issue: Database Migration Failed

**Error:**
```
Cannot open database "EvoAITest" requested by the login.
```

**Solution:**
1. **Create Database**
   ```sql
   CREATE DATABASE EvoAITest;
   GO
   ```

2. **Update Connection String**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=EvoAITest;Trusted_Connection=True;TrustServerCertificate=True"
     }
   }
   ```

3. **Run Migrations**
   ```bash
   cd EvoAITest.Core
   dotnet ef database update
   ```

---

### Issue: Playwright Browsers Not Installed

**Error:**
```
Executable doesn't exist at C:\Users\...\ms-playwright\chromium-1234\chrome-win\chrome.exe
```

**Solution:**
1. **Install Browsers**
   ```bash
   pwsh bin/Debug/net10.0/playwright.ps1 install
   ```

2. **Verify Installation**
   ```bash
   pwsh bin/Debug/net10.0/playwright.ps1 install --dry-run
   ```

3. **Install Specific Browser**
   ```bash
   pwsh bin/Debug/net10.0/playwright.ps1 install chromium
   ```

---

### Issue: Storage Directory Not Accessible

**Error:**
```
Access to the path 'C:\VisualRegressionData' is denied.
```

**Solution:**
1. **Create Directory**
   ```powershell
   New-Item -Path "C:\VisualRegressionData" -ItemType Directory -Force
   ```

2. **Set Permissions**
   ```powershell
   $acl = Get-Acl "C:\VisualRegressionData"
   $rule = New-Object System.Security.AccessControl.FileSystemAccessRule("Users","FullControl","Allow")
   $acl.SetAccessRule($rule)
   Set-Acl "C:\VisualRegressionData" $acl
   ```

3. **Use Alternative Path**
   ```json
   {
     "EvoAITestCore": {
       "StorageBasePath": "C:\\Users\\YourUser\\VisualRegressionData"
     }
   }
   ```

---

## Runtime Errors

### Issue: Visual Comparison Service Not Configured

**Error:**
```
Service 'IVisualComparisonService' not found in DI container
```

**Solution:**

**1. Verify Service Registration**

Check `Program.cs` or `Startup.cs`:
```csharp
builder.Services.AddEvoAITestCore(builder.Configuration);
```

**2. Check Extension Method**

In `ServiceCollectionExtensions.cs`:
```csharp
services.AddScoped<IVisualComparisonService, VisualComparisonService>();
services.AddScoped<VisualComparisonEngine>();
services.AddSingleton<IFileStorageService, LocalFileStorageService>();
```

**3. Rebuild Solution**
```bash
dotnet clean
dotnet build
```

---

### Issue: Screenshot Timeout

**Error:**
```
Timeout 30000ms exceeded while waiting for element to be visible
```

**Solutions:**

**1. Increase Timeout**
```csharp
await page.WaitForSelectorAsync("#element", new() {
    Timeout = 60000 // 60 seconds
});
```

**2. Check Element Exists**
```csharp
var element = await page.QuerySelectorAsync("#element");
if (element == null) {
    throw new Exception("Element not found");
}
```

**3. Wait for Network Idle**
```csharp
await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new() {
    Timeout = 60000
});
```

---

### Issue: Out of Memory

**Error:**
```
System.OutOfMemoryException: Insufficient memory to continue execution
```

**Solutions:**

**1. Dispose Images Properly**
```csharp
using var baseline = Image.Load<Rgba32>(baselineBytes);
using var actual = Image.Load<Rgba32>(actualBytes);
using var diff = GenerateDiffImage(baseline, actual);
// Images automatically disposed
```

**2. Process in Batches**
```csharp
// Process checkpoints in smaller batches
foreach (var batch in checkpoints.Chunk(5)) {
    foreach (var checkpoint in batch) {
        await ProcessCheckpoint(checkpoint);
    }
    GC.Collect(); // Force garbage collection between batches
}
```

**3. Reduce Image Size**
```csharp
// Resize large images before comparison
if (image.Width > 1920 || image.Height > 1080) {
    image.Mutate(x => x.Resize(new ResizeOptions {
        Size = new Size(1920, 1080),
        Mode = ResizeMode.Max
    }));
}
```

---

## Comparison Issues

### Issue: SSIM Score Doesn't Match Visual Difference

**Symptoms:**
- SSIM score is high (>0.95) but images look different
- Or SSIM score is low (<0.90) but images look similar

**Explanation:**

SSIM measures structural similarity (layout, composition), while pixel comparison measures exact color matching.

**When SSIM High but Pixels Different:**
- Color changes (same structure, different colors)
- Brightness/contrast adjustments
- Small text changes

**When SSIM Low but Pixels Similar:**
- Layout shifts
- Element position changes
- Size changes

**Solution:**

Use both metrics together:
```csharp
if (result.DifferencePercentage > checkpoint.Tolerance && result.SsimScore < 0.90) {
    // Significant difference - both metrics agree
    return ComparisonResult.Failed;
} else if (result.DifferencePercentage > checkpoint.Tolerance && result.SsimScore > 0.95) {
    // Minor color differences - may be acceptable
    return ComparisonResult.Warning;
}
```

---

### Issue: Ignore Regions Not Working

**Symptoms:**
- Ignored regions still cause failures
- Dynamic content still detected as different

**Solutions:**

**1. Verify Selectors**
```javascript
// Test selector in browser console
document.querySelectorAll('.timestamp').length // Should return count
```

**2. Wait for Elements**
```csharp
// Ensure ignored elements are loaded
foreach (var selector in checkpoint.IgnoreSelectors) {
    await page.WaitForSelectorAsync(selector, new() {
        State = WaitForSelectorState.Attached,
        Timeout = 5000
    }).ConfigureAwait(false);
}
```

**3. Use More Specific Selectors**
```json
{
  "ignoreSelectors": [
    "div.timestamp",           // ? Good - specific
    "#user-count",             // ? Good - ID
    "span[data-dynamic='true']" // ? Good - attribute
  ]
}
```

---

## Browser Issues

### Issue: Browser Crashes

**Error:**
```
Target page, context or browser has been closed
```

**Solutions:**

**1. Increase Memory**
```csharp
var browser = await playwright.Chromium.LaunchAsync(new() {
    Headless = true,
    Args = new[] {
        "--disable-dev-shm-usage",
        "--no-sandbox",
        "--disable-setuid-sandbox",
        "--disable-gpu"
    }
});
```

**2. Limit Concurrent Browsers**
```csharp
var semaphore = new SemaphoreSlim(2); // Max 2 concurrent browsers
await semaphore.WaitAsync();
try {
    await RunBrowserTest();
} finally {
    semaphore.Release();
}
```

**3. Restart Browser Periodically**
```csharp
private int _testsRun = 0;
private const int MAX_TESTS_PER_BROWSER = 50;

if (_testsRun >= MAX_TESTS_PER_BROWSER) {
    await _browser.DisposeAsync();
    _browser = await playwright.Chromium.LaunchAsync();
    _testsRun = 0;
}
```

---

### Issue: Screenshot Colors Look Different

**Symptoms:**
- Colors appear washed out or oversaturated
- Gradients render differently

**Solutions:**

**1. Force Color Profile**
```csharp
var context = await browser.NewContextAsync(new() {
    ColorScheme = ColorScheme.Light,
    ForcedColors = ForcedColors.None
});
```

**2. Set Device Scale Factor**
```csharp
var context = await browser.NewContextAsync(new() {
    DeviceScaleFactor = 1.0, // Disable high-DPI scaling
    Viewport = new() { Width = 1920, Height = 1080 }
});
```

**3. Disable Hardware Acceleration**
```csharp
var browser = await playwright.Chromium.LaunchAsync(new() {
    Args = new[] { "--disable-gpu", "--disable-software-rasterizer" }
});
```

---

## Database Issues

### Issue: Slow Queries

**Symptoms:**
- API endpoints taking >1 second
- Comparison history loading slowly
- Database CPU high

**Solutions:**

**1. Verify Indexes**
```sql
-- Check if indexes exist
SELECT name, type_desc FROM sys.indexes 
WHERE object_id = OBJECT_ID('VisualComparisonResults');

-- Add missing indexes if needed
CREATE INDEX IX_VisualComparisonResults_TaskCheckpoint 
ON VisualComparisonResults (TaskId, CheckpointName);
```

**2. Use AsNoTracking**
```csharp
var results = await _context.VisualComparisonResults
    .AsNoTracking() // Faster read-only queries
    .Where(r => r.TaskId == taskId)
    .ToListAsync();
```

**3. Add Pagination**
```csharp
var results = await _context.VisualComparisonResults
    .Where(r => r.TaskId == taskId)
    .OrderByDescending(r => r.ComparedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

---

### Issue: Database Connection Pool Exhausted

**Error:**
```
Timeout expired. The timeout period elapsed prior to obtaining a connection from the pool.
```

**Solutions:**

**1. Increase Pool Size**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EvoAITest;Integrated Security=true;Max Pool Size=200;Min Pool Size=5"
  }
}
```

**2. Dispose Contexts Properly**
```csharp
using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<EvoAIDbContext>();
// Use context
// Automatically disposed
```

**3. Use Connection Resiliency**
```csharp
services.AddDbContext<EvoAIDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    }));
```

---

## Performance Issues

### Issue: Slow Comparisons

**Symptoms:**
- Comparison taking >10 seconds for 1920x1080
- High CPU usage during comparison

**Solutions:**

**1. Use Parallel Processing**
```csharp
Parallel.For(0, height, new ParallelOptions { 
    MaxDegreeOfParallelism = Environment.ProcessorCount 
}, y => {
    for (int x = 0; x < width; x++) {
        // Pixel comparison
    }
});
```

**2. Downsample for SSIM**
```csharp
// Resize to 25% for SSIM calculation
using var baselineSmall = baseline.Clone(ctx => 
    ctx.Resize(width / 2, height / 2));
using var actualSmall = actual.Clone(ctx => 
    ctx.Resize(width / 2, height / 2));

var ssimScore = CalculateSSIM(baselineSmall, actualSmall);
```

**3. Early Exit on Tolerance Exceeded**
```csharp
if (pixelsDifferent > totalPixels * checkpoint.Tolerance * 2) {
    // Already way over tolerance, stop early
    break;
}
```

---

### Issue: High Memory Usage

**Symptoms:**
- Application using >2GB RAM
- Frequent garbage collections
- System slowing down

**Solutions:**

**1. Limit Image Cache**
```csharp
services.AddMemoryCache(options => {
    options.SizeLimit = 100 * 1024 * 1024; // 100 MB max
});
```

**2. Compress Images**
```csharp
image.SaveAsPng(stream, new PngEncoder {
    CompressionLevel = PngCompressionLevel.BestCompression
});
```

**3. Clean Up Old Files**
```csharp
// Delete files older than 30 days
var cutoffDate = DateTimeOffset.UtcNow.AddDays(-30);
await repository.DeleteOldBaselinesAsync(cutoffDate, cancellationToken);
```

---

## Debugging Tips

### Enable Debug Logging

**appsettings.Development.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "EvoAITest.Core.Services.VisualComparisonService": "Debug",
      "EvoAITest.Core.Services.VisualComparisonEngine": "Debug",
      "EvoAITest.Core.Browser.PlaywrightBrowserAgent": "Debug"
    }
  }
}
```

### View Logs

**Console Output:**
```bash
dotnet run --project EvoAITest.ApiService
```

**Log Files:**
```bash
tail -f logs/evoaitest-{date}.log
```

**Filter Logs:**
```bash
grep "Visual" logs/evoaitest-{date}.log
```

### Debugging API Requests

**Using cURL with verbose output:**
```bash
curl -v https://localhost:7001/api/visual/tasks/{taskId}/checkpoints
```

**Using Postman:**
1. Import OpenAPI spec from `/swagger/v1/swagger.json`
2. Test endpoints with debugging
3. View response headers and timing

### Browser Debugging

**Run in Non-Headless Mode:**
```csharp
var browser = await playwright.Chromium.LaunchAsync(new() {
    Headless = false, // Show browser window
    SlowMo = 1000     // Slow down by 1 second per action
});
```

**Save Debug Screenshots:**
```csharp
await page.ScreenshotAsync(new() {
    Path = $"debug_{checkpoint.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png",
    FullPage = true
});
```

**Record Video:**
```csharp
var context = await browser.NewContextAsync(new() {
    RecordVideoDir = "videos/",
    RecordVideoSize = new() { Width = 1920, Height = 1080 }
});
```

---

## Getting Additional Help

### Check Documentation

- **User Guide:** `/docs/VisualRegressionUserGuide.md`
- **API Docs:** `/docs/VisualRegressionAPI.md`
- **Dev Guide:** `/docs/VisualRegressionDevelopment.md`

### Online Resources

- **GitHub Issues:** https://github.com/YourOrg/EvoAITest/issues
- **Discussions:** https://github.com/YourOrg/EvoAITest/discussions
- **Documentation:** https://docs.evoaitest.com

### Support Channels

- **Email:** support@evoaitest.com
- **Slack:** [workspace-url]
- **Stack Overflow:** Tag `evoaitest`

### Report a Bug

**Include:**
1. Error message and stack trace
2. Steps to reproduce
3. Expected vs actual behavior
4. Environment details (OS, .NET version)
5. Configuration (redact sensitive data)
6. Logs (enable debug logging)

**Template:**
```markdown
**Description:**
Brief description of the issue

**Steps to Reproduce:**
1. Step 1
2. Step 2
3. Step 3

**Expected Behavior:**
What should happen

**Actual Behavior:**
What actually happens

**Environment:**
- OS: Windows 11
- .NET: 10.0
- Browser: Chromium 120.0

**Logs:**
```
[Paste relevant logs here]
```

**Screenshots:**
[Attach screenshots if applicable]
```

---

**Version:** 1.0  
**Last Updated:** 2025-12-07  
**Need Help?** support@evoaitest.com
