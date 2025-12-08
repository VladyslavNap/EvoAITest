using System.Numerics;
using EvoAITest.Core.Models;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace EvoAITest.Core.Services;

/// <summary>
/// Core engine for comparing images and detecting visual differences.
/// Implements pixel-by-pixel comparison with SSIM enhancements.
/// </summary>
public sealed class VisualComparisonEngine
{
    private readonly ILogger<VisualComparisonEngine> _logger;
    
    /// <summary>
    /// Color distance threshold for pixel comparison (0.0 to 1.0).
    /// Default: 0.02 (2% color difference).
    /// </summary>
    private const double ColorDistanceThreshold = 0.02;

    public VisualComparisonEngine(ILogger<VisualComparisonEngine> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Compares two images and returns difference metrics.
    /// </summary>
    /// <param name="baselineImage">The baseline image bytes.</param>
    /// <param name="actualImage">The actual screenshot bytes.</param>
    /// <param name="checkpoint">The visual checkpoint configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Comparison metrics including difference percentage, SSIM score, and regions.</returns>
    public async Task<ComparisonMetrics> CompareImagesAsync(
        byte[] baselineImage,
        byte[] actualImage,
        VisualCheckpoint checkpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(baselineImage);
        ArgumentNullException.ThrowIfNull(actualImage);
        ArgumentNullException.ThrowIfNull(checkpoint);

        _logger.LogDebug("Starting image comparison for checkpoint '{CheckpointName}'", checkpoint.Name);

        try
        {
            // Step 1: Quick dimension check
            if (!AreDimensionsEqual(baselineImage, actualImage, out var dimensions))
            {
                _logger.LogWarning(
                    "Image dimensions do not match for checkpoint '{CheckpointName}'. " +
                    "Baseline: {BaselineWidth}x{BaselineHeight}, Actual: {ActualWidth}x{ActualHeight}",
                    checkpoint.Name, dimensions.BaselineWidth, dimensions.BaselineHeight,
                    dimensions.ActualWidth, dimensions.ActualHeight);

                return new ComparisonMetrics
                {
                    Passed = false,
                    DifferencePercentage = 1.0,
                    ErrorMessage = $"Image dimensions do not match. Baseline: {dimensions.BaselineWidth}x{dimensions.BaselineHeight}, " +
                                 $"Actual: {dimensions.ActualWidth}x{dimensions.ActualHeight}"
                };
            }

            // Step 2: Load images
            using var baselineImg = Image.Load<Rgba32>(baselineImage);
            using var actualImg = Image.Load<Rgba32>(actualImage);

            // Step 3: Apply ignore masks if specified
            if (checkpoint.IgnoreSelectors.Count > 0)
            {
                _logger.LogDebug("Applying {Count} ignore masks", checkpoint.IgnoreSelectors.Count);
                // Note: Actual mask application requires browser context for selector -> region mapping
                // For now, we'll skip this step and handle it in the service layer
            }

            // Step 4: Choose comparison algorithm based on checkpoint type
            var metrics = checkpoint.Type switch
            {
                CheckpointType.FullPage => await CompareFullPageAsync(baselineImg, actualImg, cancellationToken),
                CheckpointType.Element => await CompareElementAsync(baselineImg, actualImg, cancellationToken),
                CheckpointType.Region => await CompareRegionAsync(baselineImg, actualImg, checkpoint.Region, cancellationToken),
                CheckpointType.Viewport => await CompareViewportAsync(baselineImg, actualImg, cancellationToken),
                _ => throw new NotSupportedException($"Checkpoint type {checkpoint.Type} not supported")
            };

            // Step 5: Generate diff image
            metrics.DiffImage = GenerateDiffImage(baselineImg, actualImg, metrics.DifferenceMap);

            // Step 6: Apply tolerance
            metrics.Passed = metrics.DifferencePercentage <= checkpoint.Tolerance;

            _logger.LogInformation(
                "Comparison complete for checkpoint '{CheckpointName}'. " +
                "Difference: {DifferencePercentage:P2}, SSIM: {SsimScore:F4}, Passed: {Passed}",
                checkpoint.Name, metrics.DifferencePercentage, metrics.SsimScore, metrics.Passed);

            return metrics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing images for checkpoint '{CheckpointName}'", checkpoint.Name);
            
            return new ComparisonMetrics
            {
                Passed = false,
                DifferencePercentage = 1.0,
                ErrorMessage = $"Comparison failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Checks if two images have the same dimensions.
    /// </summary>
    private bool AreDimensionsEqual(
        byte[] image1,
        byte[] image2,
        out ImageDimensions dimensions)
    {
        try
        {
            var info1 = Image.Identify(image1);
            var info2 = Image.Identify(image2);

            dimensions = new ImageDimensions
            {
                BaselineWidth = info1.Width,
                BaselineHeight = info1.Height,
                ActualWidth = info2.Width,
                ActualHeight = info2.Height
            };

            return info1.Width == info2.Width && info1.Height == info2.Height;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error identifying image dimensions");
            dimensions = new ImageDimensions();
            return false;
        }
    }

    /// <summary>
    /// Pixel-by-pixel comparison with SSIM enhancements for full page screenshots.
    /// </summary>
    private async Task<ComparisonMetrics> CompareFullPageAsync(
        Image<Rgba32> baseline,
        Image<Rgba32> actual,
        CancellationToken cancellationToken)
    {
        var metrics = new ComparisonMetrics
        {
            TotalPixels = baseline.Width * baseline.Height,
            DifferenceMap = new bool[baseline.Width, baseline.Height]
        };

        int pixelsDifferent = 0;

        // Phase 1: Pixel-by-pixel comparison
        for (int y = 0; y < baseline.Height; y++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (int x = 0; x < baseline.Width; x++)
            {
                var baselinePixel = baseline[x, y];
                var actualPixel = actual[x, y];

                // Calculate color distance
                var distance = CalculateColorDistance(baselinePixel, actualPixel);

                if (distance > ColorDistanceThreshold)
                {
                    metrics.DifferenceMap[x, y] = true;
                    pixelsDifferent++;
                }
            }
        }

        metrics.PixelsDifferent = pixelsDifferent;
        metrics.DifferencePercentage = (double)pixelsDifferent / metrics.TotalPixels;

        // Phase 2: Structural Similarity (SSIM) for localized differences
        if (metrics.DifferencePercentage > 0.001) // Only if differences found
        {
            metrics.SsimScore = await Task.Run(() => CalculateSSIM(baseline, actual), cancellationToken);

            // If SSIM is high but pixel diff is moderate, might be anti-aliasing or font rendering
            if (metrics.SsimScore > 0.95 && metrics.DifferencePercentage < 0.05)
            {
                metrics.DifferenceType = DifferenceType.MinorRendering;
                _logger.LogDebug(
                    "Detected minor rendering differences. SSIM: {SsimScore:F4}, Diff: {DifferencePercentage:P2}",
                    metrics.SsimScore, metrics.DifferencePercentage);
            }
            else if (metrics.DifferencePercentage > 0.1)
            {
                metrics.DifferenceType = DifferenceType.ContentChange;
            }
        }
        else
        {
            metrics.SsimScore = 1.0; // Perfect match
            metrics.DifferenceType = DifferenceType.NoDifference;
        }

        // Phase 3: Identify contiguous difference regions
        metrics.Regions = IdentifyDifferenceRegions(metrics.DifferenceMap, baseline.Width, baseline.Height);

        return metrics;
    }

    /// <summary>
    /// Comparison optimized for element screenshots.
    /// </summary>
    private async Task<ComparisonMetrics> CompareElementAsync(
        Image<Rgba32> baseline,
        Image<Rgba32> actual,
        CancellationToken cancellationToken)
    {
        // For element screenshots, use the same logic as full page
        // but potentially with stricter thresholds
        return await CompareFullPageAsync(baseline, actual, cancellationToken);
    }

    /// <summary>
    /// Comparison for specific rectangular region.
    /// </summary>
    private async Task<ComparisonMetrics> CompareRegionAsync(
        Image<Rgba32> baseline,
        Image<Rgba32> actual,
        ScreenshotRegion? region,
        CancellationToken cancellationToken)
    {
        if (region == null)
        {
            return await CompareFullPageAsync(baseline, actual, cancellationToken);
        }

        // Crop both images to the specified region
        using var baselineCropped = baseline.Clone(ctx => ctx.Crop(
            new Rectangle(region.X, region.Y, region.Width, region.Height)));
        using var actualCropped = actual.Clone(ctx => ctx.Crop(
            new Rectangle(region.X, region.Y, region.Width, region.Height)));

        return await CompareFullPageAsync(baselineCropped, actualCropped, cancellationToken);
    }

    /// <summary>
    /// Comparison for viewport screenshots.
    /// </summary>
    private async Task<ComparisonMetrics> CompareViewportAsync(
        Image<Rgba32> baseline,
        Image<Rgba32> actual,
        CancellationToken cancellationToken)
    {
        // Viewport uses same logic as full page
        return await CompareFullPageAsync(baseline, actual, cancellationToken);
    }

    /// <summary>
    /// Calculates Euclidean distance between two colors (0.0 to 1.0).
    /// </summary>
    private double CalculateColorDistance(Rgba32 color1, Rgba32 color2)
    {
        var rDiff = Math.Pow(color1.R - color2.R, 2);
        var gDiff = Math.Pow(color1.G - color2.G, 2);
        var bDiff = Math.Pow(color1.B - color2.B, 2);
        var aDiff = Math.Pow(color1.A - color2.A, 2);

        return Math.Sqrt((rDiff + gDiff + bDiff + aDiff) / 4) / 255.0;
    }

    /// <summary>
    /// Calculates Structural Similarity Index (SSIM) between two images.
    /// Returns a value between -1 and 1, where 1 means identical.
    /// </summary>
    private double CalculateSSIM(Image<Rgba32> baseline, Image<Rgba32> actual)
    {
        const double k1 = 0.01;
        const double k2 = 0.03;
        const double L = 255; // Dynamic range

        var c1 = Math.Pow(k1 * L, 2);
        var c2 = Math.Pow(k2 * L, 2);

        // Calculate means
        var mean1 = CalculateMean(baseline);
        var mean2 = CalculateMean(actual);

        // Calculate variances and covariance
        var (variance1, variance2, covariance) = CalculateVariancesAndCovariance(
            baseline, actual, mean1, mean2);

        // SSIM formula
        var numerator = (2 * mean1 * mean2 + c1) * (2 * covariance + c2);
        var denominator = (mean1 * mean1 + mean2 * mean2 + c1) * (variance1 + variance2 + c2);

        return numerator / denominator;
    }

    /// <summary>
    /// Calculates the mean brightness of an image.
    /// </summary>
    private double CalculateMean(Image<Rgba32> image)
    {
        double sum = 0;
        int count = 0;

        for (int y = 0; y < image.Height; y++)
        {
            for (int x = 0; x < image.Width; x++)
            {
                var pixel = image[x, y];
                // Convert to grayscale using standard formula
                sum += 0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B;
                count++;
            }
        }

        return sum / count;
    }

    /// <summary>
    /// Calculates variances and covariance for SSIM calculation.
    /// </summary>
    private (double variance1, double variance2, double covariance) CalculateVariancesAndCovariance(
        Image<Rgba32> baseline,
        Image<Rgba32> actual,
        double mean1,
        double mean2)
    {
        double variance1 = 0;
        double variance2 = 0;
        double covariance = 0;
        int count = 0;

        for (int y = 0; y < baseline.Height; y++)
        {
            for (int x = 0; x < baseline.Width; x++)
            {
                var pixel1 = baseline[x, y];
                var pixel2 = actual[x, y];

                // Convert to grayscale
                var gray1 = 0.299 * pixel1.R + 0.587 * pixel1.G + 0.114 * pixel1.B;
                var gray2 = 0.299 * pixel2.R + 0.587 * pixel2.G + 0.114 * pixel2.B;

                var diff1 = gray1 - mean1;
                var diff2 = gray2 - mean2;

                variance1 += diff1 * diff1;
                variance2 += diff2 * diff2;
                covariance += diff1 * diff2;
                count++;
            }
        }

        return (variance1 / count, variance2 / count, covariance / count);
    }

    /// <summary>
    /// Identifies contiguous regions where differences were detected.
    /// Uses connected component labeling to find difference clusters.
    /// </summary>
    private List<DifferenceRegion> IdentifyDifferenceRegions(
        bool[,] differenceMap,
        int width,
        int height)
    {
        var regions = new List<DifferenceRegion>();
        var visited = new bool[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (differenceMap[x, y] && !visited[x, y])
                {
                    // Start flood fill to find connected region
                    var region = FloodFillRegion(differenceMap, visited, x, y, width, height);
                    
                    // Only include regions larger than a minimum size (100 pixels)
                    if (region.Width * region.Height >= 100)
                    {
                        regions.Add(region);
                    }
                }
            }
        }

        _logger.LogDebug("Identified {RegionCount} difference regions", regions.Count);
        return regions;
    }

    /// <summary>
    /// Flood fill algorithm to identify a contiguous difference region.
    /// </summary>
    private DifferenceRegion FloodFillRegion(
        bool[,] differenceMap,
        bool[,] visited,
        int startX,
        int startY,
        int width,
        int height)
    {
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((startX, startY));

        int minX = startX, maxX = startX;
        int minY = startY, maxY = startY;
        int pixelCount = 0;

        while (queue.Count > 0)
        {
            var (x, y) = queue.Dequeue();

            if (x < 0 || x >= width || y < 0 || y >= height)
                continue;

            if (visited[x, y] || !differenceMap[x, y])
                continue;

            visited[x, y] = true;
            pixelCount++;

            // Update bounding box
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x);
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y);

            // Check 4-connected neighbors
            queue.Enqueue((x + 1, y));
            queue.Enqueue((x - 1, y));
            queue.Enqueue((x, y + 1));
            queue.Enqueue((x, y - 1));
        }

        var regionWidth = maxX - minX + 1;
        var regionHeight = maxY - minY + 1;
        var regionArea = regionWidth * regionHeight;

        return new DifferenceRegion
        {
            X = minX,
            Y = minY,
            Width = regionWidth,
            Height = regionHeight,
            DifferenceScore = regionArea > 0 ? (double)pixelCount / regionArea : 0
        };
    }

    /// <summary>
    /// Generates a diff image highlighting differences between baseline and actual.
    /// Differences are shown in red, matching areas in grayscale.
    /// </summary>
    private byte[] GenerateDiffImage(
        Image<Rgba32> baseline,
        Image<Rgba32> actual,
        bool[,] differenceMap)
    {
        using var diffImage = new Image<Rgba32>(baseline.Width, baseline.Height);

        for (int y = 0; y < baseline.Height; y++)
        {
            for (int x = 0; x < baseline.Width; x++)
            {
                if (differenceMap[x, y])
                {
                    // Highlight differences in red
                    diffImage[x, y] = new Rgba32(255, 0, 0, 255);
                }
                else
                {
                    // Show matching pixels in grayscale
                    var pixel = actual[x, y];
                    var gray = (byte)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                    diffImage[x, y] = new Rgba32(gray, gray, gray, 255);
                }
            }
        }

        using var ms = new MemoryStream();
        diffImage.SaveAsPng(ms);
        return ms.ToArray();
    }
}

/// <summary>
/// Metrics resulting from an image comparison.
/// </summary>
public sealed class ComparisonMetrics
{
    /// <summary>Gets or sets whether the comparison passed within tolerance.</summary>
    public bool Passed { get; set; }

