# Visual Regression Testing - Quick Start Guide

## What is Visual Regression Testing?

Visual regression testing automatically detects unintended visual changes in your web application by comparing screenshots against approved baseline images.

**Perfect for catching:**
- Layout issues
- CSS regressions
- Font rendering problems
- Color changes
- Missing or misaligned elements

## 5-Minute Quick Start

### 1. Install and Run

```bash
# Clone repository
git clone https://github.com/YourOrg/EvoAITest.git
cd EvoAITest

# Setup database
cd EvoAITest.Core
dotnet ef database update

# Install Playwright browsers
cd ../EvoAITest.ApiService/bin/Debug/net10.0
pwsh playwright.ps1 install chromium

# Start application
cd ../../../EvoAITest.AppHost
dotnet run
```

Access:
- **Web UI:** https://localhost:7002
- **API:** https://localhost:7001
- **Swagger:** https://localhost:7001/swagger

### 2. Create Your First Visual Checkpoint

**Via Web UI:**
1. Navigate to https://localhost:7002
2. Click **Create Task**
3. Add visual checkpoint:

```json
{
  "name": "HomePage_Header",
  "type": "Element",
  "tolerance": 0.01,
  "selector": "#header"
}
```

**Via API:**
```bash
curl -X POST https://localhost:7001/api/tasks \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Homepage Test",
    "visualCheckpoints": [{
      "name": "HomePage_Header",
      "type": "Element",
      "tolerance": 0.01,
      "selector": "#header"
    }]
  }'
```

### 3. Run Your First Test

**Via UI:**
1. Find your task in the task list
2. Click **Execute**
3. Wait for completion

**Via API:**
```bash
curl -X POST https://localhost:7001/api/tasks/{taskId}/execute
```

### 4. Review Results

Navigate to the Visual Regression Viewer:
```
https://localhost:7002/visual-regression/{taskId}/HomePage_Header
```

**On First Run:**
- ? Baseline created automatically
- ? Status: "Passed - First Run"
- View baseline image in "Baseline" tab

**On Subsequent Runs:**
- Compare against baseline
- View differences in "Diff" tab
- See metrics (difference %, SSIM score)
- Review detected regions

### 5. Handle Visual Changes

**If change is intentional:**
1. Click **Approve as New Baseline**
2. Enter reason (e.g., "Updated header design per #1234")
3. Confirm approval
4. New screenshot becomes baseline

**If change is unintentional:**
1. Review diff image
2. Fix the issue in your code
3. Run test again
4. Verify difference is gone

## Checkpoint Types

### Full Page
Captures entire page including content below the fold.

```json
{
  "name": "HomePage_FullPage",
  "type": "FullPage",
  "tolerance": 0.01
}
```

**Use for:** Landing pages, complete layouts

### Viewport
Captures only visible area (above the fold).

```json
{
  "name": "HomePage_AboveFold",
  "type": "Viewport",
  "tolerance": 0.01
}
```

**Use for:** Hero sections, navigation bars

### Element
Captures specific element by CSS selector.

```json
{
  "name": "Header_Navigation",
  "type": "Element",
  "tolerance": 0.02,
  "selector": "#main-header"
}
```

**Use for:** Buttons, headers, specific components

### Region
Captures rectangular area by coordinates.

```json
{
  "name": "TopRightSection",
  "type": "Region",
  "tolerance": 0.01,
  "region": {
    "x": 800,
    "y": 0,
    "width": 400,
    "height": 200
  }
}
```

**Use for:** Fixed position elements, specific areas

## Common Patterns

### Ignore Dynamic Content

```json
{
  "name": "HomePage_WithoutAds",
  "type": "FullPage",
  "tolerance": 0.01,
  "ignoreSelectors": [
    ".advertisement",
    ".timestamp",
    "#user-count"
  ]
}
```

### Multiple Checkpoints

```json
{
  "name": "Homepage Test",
  "visualCheckpoints": [
    {
      "name": "Header",
      "type": "Element",
      "selector": "#header",
      "tolerance": 0.01
    },
    {
      "name": "Footer",
      "type": "Element",
      "selector": "#footer",
      "tolerance": 0.01
    },
    {
      "name": "FullPage",
      "type": "FullPage",
      "tolerance": 0.02
    }
  ]
}
```

