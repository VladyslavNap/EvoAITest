# Visual Regression Testing - Development Guide

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Core Components](#core-components)
3. [Comparison Algorithm](#comparison-algorithm)
4. [Storage Structure](#storage-structure)
5. [Extending the System](#extending-the-system)
6. [Custom Implementations](#custom-implementations)
7. [Testing](#testing)
8. [Performance Optimization](#performance-optimization)

---

## Architecture Overview

### High-Level Architecture

```
???????????????????????????????????????????????????????????????
?                        Blazor Web UI                        ?
?  ?????????????????????????????????????????????????????????  ?
?  ?  VisualRegressionViewer.razor                        ?  ?
?  ?  BaselineApprovalDialog.razor                        ?  ?
?  ?  ToleranceAdjustmentDialog.razor                     ?  ?
?  ?  DifferenceRegionOverlay.razor                       ?  ?
?  ?????????????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????????????
                         ? HTTP/REST API
???????????????????????????????????????????????????????????????
?                      API Service                            ?
?  ?????????????????????????????????????????????????????????  ?
?  ?  VisualRegressionController                          ?  ?
?  ?  ?? GET /tasks/{id}/checkpoints                     ?  ?
?  ?  ?? GET /checkpoints/{name}/history                 ?  ?
?  ?  ?? GET /comparisons/{id}                           ?  ?
?  ?  ?? PUT /checkpoints/{name}/tolerance               ?  ?
?  ?  ?? GET /images/{path}                              ?  ?
?  ?????????????????????????????????????????????????????????  ?
???????????????????????????????????????????????????????????????
                         ?
???????????????????????????????????????????????????????????????
?                      Core Services                          ?
?  ?????????????????????????????????????????????????????????? ?
?  ?  IVisualComparisonService                             ? ?
?  ?  ?? VisualComparisonService (Orchestration)          ? ?
?  ?     ?? VisualComparisonEngine (Comparison Logic)     ? ?
?  ?     ?? IFileStorageService (Image Storage)           ? ?
?  ?     ?? IAutomationTaskRepository (Database)          ? ?
?  ?????????????????????????????????????????????????????????? ?
?  ?????????????????????????????????????????????????????????? ?
?  ?  IToolExecutor                                         ? ?
?  ?  ?? DefaultToolExecutor                               ? ?
?  ?     ?? ExecuteVisualCheckAsync()                      ? ?
?  ?????????????????????????????????????????????????????????? ?
?  ?????????????????????????????????????????????????????????? ?
?  ?  IBrowserAgent                                         ? ?
?  ?  ?? PlaywrightBrowserAgent                            ? ?
?  ?     ?? TakeFullPageScreenshotBytesAsync()            ? ?
?  ?     ?? TakeViewportScreenshotAsync()                 ? ?
?  ?     ?? TakeElementScreenshotAsync()                  ? ?
?  ?     ?? TakeRegionScreenshotAsync()                   ? ?
?  ?????????????????????????????????????????????????????????? ?
???????????????????????????????????????????????????????????????
                         ?
???????????????????????????????????????????????????????????????
?                    Infrastructure                           ?
?  ????????????????  ????????????????  ????????????????????  ?
?  ?   Database   ?  ? File Storage ?  ?  Image Library   ?  ?
?  ?  (EF Core)   ?  ?   (Local)    ?  ?  (ImageSharp)    ?  ?
?  ????????????????  ????????????????  ????????????????????  ?
???????????????????????????????????????????????????????????????
```

### Layer Responsibilities

#### Presentation Layer (Web UI)
- Display visual comparison results
- Interactive image viewer
- Baseline approval workflow
- Tolerance adjustment
- Region highlighting

#### API Layer
- RESTful endpoints
- Request validation
- DTO mapping
- Authentication/authorization
- Rate limiting

#### Service Layer
- Business logic
- Workflow orchestration
- Image comparison
- Screenshot capture
- File management

#### Data Layer
- Database access
- Entity mapping
- Query optimization
- Migration management

---

## Core Components

### 1. VisualComparisonService

**Purpose:** Orchestrates the complete visual regression workflow.

**Location:** `EvoAITest.Core/Services/VisualComparisonService.cs`

**Responsibilities:**
- Coordinate baseline management
- Trigger image comparison
- Persist results to database
- Handle first-run scenarios
- Calculate image hashes

**Key Methods:**

```csharp
public interface IVisualComparisonService
{
    // Main comparison method
    Task<VisualComparisonResult> CompareAsync(
        VisualCheckpoint checkpoint,
        byte[] actualScreenshot,
        Guid taskId,
        string environment,
        string browser,
        string viewport,
        CancellationToken cancellationToken = default);
        
    // Get comparison history
    Task<List<VisualComparisonResult>> GetHistoryAsync(
        Guid taskId,
        string checkpointName,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
```

**Workflow:**

```
CompareAsync()
  ?
  ?? GetBaselineAsync() ? Retrieve existing baseline
  ?  ?
  ?  ?? If NOT found:
  ?  ?   ?? SaveBaselineAsync() ? Create new baseline (first run)
  ?  ?
  ?  ?? If found:
  ?      ?? VisualComparisonEngine.CompareImagesAsync()
  ?          ?? Pixel-by-pixel comparison
  ?          ?? SSIM calculation
  ?          ?? Region detection
  ?          ?? Diff generation
  ?
  ?? SaveImageAsync() ? Store actual screenshot
  ?
  ?? SaveImageAsync() ? Store diff image (if differences)
  ?
  ?? SaveComparisonResultAsync() ? Persist to database
```

### 2. VisualComparisonEngine

**Purpose:** Performs the actual image comparison using pixel-level and structural analysis.

**Location:** `EvoAITest.Core/Services/VisualComparisonEngine.cs`

**Responsibilities:**
- Load and decode images
- Pixel-by-pixel comparison
- SSIM (Structural Similarity Index) calculation
- Difference region detection
- Diff image generation

**Key Methods:**

```csharp
public sealed class VisualComparisonEngine
{
    // Main comparison method
    public ComparisonResult CompareImages(
        byte[] baselineImage,
        byte[] actualImage,
        VisualCheckpoint checkpoint,
        CancellationToken cancellationToken = default);
        
    // Generate diff image with highlights
    private Image<Rgba32> GenerateDiffImage(
        Image<Rgba32> baseline,
        Image<Rgba32> actual,
        bool[,] diffMask);
        
    // Detect contiguous difference regions
    private List<DifferenceRegion> DetectRegions(
        bool[,] diffMask,
        int width,
        int height);
}
```

**Comparison Algorithm:** See [Comparison Algorithm](#comparison-algorithm) section.

### 3. FileStorageService

**Purpose:** Abstracts file storage operations for baseline, actual, and diff images.

**Location:**
- Interface: `EvoAITest.Core/Abstractions/IFileStorageService.cs`
- Implementation: `EvoAITest.Core/Services/LocalFileStorageService.cs`

**Responsibilities:**
- Save images to storage
- Load images from storage
- Generate access URLs
- Check file existence
- Delete old files

**Key Methods:**

```csharp
public interface IFileStorageService
{
    Task<string> SaveImageAsync(
        byte[] image,
        string relativePath,
        CancellationToken cancellationToken = default);
        
    Task<byte[]> LoadImageAsync(
        string path,
        CancellationToken cancellationToken = default);
        
    Task<bool> FileExistsAsync(
        string path,
        CancellationToken cancellationToken = default);
        
    string GetImageUrl(string path);
}
```

**Storage Structure:** See [Storage Structure](#storage-structure) section.

### 4. AutomationTaskRepository

**Purpose:** Data access layer for visual regression entities.

**Location:** `EvoAITest.Core/Repositories/AutomationTaskRepository.cs`

**Responsibilities:**
- CRUD operations for baselines
- CRUD operations for comparison results
- Query optimization
- Relationship management

**Key Methods:**

```csharp
public interface IAutomationTaskRepository
{
    // Baseline operations
    Task<VisualBaseline?> GetBaselineAsync(...);
    Task<VisualBaseline> SaveBaselineAsync(VisualBaseline baseline, ...);
    Task<List<VisualBaseline>> GetBaselinesByTaskAsync(Guid taskId, ...);
    
    // Comparison operations
    Task<VisualComparisonResult> SaveComparisonResultAsync(...);
    Task<List<VisualComparisonResult>> GetComparisonHistoryAsync(...);
    Task<List<VisualComparisonResult>> GetFailedComparisonsAsync(...);
}
```

### 5. PlaywrightBrowserAgent

**Purpose:** Browser automation for screenshot capture.

**Location:** `EvoAITest.Core/Browser/PlaywrightBrowserAgent.cs`

**Responsibilities:**
- Playwright browser management
- Full page screenshots
- Viewport screenshots
- Element screenshots
- Region screenshots

**Key Methods:**

```csharp
public interface IBrowserAgent
{
    Task<byte[]> TakeFullPageScreenshotBytesAsync(
        CancellationToken cancellationToken = default);
        
    Task<byte[]> TakeViewportScreenshotAsync(
        CancellationToken cancellationToken = default);
        
    Task<byte[]> TakeElementScreenshotAsync(
        string selector,
        CancellationToken cancellationToken = default);
        
    Task<byte[]> TakeRegionScreenshotAsync(
        ScreenshotRegion region,
        CancellationToken cancellationToken = default);
}
```

---

## Comparison Algorithm

### Overview

The comparison algorithm uses two complementary techniques:

1. **Pixel-by-Pixel Comparison**: Detects exact color differences
2. **SSIM (Structural Similarity Index)**: Detects structural/layout differences

### Pixel Comparison

**Algorithm:**

```csharp
for (int y = 0; y < height; y++)
{
    for (int x = 0; x < width; x++)
    {
        var baselinePixel = baseline[x, y];
        var actualPixel = actual[x, y];
        
        // Calculate color distance
        var dr = baselinePixel.R - actualPixel.R;
        var dg = baselinePixel.G - actualPixel.G;
        var db = baselinePixel.B - actualPixel.B;
        
        var distance = Math.Sqrt(dr*dr + dg*dg + db*db);
        
        // Threshold: 10 out of 441 (sqrt(3 * 255^2))
        if (distance > 10.0)
        {
            pixelsDifferent++;
            diffMask[x, y] = true;
        }
    }
}

differencePercentage = (double)pixelsDifferent / totalPixels;
```

**Threshold Rationale:**
- Color space distance: Euclidean distance in RGB space
- Threshold of 10: Allows minor rendering differences (anti-aliasing)
- Maximum distance: 441 (?(3 × 255²))
- Threshold %: 2.3% of max distance

### SSIM Calculation

**Structural Similarity Index Measure (SSIM)** compares:
- Luminance (brightness)
- Contrast
- Structure

**Formula:**

```
SSIM(x, y) = [l(x, y)^? × c(x, y)^? × s(x, y)^?]

Where:
- l(x, y) = (2?x ?y + C1) / (?x² + ?y² + C1)  [luminance]
- c(x, y) = (2?x ?y + C2) / (?x² + ?y² + C2)  [contrast]
- s(x, y) = (?xy + C3) / (?x ?y + C3)         [structure]

Constants:
- C1 = (0.01 × 255)² = 6.5025
- C2 = (0.03 × 255)² = 58.5225
- C3 = C2 / 2
- ? = ? = ? = 1
```

**Implementation:**

```csharp
private double CalculateSSIM(Image<Rgba32> baseline, Image<Rgba32> actual)
{
    const int windowSize = 11;
    const double C1 = 6.5025;
    const double C2 = 58.5225;
    
    double totalSSIM = 0;
    int windowCount = 0;
    
    // Slide window across image
    for (int y = 0; y <= height - windowSize; y += windowSize)
    {
        for (int x = 0; x <= width - windowSize; x += windowSize)
        {
            // Calculate means
            double mean1 = CalculateMean(baseline, x, y, windowSize);
            double mean2 = CalculateMean(actual, x, y, windowSize);
            
            // Calculate variances and covariance
            double variance1 = CalculateVariance(baseline, x, y, windowSize, mean1);
            double variance2 = CalculateVariance(actual, x, y, windowSize, mean2);
            double covariance = CalculateCovariance(baseline, actual, x, y, windowSize, mean1, mean2);
            
            // Calculate SSIM for this window
            double luminance = (2 * mean1 * mean2 + C1) / (mean1 * mean1 + mean2 * mean2 + C1);
            double contrast = (2 * Math.Sqrt(variance1 * variance2) + C2) / (variance1 + variance2 + C2);
            double structure = (covariance + C2 / 2) / (Math.Sqrt(variance1 * variance2) + C2 / 2);
            
            totalSSIM += luminance * contrast * structure;
            windowCount++;
        }
    }
    
    return totalSSIM / windowCount; // Average SSIM score
}
```

**SSIM Score Interpretation:**
- **1.0**: Identical images
- **0.95 - 1.0**: Very similar (minor differences)
- **0.90 - 0.95**: Similar (noticeable differences)
- **0.80 - 0.90**: Different (significant differences)
- **< 0.80**: Very different

### Ignore Regions

Regions specified in `checkpoint.IgnoreSelectors` are excluded:

```csharp
private bool[,] CreateIgnoreMask(Image<Rgba32> image, List<string> ignoreSelectors)
{
    var mask = new bool[width, height];
    
    foreach (var selector in ignoreSelectors)
    {
        var element = await page.QuerySelectorAsync(selector);
        var box = await element.BoundingBoxAsync();
        
        // Mark region as ignored
        for (int y = box.Y; y < box.Y + box.Height; y++)
        {
            for (int x = box.X; x < box.X + box.Width; x++)
            {
                mask[x, y] = true;
            }
        }
    }
    
    return mask;
}
```

Ignored pixels are:
- Skipped during comparison
- Shown as gray in diff images
- Not counted in difference percentage

### Region Detection

**Algorithm:** Flood-fill to find contiguous difference regions.

```csharp
private List<DifferenceRegion> DetectRegions(bool[,] diffMask, int width, int height)
{
    var regions = new List<DifferenceRegion>();
    var visited = new bool[width, height];
    
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            if (diffMask[x, y] && !visited[x, y])
            {
                // Found new region, flood fill
                var region = FloodFill(diffMask, visited, x, y, width, height);
                
                if (region.PixelCount >= 100) // Minimum size threshold
                {
                    regions.Add(region);
                }
            }
        }
    }
    
    return regions.OrderByDescending(r => r.PixelCount).Take(50).ToList();
}

private DifferenceRegion FloodFill(bool[,] diffMask, bool[,] visited, 
                                    int startX, int startY, int width, int height)
{
    var queue = new Queue<(int x, int y)>();
    queue.Enqueue((startX, startY));
    visited[startX, startY] = true;
    
    int minX = startX, minY = startY;
    int maxX = startX, maxY = startY;
    int pixelCount = 0;
    
    while (queue.Count > 0)
    {
        var (x, y) = queue.Dequeue();
        pixelCount++;
        
        // Update bounding box
        minX = Math.Min(minX, x);
        minY = Math.Min(minY, y);
        maxX = Math.Max(maxX, x);
        maxY = Math.Max(maxY, y);
        
        // Check 4-connected neighbors
        foreach (var (dx, dy) in new[] { (-1, 0), (1, 0), (0, -1), (0, 1) })
        {
            int nx = x + dx, ny = y + dy;
            
            if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                diffMask[nx, ny] && !visited[nx, ny])
            {
                visited[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }
    }
    
    return new DifferenceRegion
    {
        X = minX,
        Y = minY,
        Width = maxX - minX + 1,
        Height = maxY - minY + 1,
        PixelCount = pixelCount,
        DifferenceScore = (double)pixelCount / ((maxX - minX + 1) * (maxY - minY + 1))
    };
}
```

**Region Properties:**
- **Position**: (X, Y) top-left corner
- **Size**: Width × Height
- **PixelCount**: Number of different pixels
- **DifferenceScore**: Density of changes (0.0 - 1.0)

### Diff Image Generation

```csharp
private Image<Rgba32> GenerateDiffImage(Image<Rgba32> baseline, Image<Rgba32> actual, bool[,] diffMask)
{
    var diff = actual.Clone();
    
    for (int y = 0; y < height; y++)
    {
        for (int x = 0; x < width; x++)
        {
            if (diffMask[x, y])
            {
                // Highlight difference in red
                var pixel = diff[x, y];
                diff[x, y] = new Rgba32(
                    (byte)Math.Min(255, pixel.R + 100),  // Increase red
                    (byte)(pixel.G * 0.5),                // Reduce green
                    (byte)(pixel.B * 0.5),                // Reduce blue
                    255
                );
            }
        }
    }
    
    return diff;
}
```

**Visual Encoding:**
- **Red overlay**: Changed pixels
- **Original colors**: Preserved underneath for context
- **Intensity**: Proportional to difference magnitude

---

## Storage Structure

### Directory Layout

```
{StorageBasePath}/
??? baselines/
?   ??? {taskId}/
?   ?   ??? {checkpointName}_{hash}.png
?   ?   ??? {checkpointName}_{hash}.png
?   ??? {taskId}/
?       ??? ...
??? actual/
?   ??? {taskId}/
?   ?   ??? {checkpointName}_{timestamp}.png
?   ?   ??? {checkpointName}_{timestamp}.png
?   ??? {taskId}/
?       ??? ...
??? diff/
    ??? {taskId}/
    ?   ??? {checkpointName}_diff_{timestamp}.png
    ?   ??? {checkpointName}_diff_{timestamp}.png
    ??? {taskId}/
        ??? ...
```

### File Naming Conventions

**Baselines:**
```
{checkpointName}_{imageHash}.png
```
- `checkpointName`: e.g., "HomePage_Header"
- `imageHash`: SHA256 hash (first 16 chars)
- Purpose: Version baselines by content

**Actual Screenshots:**
```
{checkpointName}_{timestamp}.png
```
- `checkpointName`: e.g., "HomePage_Header"
- `timestamp`: `yyyyMMdd_HHmmss`
- Purpose: Track when screenshot was captured

**Diff Images:**
```
{checkpointName}_diff_{timestamp}.png
```
- `checkpointName`: e.g., "HomePage_Header"
- `timestamp`: Matches actual screenshot
- Purpose: Link diff to specific comparison

### Database Schema

**VisualBaselines Table:**
```sql
CREATE TABLE VisualBaselines (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TaskId UNIQUEIDENTIFIER NOT NULL,
    CheckpointName NVARCHAR(255) NOT NULL,
    Environment NVARCHAR(50) NOT NULL,
    Browser NVARCHAR(50) NOT NULL,
    Viewport NVARCHAR(50) NOT NULL,
    BaselinePath NVARCHAR(500) NOT NULL,
    ImageHash NVARCHAR(100) NOT NULL,
    CreatedAt DATETIMEOFFSET NOT NULL,
    ApprovedBy NVARCHAR(255) NOT NULL,
    GitCommit NVARCHAR(100) NULL,
    GitBranch NVARCHAR(100) NULL,
    
    CONSTRAINT FK_VisualBaselines_Tasks 
        FOREIGN KEY (TaskId) REFERENCES AutomationTasks(Id) ON DELETE CASCADE,
    
    INDEX IX_VisualBaselines_TaskId (TaskId),
    INDEX IX_VisualBaselines_Configuration (TaskId, CheckpointName, Environment, Browser, Viewport)
);
```

**VisualComparisonResults Table:**
```sql
CREATE TABLE VisualComparisonResults (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TaskId UNIQUEIDENTIFIER NOT NULL,
    ExecutionHistoryId UNIQUEIDENTIFIER NOT NULL,
    BaselineId UNIQUEIDENTIFIER NULL,
    CheckpointName NVARCHAR(255) NOT NULL,
    BaselinePath NVARCHAR(500) NOT NULL,
    ActualPath NVARCHAR(500) NOT NULL,
    DiffPath NVARCHAR(500) NULL,
    DifferencePercentage FLOAT NOT NULL,
    Tolerance FLOAT NOT NULL,
    Passed BIT NOT NULL,
    PixelsDifferent INT NOT NULL,
    TotalPixels INT NOT NULL,
    SsimScore FLOAT NULL,
    DifferenceType NVARCHAR(100) NULL,
    Regions NVARCHAR(MAX) NULL,
    ComparedAt DATETIMEOFFSET NOT NULL,
    
    CONSTRAINT FK_VisualComparisonResults_Tasks 
        FOREIGN KEY (TaskId) REFERENCES AutomationTasks(Id) ON DELETE CASCADE,
    CONSTRAINT FK_VisualComparisonResults_History 
        FOREIGN KEY (ExecutionHistoryId) REFERENCES ExecutionHistory(Id),
    CONSTRAINT FK_VisualComparisonResults_Baselines 
        FOREIGN KEY (BaselineId) REFERENCES VisualBaselines(Id),
        
    INDEX IX_VisualComparisonResults_TaskCheckpoint (TaskId, CheckpointName),
    INDEX IX_VisualComparisonResults_Failed (TaskId, Passed),
    INDEX IX_VisualComparisonResults_ComparedAt (ComparedAt)
);
```

### Retention Policy

**Automatic Cleanup:**

Configure in `appsettings.json`:
```json
{
  "VisualRegression": {
    "BaselineRetentionDays": 90,
    "ActualRetentionDays": 30,
    "DiffRetentionDays": 30,
    "AutoCleanup": true,
    "CleanupSchedule": "0 2 * * *"  // Daily at 2 AM
  }
}
```

**Manual Cleanup:**
```csharp
await repository.DeleteOldBaselinesAsync(
    olderThan: DateTimeOffset.UtcNow.AddDays(-90),
    cancellationToken);
```

---

## Extending the System

### Adding New Checkpoint Types

**1. Define Type in Enum:**

```csharp
// EvoAITest.Core/Models/VisualCheckpoint.cs
public enum CheckpointType
{
    FullPage,
    Viewport,
    Element,
    Region,
    Scroll,        // NEW: Capture while scrolling
    AnimatedGif    // NEW: Capture animation sequence
}
```

**2. Implement Screenshot Logic:**

```csharp
// EvoAITest.Core/Browser/PlaywrightBrowserAgent.cs
public async Task<byte[]> TakeScrollScreenshotAsync(
    int scrollSteps,
    CancellationToken cancellationToken = default)
{
    var screenshots = new List<Image<Rgba32>>();
    var viewportHeight = await _page!.ViewportSize.Height;
    
    for (int i = 0; i < scrollSteps; i++)
    {
        var screenshot = await TakeViewportScreenshotAsync(cancellationToken);
        screenshots.Add(Image.Load<Rgba32>(screenshot));
        
        await _page.EvaluateAsync($"window.scrollBy(0, {viewportHeight})");
        await Task.Delay(500, cancellationToken); // Wait for content
    }
    
    // Stitch screenshots vertically
    return StitchImagesVertically(screenshots);
}
```

**3. Update ToolExecutor:**

```csharp
// EvoAITest.Core/Services/DefaultToolExecutor.cs
private async Task<byte[]> CaptureScreenshotAsync(VisualCheckpoint checkpoint)
{
    return checkpoint.Type switch
    {
        CheckpointType.FullPage => await _browserAgent.TakeFullPageScreenshotBytesAsync(_cts!.Token),
        CheckpointType.Viewport => await _browserAgent.TakeViewportScreenshotAsync(_cts!.Token),
        CheckpointType.Element => await _browserAgent.TakeElementScreenshotAsync(checkpoint.Selector!, _cts!.Token),
        CheckpointType.Region => await _browserAgent.TakeRegionScreenshotAsync(checkpoint.Region!, _cts!.Token),
        CheckpointType.Scroll => await _browserAgent.TakeScrollScreenshotAsync(5, _cts!.Token),  // NEW
        _ => throw new NotSupportedException($"Checkpoint type {checkpoint.Type} not supported")
    };
}
```

### Custom Comparison Algorithms

**1. Create Custom Engine:**

```csharp
public class PerceptualComparisonEngine : VisualComparisonEngine
{
    // Override comparison method
    public override ComparisonResult CompareImages(
        byte[] baselineImage,
        byte[] actualImage,
        VisualCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        using var baseline = Image.Load<Rgba32>(baselineImage);
        using var actual = Image.Load<Rgba32>(actualImage);
        
        // Use perceptual hash instead of pixel comparison
        var baselineHash = CalculatePerceptualHash(baseline);
        var actualHash = CalculatePerceptualHash(actual);
        
        var distance = HammingDistance(baselineHash, actualHash);
        var differencePercentage = distance / 64.0; // 64-bit hash
        
        return new ComparisonResult
        {
            Passed = differencePercentage <= checkpoint.Tolerance,
            DifferencePercentage = differencePercentage,
            SsimScore = null, // Not applicable
            DiffImage = null  // Not applicable
        };
    }
    
    private ulong CalculatePerceptualHash(Image<Rgba32> image)
    {
        // Resize to 8x8
        var resized = image.Clone(ctx => ctx.Resize(8, 8));
        
        // Convert to grayscale and calculate DCT
        // ... implementation ...
        
        return hash;
    }
}
```

**2. Register Custom Engine:**

```csharp
// Program.cs or ServiceCollectionExtensions.cs
services.AddScoped<VisualComparisonEngine, PerceptualComparisonEngine>();
```

### Custom Storage Providers

**1. Implement Interface:**

```csharp
public class AzureBlobStorageService : IFileStorageService
{
    private readonly BlobServiceClient _blobClient;
    private readonly string _containerName;
    
    public AzureBlobStorageService(BlobServiceClient blobClient, string containerName)
    {
        _blobClient = blobClient;
        _containerName = containerName;
    }
    
    public async Task<string> SaveImageAsync(
        byte[] image,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var container = _blobClient.GetBlobContainerClient(_containerName);
        var blobClient = container.GetBlobClient(relativePath);
        
        using var stream = new MemoryStream(image);
        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken);
        
        return blobClient.Uri.ToString();
    }
    
    public async Task<byte[]> LoadImageAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        var blobClient = new BlobClient(new Uri(path));
        
        using var ms = new MemoryStream();
        await blobClient.DownloadToAsync(ms, cancellationToken);
        
        return ms.ToArray();
    }
    
    // ... implement other methods ...
}
```

**2. Register Provider:**

```csharp
services.AddSingleton<IFileStorageService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config["AzureStorage:ConnectionString"];
    var containerName = config["AzureStorage:ContainerName"];
    
    var blobClient = new BlobServiceClient(connectionString);
    return new AzureBlobStorageService(blobClient, containerName);
});
```

### Custom Healing Strategies

**1. Define Strategy:**

```csharp
// EvoAITest.Agents/Models/HealingStrategy.cs
public enum HealingStrategyType
{
    // ... existing strategies ...
    SmartIgnoreRegion,  // NEW: AI-powered ignore region detection
}
```

**2. Implement Handler:**

```csharp
// EvoAITest.Agents/Agents/HealerAgent.cs
public async Task<HealingStrategy?> HealVisualRegressionAsync(...)
{
    // ... existing code ...
    
    if (strategy.Type == HealingStrategyType.SmartIgnoreRegion)
    {
        // Use computer vision to detect dynamic regions
        var dynamicRegions = await DetectDynamicRegionsAsync(comparison);
        
        strategy.Parameters["ignoreSelectors"] = dynamicRegions;
        strategy.Rationale = "AI detected dynamic content regions";
    }
    
    return strategy;
}

private async Task<List<string>> DetectDynamicRegionsAsync(
    VisualComparisonResult comparison)
{
    // Use AI/ML to analyze diff regions
    // Identify patterns (timestamps, counters, ads)
    // Return CSS selectors for ignore regions
    
    return selectors;
}
```

---

## Custom Implementations

### Example: PDF Comparison

**Scenario:** Compare PDF documents instead of web pages.

**Implementation:**

```csharp
public class PdfComparisonService : IVisualComparisonService
{
    private readonly VisualComparisonEngine _engine;
    private readonly IFileStorageService _storage;
    
    public async Task<VisualComparisonResult> ComparePdfAsync(
        byte[] baselinePdf,
        byte[] actualPdf,
        VisualCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        // Convert PDF pages to images
        var baselineImages = await ConvertPdfToImagesAsync(baselinePdf);
        var actualImages = await ConvertPdfToImagesAsync(actualPdf);
        
        // Compare each page
        var pageResults = new List<ComparisonResult>();
        for (int i = 0; i < Math.Min(baselineImages.Count, actualImages.Count); i++)
        {
            var result = _engine.CompareImages(
                baselineImages[i],
                actualImages[i],
                checkpoint,
                cancellationToken);
                
            pageResults.Add(result);
        }
        
        // Aggregate results
        var overallPassed = pageResults.All(r => r.Passed);
        var avgDifference = pageResults.Average(r => r.DifferencePercentage);
        
        return new VisualComparisonResult
        {
            Passed = overallPassed,
            DifferencePercentage = avgDifference,
            // ... other properties ...
        };
    }
    
    private async Task<List<byte[]>> ConvertPdfToImagesAsync(byte[] pdf)
    {
        // Use library like PdfiumViewer or Docnet.Core
        // Convert each page to PNG
        // Return list of image bytes
        
        var images = new List<byte[]>();
        // ... implementation ...
        return images;
    }
}
```

### Example: Mobile App Comparison

**Scenario:** Compare mobile app screenshots from Appium.

**Implementation:**

```csharp
public class AppiumBrowserAgent : IBrowserAgent
{
    private readonly AppiumDriver _driver;
    
    public async Task<byte[]> TakeFullPageScreenshotBytesAsync(
        CancellationToken cancellationToken = default)
    {
        // Appium screenshot
        var screenshot = _driver.GetScreenshot();
        return screenshot.AsByteArray;
    }
    
    public async Task<byte[]> TakeElementScreenshotAsync(
        string selector,
        CancellationToken cancellationToken = default)
    {
        // Find element by accessibility ID or XPath
        var element = _driver.FindElement(By.Id(selector));
        
        // Take screenshot
        var screenshot = element.GetScreenshot();
        return screenshot.AsByteArray;
    }
    
    // ... implement other methods ...
}
```

**Registration:**

```csharp
services.AddScoped<IBrowserAgent>(sp =>
{
    var appiumUrl = sp.GetRequiredService<IConfiguration>()["Appium:ServerUrl"];
    var capabilities = new AppiumOptions();
    // ... configure capabilities ...
    
    var driver = new AndroidDriver(new Uri(appiumUrl), capabilities);
    return new AppiumBrowserAgent(driver);
});
```

---

## Testing

### Unit Testing Guidelines

**Testing VisualComparisonEngine:**

```csharp
[TestMethod]
public void CompareImages_IdenticalImages_ReturnsZeroDifference()
{
    // Arrange
    var engine = new VisualComparisonEngine(Mock.Of<ILogger<VisualComparisonEngine>>());
    var image = CreateTestImage(100, 100, Color.Blue);
    var checkpoint = new VisualCheckpoint { Tolerance = 0.01 };
    
    // Act
    var result = engine.CompareImages(image, image, checkpoint, CancellationToken.None);
    
    // Assert
    result.Passed.Should().BeTrue();
    result.DifferencePercentage.Should().Be(0.0);
    result.PixelsDifferent.Should().Be(0);
}
```

**Testing VisualComparisonService:**

```csharp
[TestMethod]
public async Task CompareAsync_FirstRun_CreatesBaseline()
{
    // Arrange
    var mockRepository = new Mock<IAutomationTaskRepository>();
    mockRepository.Setup(r => r.GetBaselineAsync(It.IsAny<Guid>(), It.IsAny<string>(), 
        It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync((VisualBaseline?)null);  // No existing baseline
    
    var service = new VisualComparisonService(/* ... dependencies ... */);
    
    // Act
    var result = await service.CompareAsync(checkpoint, screenshot, taskId, "dev", "chromium", "1920x1080");
    
    // Assert
    result.Passed.Should().BeTrue();
    mockRepository.Verify(r => r.SaveBaselineAsync(It.IsAny<VisualBaseline>(), It.IsAny<CancellationToken>()), Times.Once);
}
```

### Integration Testing

See `EvoAITest.Tests/Integration/` for examples:
- `VisualRegressionApiTests.cs` - API endpoint tests
- `VisualRegressionWorkflowTests.cs` - End-to-end workflow tests
- `BrowserScreenshotIntegrationTests.cs` - Browser automation tests

### Performance Testing

**Benchmark Comparison Speed:**

```csharp
[Benchmark]
public void CompareImages_1920x1080()
{
    var engine = new VisualComparisonEngine(Mock.Of<ILogger<VisualComparisonEngine>>());
    var baseline = CreateTestImage(1920, 1080, Color.Blue);
    var actual = CreateTestImage(1920, 1080, Color.Blue);
    var checkpoint = new VisualCheckpoint { Tolerance = 0.01 };
    
    engine.CompareImages(baseline, actual, checkpoint, CancellationToken.None);
}
```

**Target Performance:**
- 1920×1080: <3 seconds
- 1366×768: <1.5 seconds
- 768×1024: <1 second

---

## Performance Optimization

### Image Comparison Optimization

**1. Parallel Processing:**

```csharp
Parallel.For(0, height, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, y =>
{
    for (int x = 0; x < width; x++)
    {
        // Pixel comparison logic
    }
});
```

**2. Early Exit:**

```csharp
if (pixelsDifferent > totalPixels * checkpoint.Tolerance)
{
    // Already exceeded tolerance, stop early
    break;
}
```

**3. Downsampling for SSIM:**

```csharp
// Resize to 25% for SSIM calculation (4x faster)
var baselineSmall = baseline.Clone(ctx => ctx.Resize(width / 2, height / 2));
var actualSmall = actual.Clone(ctx => ctx.Resize(width / 2, height / 2));

var ssimScore = CalculateSSIM(baselineSmall, actualSmall);
```

### Database Optimization

**1. Indexes:**

```sql
CREATE INDEX IX_VisualBaselines_Configuration 
ON VisualBaselines (TaskId, CheckpointName, Environment, Browser, Viewport);

CREATE INDEX IX_VisualComparisonResults_TaskCheckpoint 
ON VisualComparisonResults (TaskId, CheckpointName) 
INCLUDE (Passed, ComparedAt);
```

**2. Query Optimization:**

```csharp
// Use AsNoTracking for read-only queries
var history = await _context.VisualComparisonResults
    .AsNoTracking()
    .Where(r => r.TaskId == taskId && r.CheckpointName == checkpointName)
    .OrderByDescending(r => r.ComparedAt)
    .Take(limit)
    .ToListAsync(cancellationToken);
```

**3. Batch Operations:**

```csharp
// Save multiple results in one transaction
await _context.VisualComparisonResults.AddRangeAsync(results);
await _context.SaveChangesAsync();
```

### File Storage Optimization

**1. Compression:**

```csharp
using var output = new MemoryStream();
image.SaveAsPng(output, new PngEncoder
{
    CompressionLevel = PngCompressionLevel.BestCompression
});
```

**2. CDN Integration:**

```csharp
public class CdnFileStorageService : IFileStorageService
{
    public string GetImageUrl(string path)
    {
        // Return CDN URL instead of direct file URL
        return $"https://cdn.evoaitest.com/visual-regression/{path}";
    }
}
```

**3. Caching:**

```csharp
private readonly IMemoryCache _cache;

public async Task<byte[]> LoadImageAsync(string path, CancellationToken cancellationToken)
{
    return await _cache.GetOrCreateAsync(path, async entry =>
    {
        entry.SlidingExpiration = TimeSpan.FromMinutes(10);
        return await File.ReadAllBytesAsync(GetFullPath(path), cancellationToken);
    });
}
```

### Browser Automation Optimization

**1. Reuse Browser Context:**

```csharp
// Don't create new browser for each screenshot
private static IPlaywright? _playwright;
private static IBrowser? _browser;

public async Task InitializeAsync(CancellationToken cancellationToken)
{
    if (_playwright == null)
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }
    
    _page = await _browser.NewPageAsync();
}
```

**2. Disable Unnecessary Features:**

```csharp
var context = await browser.NewContextAsync(new()
{
    IgnoreHTTPSErrors = true,
    JavaScriptEnabled = true,
    HasTouch = false,
    IsMobile = false
});
```

**3. Wait Strategies:**

```csharp
// Wait for network idle instead of fixed delay
await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

// Wait for specific element instead of entire page
await page.WaitForSelectorAsync("img[src]", new() { State = WaitForSelectorState.Visible });
```

---

## Troubleshooting Development Issues

### Debugging Comparison Algorithm

**Enable Debug Logging:**

```csharp
_logger.LogDebug("Comparing images: baseline={BaselineSize}KB, actual={ActualSize}KB", 
    baselineImage.Length / 1024, actualImage.Length / 1024);
    
_logger.LogDebug("Difference: {DiffPercent:P2}, Pixels: {PixelsDiff}/{TotalPixels}", 
    diffPercentage, pixelsDifferent, totalPixels);
```

**Save Intermediate Images:**

```csharp
if (_logger.IsEnabled(LogLevel.Debug))
{
    diffImage.SaveAsPng($"debug_diff_{DateTime.Now:yyyyMMdd_HHmmss}.png");
}
```

### Memory Profiling

**Monitor Memory Usage:**

```csharp
var before = GC.GetTotalMemory(false);

// Comparison logic

var after = GC.GetTotalMemory(false);
_logger.LogInformation("Memory used: {MemoryMB:F2} MB", (after - before) / 1024.0 / 1024.0);
```

**Dispose Resources:**

```csharp
using var baseline = Image.Load<Rgba32>(baselineImage);
using var actual = Image.Load<Rgba32>(actualImage);
using var diff = GenerateDiffImage(baseline, actual, diffMask);

// Images automatically disposed
```

### Common Development Pitfalls

**1. Image Format Mismatches**

```csharp
// ? Good: Explicit format
image.SaveAsPng(stream);

// ? Bad: Auto-detect (can change)
image.Save(stream);
```

**2. Async/Await Issues**

```csharp
// ? Good: Proper async
await SaveImageAsync(image, path, cancellationToken);

// ? Bad: Blocking
SaveImageAsync(image, path, cancellationToken).Wait();
```

**3. Resource Leaks**

```csharp
// ? Good: Using statement
using var image = Image.Load<Rgba32>(bytes);

// ? Bad: No disposal
var image = Image.Load<Rgba32>(bytes);
```

---

## Additional Resources

- **ImageSharp Documentation**: https://docs.sixlabors.com/
- **Playwright .NET**: https://playwright.dev/dotnet/
- **Entity Framework Core**: https://docs.microsoft.com/ef/core/
- **SSIM Paper**: https://www.cns.nyu.edu/pub/lcv/wang03-preprint.pdf

---

**Version:** 1.0  
**Last Updated:** 2025-12-07  
**Contributors:** Development Team  
**License:** MIT
