# Phase 3.1: DefaultToolExecutor Extension for Visual Regression - Implementation Plan

## Status: ? **IN PROGRESS**

## What Needs to Be Implemented

### 1. Update IBrowserAgent Interface
Add new screenshot methods:
```csharp
Task<byte[]> TakeElementScreenshotAsync(string selector, CancellationToken cancellationToken = default);
Task<byte[]> TakeRegionScreenshotAsync(ScreenshotRegion region, CancellationToken cancellationToken = default);
Task<byte[]> TakeViewportScreenshotAsync(CancellationToken cancellationToken = default);
Task<byte[]> TakeFullPageScreenshotBytesAsync(CancellationToken cancellationToken = default);
```

### 2. Implement New Screenshot Methods in PlaywrightBrowserAgent
- Element screenshot: Capture specific element
- Region screenshot: Capture rectangular region  
- Viewport screenshot: Capture current view only
- Full page bytes: Return full page screenshot as bytes instead of base64

### 3. Add ExecuteVisualCheckAsync to DefaultToolExecutor
- Parse visual check parameters
- Create VisualCheckpoint
- Capture screenshot based on checkpoint type
- Call IVisualComparisonService.CompareAsync
- Store results in ToolExecutionContext
- Return success/failure based on comparison

### 4. Add visual_check to Tool Dispatch
Update ExecuteToolInternalAsync switch statement to handle "visual_check" tool

### 5. Inject IVisualComparisonService
Add IVisual ComparisonService to DefaultToolExecutor constructor

## Implementation Details

### Visual Check Parameters
```json
{
  "checkpoint_name": "HomePage_Header",
  "checkpoint_type": "fullpage|element|region|viewport",
  "selector": "header.main" // For element type
  "region": {  // For region type
    "x": 0,
    "y": 0,
    "width": 1920,
    "height": 200
  },
  "tolerance": 0.01,  // Optional, default 0.01
  "ignore_selectors": ["#timestamp", ".ad"], // Optional
  "create_baseline_if_missing": true // Optional, default false
}
```

### Flow Diagram
```
1. Parse parameters ? VisualCheckpoint
2. Determine screenshot type from checkpoint_type
3. Capture screenshot:
   - fullpage ? TakeFullPageScreenshotBytesAsync()
   - element ? TakeElementScreenshotAsync(selector)
   - region ? TakeRegionScreenshotAsync(region)
   - viewport ? TakeViewportScreenshotAsync()
4. Get environment/browser/viewport from context
5. Call visualService.CompareAsync(checkpoint, screenshot, taskId, env, browser, viewport)
6. Store VisualComparisonResult in context.VisualComparisonResults
7. Return ToolExecutionResult with passed/failed status
```

### Error Handling
- Missing checkpoint_name ? ArgumentException
- Invalid checkpoint_type ? ArgumentException
- Missing selector (for element type) ? ArgumentException
- Missing region (for region type) ? ArgumentException
- Screenshot capture failure ? Retry with transient error handling
- Comparison failure ? Return failed ToolExecutionResult with error details

## Files to Create/Modify

| File | Action | Purpose |
|------|--------|---------|
| IBrowserAgent.cs | Add methods | New screenshot APIs |
| PlaywrightBrowserAgent.cs | Implement | Screenshot implementations |
| DefaultToolExecutor.cs | Modify | Add visual check execution |
| ToolExecutionContext.cs | ? Created | Context for visual results |

## Dependencies

? **Ready:**
- VisualComparisonEngine
- VisualComparisonService
- IFileStorageService
- Database tables
- Repository methods
- ToolExecutionContext model

? **Needed:**
- Screenshot method implementations
- ExecuteVisualCheckAsync method
- Tool dispatch update
- Service injection

## Testing Plan

### Unit Tests
```csharp
[TestMethod]
public async Task ExecuteVisualCheck_WithFullPage_ComparesSuccessfully()
{
    // Arrange: Mock visual service, browser agent
    // Act: Execute visual_check tool
    // Assert: CompareAsync called, result stored in context
}

[TestMethod]
public async Task ExecuteVisualCheck_WithElement_CapturesElementScreenshot()
{
    // Arrange: Mock browser agent with element screenshot
    // Act: Execute visual_check with selector
    // Assert: TakeElementScreenshotAsync called with correct selector
}

[TestMethod]
public async Task ExecuteVisualCheck_WithMissingBaseline_CreatesBaseline()
{
    // Arrange: Mock visual service returns first-run result
    // Act: Execute visual_check
    // Assert: Baseline created, comparison passes
}
```

### Integration Tests
```csharp
[TestMethod]
public async Task VisualCheck_EndToEnd_WithRealBrowser()
{
    // Arrange: Real browser, real service
    // Act: Navigate, execute visual check
    // Assert: Comparison result saved to database
}
```

## Next Steps

1. ? Create ToolExecutionContext model
2. ? Add screenshot methods to IBrowserAgent
3. ? Implement screenshot methods in PlaywrightBrowserAgent
4. ? Add ExecuteVisualCheckAsync to DefaultToolExecutor
5. ? Update tool dispatch
6. ? Add service injection
7. ? Write unit tests
8. ? Write integration tests

## Estimated Completion

- Screenshot methods: 2-3 hours
- Visual check execution: 2-3 hours
- Testing: 2-3 hours
- **Total: 6-9 hours** (originally estimated 1 day)

---

**Status:** ? Implementation in progress  
**Next Action:** Add screenshot methods to IBrowserAgent and PlaywrightBrowserAgent