### Different Environments

```bash
# Development
curl -X POST https://localhost:7001/api/tasks/{id}/execute \
  -d '{"environment": "dev"}'

# Production
curl -X POST https://localhost:7001/api/tasks/{id}/execute \
  -d '{"environment": "prod"}'
```

## Tolerance Guide

| Tolerance | Use Case |
|-----------|----------|
| 0.001 (0.1%) | Very strict - Static images/logos |
| 0.01 (1%) | Strict - Default, text-heavy pages |
| 0.02 (2%) | Moderate - Complex layouts |
| 0.05 (5%) | Lenient - Dynamic content |
| 0.10 (10%) | Very lenient - Highly dynamic |

**Recommendation:** Start with 1% (0.01) and adjust based on results.

## API Quick Reference

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/visual/tasks/{id}/checkpoints` | GET | List checkpoints |
| `/api/visual/checkpoints/{name}/history` | GET | Get history |
| `/api/visual/comparisons/{id}` | GET | Get comparison |
| `/api/visual/checkpoints/{name}/baseline` | GET | Get baseline |
| `/api/visual/checkpoints/{name}/tolerance` | PUT | Update tolerance |
| `/api/visual/images/{path}` | GET | Serve image |
| `/api/visual/tasks/{id}/failures` | GET | Get failures |

## Troubleshooting

### Tests Always Fail

**Problem:** Consistent failures with low difference (0.1-1%)

**Solution:**
```bash
# Increase tolerance
curl -X PUT https://localhost:7001/api/visual/.../tolerance \
  -d '{"newTolerance": 0.02}'
```

### Baseline Not Found

**Problem:** "No baseline found" error

**Solution:**
1. Verify environment matches: `?environment=dev`
2. Verify browser matches: `&browser=chromium`
3. Verify viewport matches: `&viewport=1920x1080`

### Flaky Tests

**Problem:** Tests pass/fail randomly

**Solution:** Add ignore regions for dynamic content
```json
{
  "ignoreSelectors": [".timestamp", ".loading-spinner"]
}
```

## Next Steps

- **[Complete User Guide](docs/VisualRegressionUserGuide.md)** - In-depth usage guide
- **[API Documentation](docs/VisualRegressionAPI.md)** - Complete API reference
- **[Troubleshooting](docs/Troubleshooting.md)** - Detailed troubleshooting

## Configuration

### Minimal Configuration

```json
{
  "EvoAITestCore": {
    "StorageBasePath": "C:\\VisualRegressionData"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=EvoAITest;Integrated Security=true"
  }
}
```

### Full Configuration

See [User Guide - Configuration](docs/VisualRegressionUserGuide.md#configuration) for all options.

## Examples

### JavaScript/TypeScript

```javascript
const API_BASE = 'https://localhost:7001/api/visual';

// Get checkpoints
const response = await fetch(`${API_BASE}/tasks/${taskId}/checkpoints`);
const checkpoints = await response.json();

// Update tolerance
await fetch(`${API_BASE}/tasks/${taskId}/checkpoints/${name}/tolerance`, {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ newTolerance: 0.02 })
});
```

### Python

```python
import requests

API_BASE = 'https://localhost:7001/api/visual'

# Get checkpoints
response = requests.get(f"{API_BASE}/tasks/{task_id}/checkpoints")
checkpoints = response.json()

# Update tolerance
requests.put(
    f"{API_BASE}/tasks/{task_id}/checkpoints/{name}/tolerance",
    json={'newTolerance': 0.02}
)
```

### C#

```csharp
var client = new HttpClient();
var apiBase = "https://localhost:7001/api/visual";

// Get checkpoints
var response = await client.GetAsync($"{apiBase}/tasks/{taskId}/checkpoints");
var checkpoints = await response.Content.ReadFromJsonAsync<TaskCheckpointsResponse>();

// Update tolerance
await client.PutAsJsonAsync(
    $"{apiBase}/tasks/{taskId}/checkpoints/{name}/tolerance",
    new { newTolerance = 0.02 }
);
```

## Support

- **Issues:** [GitHub Issues](https://github.com/YourOrg/EvoAITest/issues)
- **Documentation:** [docs/](docs/)
- **Email:** support@evoaitest.com

---

**Ready to start? Follow the 5-minute quick start above!** ??