    /// <summary>Gets or sets the difference percentage (0.0 to 1.0).</summary>
    public double DifferencePercentage { get; set; }

    /// <summary>Gets or sets the SSIM score (0.0 to 1.0, where 1.0 is perfect match).</summary>
    public double SsimScore { get; set; }

    /// <summary>Gets or sets the number of pixels that differ.</summary>
    public int PixelsDifferent { get; set; }

    /// <summary>Gets or sets the total number of pixels compared.</summary>
    public int TotalPixels { get; set; }

    /// <summary>Gets or sets the 2D map of pixel differences.</summary>
    public bool[,] DifferenceMap { get; set; } = new bool[0, 0];

    /// <summary>Gets or sets the diff image bytes (PNG format).</summary>
    public byte[]? DiffImage { get; set; }

    /// <summary>Gets or sets the list of contiguous difference regions.</summary>
    public List<DifferenceRegion> Regions { get; set; } = new();

    /// <summary>Gets or sets the type of difference detected.</summary>
    public DifferenceType DifferenceType { get; set; }

    /// <summary>Gets or sets the error message if comparison failed.</summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Image dimension information.
/// </summary>
internal sealed class ImageDimensions
{
    public int BaselineWidth { get; set; }
    public int BaselineHeight { get; set; }
    public int ActualWidth { get; set; }
    public int ActualHeight { get; set; }
}
