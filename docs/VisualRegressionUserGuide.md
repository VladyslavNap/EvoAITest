# Visual Regression Testing - User Guide

## Table of Contents

1. [Introduction](#introduction)
2. [Getting Started](#getting-started)
3. [Creating Visual Checkpoints](#creating-visual-checkpoints)
4. [Running Tests](#running-tests)
5. [Reviewing Results](#reviewing-results)
6. [Managing Baselines](#managing-baselines)
7. [Configuring Tolerance](#configuring-tolerance)
8. [Best Practices](#best-practices)
9. [Troubleshooting](#troubleshooting)

---

## Introduction

### What is Visual Regression Testing?

Visual regression testing automatically detects unintended visual changes in your web application by comparing screenshots against approved baseline images. This helps catch:

- Layout issues
- CSS regressions
- Font rendering problems
- Color changes
- Missing or misaligned elements
- Responsive design issues

### How It Works

1. **Baseline Creation**: First run captures and stores baseline screenshots
2. **Comparison**: Subsequent runs compare new screenshots against baselines
3. **Difference Detection**: Pixel-by-pixel and SSIM analysis identifies changes
4. **Review**: Failed comparisons show visual diffs for manual review
5. **Approval**: Approve legitimate changes as new baselines

---

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server (or SQL Server Express)
- Modern web browser (Chrome, Firefox, or Edge)
- Git (for baseline versioning)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/YourOrg/EvoAITest.git
   cd EvoAITest
   ```

2. **Configure database connection**
   
   Update `appsettings.json` in `EvoAITest.ApiService`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Database=EvoAITest;Integrated Security=true;"
     }
   }
   ```

3. **Run database migrations**
   ```bash
   cd EvoAITest.Core
   dotnet ef database update
   ```

4. **Configure file storage**
   
   Update `EvoAITestCoreOptions` in `appsettings.json`:
   ```json
   {
     "EvoAITestCore": {
       "StorageBasePath": "C:\\VisualRegressionData"
     }
   }
   ```

5. **Start the application**
   ```bash
   cd EvoAITest.AppHost
   dotnet run
   ```

   This starts:
   - API Service: `https://localhost:7001`
   - Web UI: `https://localhost:7002`

---

## Creating Visual Checkpoints

### What is a Visual Checkpoint?

A visual checkpoint is a point in your test where a screenshot is captured and compared against a baseline. Each checkpoint has:

- **Name**: Unique identifier (e.g., "HomePage_Header")
- **Type**: Screenshot type (FullPage, Viewport, Element, Region)
- **Tolerance**: Acceptable difference percentage (0.0 - 1.0)
- **Selector**: CSS selector for Element checkpoints
- **Region**: Coordinates for Region checkpoints

### Checkpoint Types

#### 1. Full Page
Captures the entire page including content below the fold.

**Use Cases:**
- Landing pages
- Complete page layouts
- Multi-section pages

**Example:**
```json
{
  "name": "HomePage_FullPage",
  "type": "FullPage",
  "tolerance": 0.01
}
```

#### 2. Viewport
Captures only the visible area (above the fold).

**Use Cases:**
- Hero sections
- Navigation bars
- Content that fits in viewport

**Example:**
```json
{
  "name": "HomePage_AboveFold",
  "type": "Viewport",
  "tolerance": 0.01
}
```

#### 3. Element
Captures a specific element by CSS selector.

**Use Cases:**
- Buttons
- Headers
- Specific components
- Isolated sections

**Example:**
```json
{
  "name": "Header_Navigation",
  "type": "Element",
  "tolerance": 0.02,
  "selector": "#main-header"
}
```

#### 4. Region
Captures a rectangular area by coordinates.

**Use Cases:**
- Fixed position elements
- Specific areas without selectors
- Overlapping elements

**Example:**
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

### Adding Checkpoints to Tasks

#### Via API

**POST** `/api/tasks`

```json
{
  "name": "Homepage Visual Test",
  "description": "Visual regression test for homepage",
  "visualCheckpoints": [
    {
      "name": "HomePage_Header",
      "type": "Element",
      "tolerance": 0.01,
      "selector": "#header",
      "isRequired": true,
      "tags": ["homepage", "header"]
    },
    {
      "name": "HomePage_FullPage",
      "type": "FullPage",
      "tolerance": 0.02,
      "isRequired": true,
      "tags": ["homepage", "full-page"]
    }
  ]
}
```

#### Via Web UI

1. Navigate to **Tasks** ? **Create New Task**
2. Fill in task details
3. Click **Add Visual Checkpoint**
4. Configure checkpoint:
   - Enter unique name
   - Select checkpoint type
   - Set tolerance (default: 1%)
   - Add selector (for Element type)
   - Mark as required (optional)
5. Click **Save Task**

### Checkpoint Configuration

#### Tolerance

Tolerance determines how much visual difference is acceptable before failing:

- **0.001 (0.1%)**: Very strict - Only for static content
- **0.01 (1%)**: Strict - Default, good for most cases
- **0.02 (2%)**: Moderate - Allows minor rendering differences
- **0.05 (5%)**: Lenient - For dynamic or animated content
- **0.10 (10%)**: Very lenient - For highly dynamic content

**Recommendation:** Start with 1% and adjust based on results.

#### Ignore Regions

For dynamic content (timestamps, ads, counters), use ignore regions:

```json
{
  "name": "HomePage_WithoutAds",
  "type": "FullPage",
  "tolerance": 0.01,
  "ignoreSelectors": [".advertisement", ".timestamp", "#user-count"]
}
```

Ignored regions are excluded from comparison and shown as gray overlays in diff images.

#### Tags

Organize checkpoints with tags:

```json
{
  "tags": ["homepage", "header", "desktop", "critical"]
}
```

Use tags to:
- Filter checkpoints
- Organize test suites
- Track coverage

---

## Running Tests

### Automatic Execution

Visual regression runs automatically during task execution:

1. Task starts ? Planner creates execution plan
2. Browser navigates to target URL
3. At each checkpoint:
   - Screenshot captured
   - Compared to baseline (if exists)
   - Result recorded
4. Task completes ? Results available for review

### Manual Execution

#### Via API

**POST** `/api/tasks/{taskId}/execute`

```bash
curl -X POST https://localhost:7001/api/tasks/{taskId}/execute \
  -H "Content-Type: application/json" \
  -d '{
    "environment": "dev",
    "browser": "chromium",
    "viewport": "1920x1080"
  }'
```

#### Via Web UI

1. Navigate to **Tasks**
2. Find your task
3. Click **Execute**
4. Select environment (dev, staging, prod)
5. Click **Start Execution**

### Execution Environments

Separate baselines are maintained for each environment:

- **dev**: Development environment
- **staging**: Staging/QA environment
- **prod**: Production environment

This allows different visual states per environment (e.g., different data, features).

### Browser & Viewport

Separate baselines per configuration:

**Browsers:**
- chromium (default)
- firefox
- webkit (Safari)

**Viewports:**
- 1920x1080 (desktop)
- 1366x768 (laptop)
- 768x1024 (tablet)
- 375x667 (mobile)

---

## Reviewing Results

### Accessing the Viewer

1. Navigate to **Execution History**
2. Click on a completed execution
3. Scroll to **Visual Regression Results**
4. Click **View Details** for any checkpoint

Or directly:
```
https://localhost:7002/visual-regression/{taskId}/{checkpointName}
```

### Understanding the Viewer

#### Tabs

**1. Baseline**
- Approved reference image
- What the application should look like
- Green border indicates this is expected

**2. Actual**
- Screenshot from latest run
- What was actually captured
- Red border if differences detected

**3. Diff**
- Visual difference overlay
- Red highlights show changed areas
- Green shows baseline-only areas
- Intensity indicates difference magnitude

**4. Side-by-Side**
- Baseline and actual side-by-side
- Scroll synced between images
- Easy visual comparison

#### Metrics Dashboard

**Difference %**
- Percentage of pixels that differ
- Calculated: (pixels changed / total pixels) × 100
- Red if exceeds tolerance

**Tolerance**
- Configured acceptable difference
- Green if difference within tolerance
- Adjustable via "Adjust Tolerance" button

**SSIM Score**
- Structural Similarity Index (0.0 - 1.0)
- 1.0 = identical
- >0.95 = very similar
- <0.90 = significant structural differences
- Complements pixel comparison

**Pixels Different**
- Absolute count of changed pixels
- Out of total pixels
- Useful for understanding scope

#### Difference Regions

Detected regions are listed with:
- Position (X, Y)
- Size (Width × Height)
- Pixel count
- Change density

Click a region to:
- Highlight on diff image
- View detailed metrics
- Zoom to region
- Copy coordinates

---

## Managing Baselines

### First Run (Baseline Creation)

On first execution:
1. Screenshot captured
2. Saved as baseline
3. Comparison skipped (nothing to compare against)
4. Result marked as "Passed" (first run)

**Status:** "Baseline Created - First Run"

### Baseline Approval

When visual changes are detected:

1. Review the diff in Visual Regression Viewer
2. If change is legitimate (intentional design update):
   - Click **Approve as New Baseline**
   - Enter approval reason (required)
   - Confirm approval
3. New screenshot becomes the baseline
4. Future runs compare against new baseline

**Approval Reasons:**
- "Updated header design per #1234"
- "New color scheme approved"
- "Fixed layout issue"
- "Refreshed homepage content"

### Viewing Baseline History

#### Via API

**GET** `/api/visual/tasks/{taskId}/checkpoints/{checkpointName}/baseline`

Query parameters:
- `environment`: dev, staging, prod
- `browser`: chromium, firefox, webkit
- `viewport`: 1920x1080, etc.

#### Via Web UI

1. Go to **Visual Regression Viewer**
2. Click **History** tab
3. View timeline of all comparisons
4. Each entry shows:
   - Timestamp
   - Result (pass/fail)
   - Difference %
   - Link to view

### Baseline Versioning

Baselines are versioned by:
- **Task ID**: Different tasks have separate baselines
- **Checkpoint Name**: Each checkpoint has its own baseline
- **Environment**: dev, staging, prod
- **Browser**: chromium, firefox, webkit
- **Viewport**: Resolution (1920x1080, etc.)

**Example:**
- Task: "Homepage Test"
- Checkpoint: "Header"
- Environment: "prod"
- Browser: "chromium"
- Viewport: "1920x1080"

This creates a unique baseline key.

### Deleting Old Baselines

Baselines are retained indefinitely by default. To clean up:

#### Via API

**DELETE** `/api/visual/baselines/cleanup`

```bash
curl -X DELETE https://localhost:7001/api/visual/baselines/cleanup \
  -H "Content-Type: application/json" \
  -d '{
    "taskId": "guid",
    "retentionDays": 90
  }'
```

#### Automatic Cleanup

Configure retention in `appsettings.json`:

```json
{
  "VisualRegression": {
    "BaselineRetentionDays": 90,
    "AutoCleanup": true
  }
}
```

---

## Configuring Tolerance

### When to Adjust Tolerance

Adjust tolerance when:
- ? Consistent false positives (minor rendering differences)
- ? Dynamic content causes failures
- ? Font rendering varies across environments
- ? Anti-aliasing differences
- ? NOT for masking real issues

### Adjusting Tolerance

#### Via Visual Regression Viewer

1. Click **Adjust Tolerance** button
2. Use slider or presets:
   - Very Strict: 0.1%
   - Strict: 1%
   - Moderate: 2%
   - Lenient: 5%
3. See live preview (pass/fail)
4. Choose scope:
   - This checkpoint only
   - All checkpoints in task
5. Click **Apply**

#### Via API

**PUT** `/api/visual/tasks/{taskId}/checkpoints/{checkpointName}/tolerance`

```json
{
  "newTolerance": 0.02,
  "applyToAll": false,
  "reason": "Increased due to font rendering differences"
}
```

### Tolerance Recommendations

| Content Type | Recommended Tolerance |
|--------------|----------------------|
| Static images/logos | 0.1% - 0.5% |
| Text-heavy pages | 0.5% - 1% |
| Complex layouts | 1% - 2% |
| Dynamic content | 2% - 5% |
| Animated elements | 5% - 10% |

### Per-Checkpoint Tolerance

Different checkpoints can have different tolerances:

```json
{
  "visualCheckpoints": [
    {
      "name": "Logo",
      "type": "Element",
      "tolerance": 0.001,
      "selector": "#logo"
    },
    {
      "name": "Dashboard",
      "type": "FullPage",
      "tolerance": 0.02
    }
  ]
}
```

---

## Best Practices

### 1. Checkpoint Naming

? **Good Names:**
- `HomePage_Header`
- `Dashboard_UserProfile`
- `Checkout_PaymentForm`

? **Bad Names:**
- `test1`
- `screenshot`
- `check`

**Convention:** `{Page}_{Component}_{State?}`

### 2. Checkpoint Coverage

**Start Small:**
1. Critical user flows
2. Homepage and landing pages
3. Navigation components
4. Forms and inputs

**Expand Gradually:**
1. Dashboard views
2. Settings pages
3. Edge cases
4. Error states

### 3. Tolerance Strategy

**Start Strict, Loosen as Needed:**
1. Begin with 1% tolerance
2. Review false positives
3. Adjust specific checkpoints
4. Document reasons

### 4. Ignore Dynamic Content

Use ignore regions for:
- ? Timestamps
- ? User-specific data (username, profile picture)
- ? Counters (view counts, likes)
- ? Advertisements
- ? Loading indicators
- ? Real-time data (stock prices, weather)

### 5. Stabilization

Add wait conditions before screenshots:
- Wait for page load complete
- Wait for animations to finish
- Wait for fonts to load
- Wait for images to load

**Example:**
```csharp
await browser.WaitForLoadStateAsync(LoadState.NetworkIdle);
await Task.Delay(500); // Extra buffer
```

### 6. Environment Separation

Maintain separate baselines:
- **dev**: Frequent changes, loose tolerance
- **staging**: Pre-production validation
- **prod**: Strict tolerance, careful approval

### 7. Baseline Approval Workflow

**Process:**
1. Designer/Developer makes visual change
2. Run visual regression tests
3. Review diff with stakeholders
4. Approve with clear reason
5. Commit baseline to Git

**Approval Checklist:**
- [ ] Change is intentional
- [ ] Change matches design specs
- [ ] Change approved by designer/PM
- [ ] No unintended side effects
- [ ] Reason documented

### 8. Git Integration

**Commit baselines to version control:**

```bash
git add VisualRegressionData/baselines/
git commit -m "Update baseline for header redesign #1234"
git push
```

**Benefits:**
- Baseline history tracked
- Rollback capability
- Team collaboration
- CI/CD integration

### 9. Parallel Testing

Run tests in parallel for speed:
- Different checkpoints simultaneously
- Different browsers in parallel
- Different viewports in parallel

**Caution:** Ensure adequate system resources.

### 10. Monitoring & Alerts

Set up alerts for:
- High failure rate (>20%)
- Consistent failures on specific checkpoints
- Baseline approval frequency

---

## Troubleshooting

### Common Issues

#### 1. False Positives (Consistent Failures)

**Symptoms:**
- Tests fail but images look identical
- Small difference percentage (0.1% - 0.5%)
- Differences in text rendering

**Solutions:**
- Increase tolerance to 1-2%
- Add ignore regions for dynamic content
- Use consistent browser/viewport
- Ensure fonts are fully loaded
- Add stabilization wait time

#### 2. Flaky Tests

**Symptoms:**
- Tests pass sometimes, fail other times
- Differences in same areas
- Animation-related failures

**Solutions:**
- Wait for animations to complete
- Disable animations via CSS
- Add `wait` before screenshot
- Increase tolerance for animated areas
- Use ignore regions for animated content

#### 3. Baseline Not Found

**Symptoms:**
- Error: "No baseline found"
- Every run creates new baseline

**Solutions:**
- Check environment matches (dev/staging/prod)
- Check browser matches (chromium/firefox/webkit)
- Check viewport matches (1920x1080)
- Verify checkpoint name is correct
- Ensure baseline was approved

#### 4. Large Diff Images

**Symptoms:**
- Entire page highlighted in diff
- 100% difference reported

**Solutions:**
- Check if page loaded completely
- Verify correct URL
- Check for authentication issues
- Verify browser viewport
- Check for CSS loading errors

#### 5. Screenshot Timeout

**Symptoms:**
- Error: "Screenshot timed out"
- Page not loading

**Solutions:**
- Increase timeout in configuration
- Check network connectivity
- Verify URL is accessible
- Check for slow-loading resources
- Optimize page load performance

#### 6. Storage Space Issues

**Symptoms:**
- Error: "Insufficient disk space"
- Cannot save images

**Solutions:**
- Clean up old baselines
- Implement retention policy
- Use image compression
- Check storage configuration
- Increase disk space

#### 7. SSIM Score Confusing

**Symptoms:**
- SSIM high but pixel difference high
- Or vice versa

**Understanding:**
- **SSIM** = structural similarity (layout, composition)
- **Pixel %** = exact color matching
- Both metrics complement each other
- Use SSIM for layout changes
- Use pixel % for color/detail changes

### Error Messages

#### "Visual comparison service not configured"

**Cause:** IVisualComparisonService not registered

**Solution:** Ensure `AddEvoAITestCore()` is called in `Program.cs`

#### "Selector not found: #header"

**Cause:** CSS selector doesn't match any element

**Solution:**
- Verify selector is correct
- Check element is visible
- Wait for element to load
- Use browser DevTools to test selector

#### "Baseline approval failed"

**Cause:** Database or file system error

**Solution:**
- Check database connection
- Verify file storage permissions
- Check disk space
- Review logs for details

### Getting Help

**Check Logs:**
```bash
# View application logs
tail -f logs/evoaitest-{date}.log

# Filter for visual regression
grep "Visual" logs/evoaitest-{date}.log
```

**Enable Debug Logging:**

In `appsettings.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "EvoAITest.Core.Services.VisualComparisonService": "Debug",
      "EvoAITest.Core.Services.VisualComparisonEngine": "Debug"
    }
  }
}
```

**Support Channels:**
- GitHub Issues: [repo-url]/issues
- Documentation: [docs-url]
- Email: support@evoaitest.com

---

## Appendix

### Keyboard Shortcuts (Viewer)

| Key | Action |
|-----|--------|
| `1` | Show Baseline tab |
| `2` | Show Actual tab |
| `3` | Show Diff tab |
| `4` | Show Side-by-Side tab |
| `A` | Approve baseline |
| `T` | Adjust tolerance |
| `H` | View history |
| `ESC` | Close dialog |

### API Quick Reference

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/visual/tasks/{id}/checkpoints` | GET | List checkpoints |
| `/api/visual/comparisons/{id}` | GET | Get comparison |
| `/api/visual/tasks/{id}/checkpoints/{name}/history` | GET | Get history |
| `/api/visual/tasks/{id}/checkpoints/{name}/baseline` | GET | Get baseline |
| `/api/visual/tasks/{id}/checkpoints/{name}/tolerance` | PUT | Update tolerance |
| `/api/visual/images/{path}` | GET | Serve image |

See [API Documentation](VisualRegressionAPI.md) for details.

### File Structure

```
VisualRegressionData/
??? baselines/
?   ??? {taskId}/
?       ??? {checkpointName}_{hash}.png
??? actual/
?   ??? {taskId}/
?       ??? {checkpointName}_{timestamp}.png
??? diff/
    ??? {taskId}/
        ??? {checkpointName}_diff_{timestamp}.png
```

### Configuration Reference

```json
{
  "EvoAITestCore": {
    "StorageBasePath": "C:\\VisualRegressionData",
    "DefaultTolerance": 0.01,
    "DefaultBrowser": "chromium",
    "DefaultViewport": "1920x1080"
  },
  "VisualRegression": {
    "BaselineRetentionDays": 90,
    "AutoCleanup": true,
    "ComparisonTimeout": 30,
    "MaxDiffImageSize": 10485760
  }
}
```

---

**Version:** 1.0  
**Last Updated:** 2025-12-07  
**Feedback:** [support@evoaitest.com](mailto:support@evoaitest.com)
