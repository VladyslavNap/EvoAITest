using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.SelfHealing;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Security.Cryptography;

namespace EvoAITest.Core.Services;

/// <summary>
/// Service for matching elements visually using screenshot comparison and perceptual hashing.
/// </summary>
public sealed class VisualElementMatcher : IDisposable
{
    private readonly ILogger<VisualElementMatcher> _logger;
    private bool _disposed;

    public VisualElementMatcher(ILogger<VisualElementMatcher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Compares two screenshots and returns a similarity score using SSIM (Structural Similarity Index).
    /// </summary>
    /// <param name="screenshot1">First screenshot as byte array.</param>
    /// <param name="screenshot2">Second screenshot as byte array.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Similarity score between 0.0 (completely different) and 1.0 (identical).</returns>
    public async Task<double> CalculateSimilarityAsync(
        byte[] screenshot1,
        byte[] screenshot2,
        CancellationToken cancellationToken = default)
    {
        if (screenshot1 == null || screenshot1.Length == 0)
            throw new ArgumentException("Screenshot1 cannot be null or empty", nameof(screenshot1));
        if (screenshot2 == null || screenshot2.Length == 0)
            throw new ArgumentException("Screenshot2 cannot be null or empty", nameof(screenshot2));

        try
        {
            using var ms1 = new MemoryStream(screenshot1);
            using var ms2 = new MemoryStream(screenshot2);
            using var image1 = await Image.LoadAsync<Rgba32>(ms1, cancellationToken);
            using var image2 = await Image.LoadAsync<Rgba32>(ms2, cancellationToken);

            // Resize images to same dimensions if different
            if (image1.Width != image2.Width || image1.Height != image2.Height)
            {
                var targetWidth = Math.Min(image1.Width, image2.Width);
                var targetHeight = Math.Min(image1.Height, image2.Height);
                
                image1.Mutate(x => x.Resize(targetWidth, targetHeight));
                image2.Mutate(x => x.Resize(targetWidth, targetHeight));
            }

            // Calculate SSIM
            var ssim = CalculateSSIM(image1, image2);
            
            _logger.LogDebug("Visual similarity calculated: {Similarity}", ssim);
            return ssim;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating visual similarity");
            return 0.0;
        }
    }

    /// <summary>
    /// Calculates a perceptual hash for an image.
    /// Similar images will have similar hashes.
    /// </summary>
    /// <param name="screenshot">The screenshot to hash.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A perceptual hash as a byte array.</returns>
    public async Task<byte[]> CalculatePerceptualHashAsync(
        byte[] screenshot,
        CancellationToken cancellationToken = default)
    {
        if (screenshot == null || screenshot.Length == 0)
            throw new ArgumentException("Screenshot cannot be null or empty", nameof(screenshot));

        try
        {
            using var ms = new MemoryStream(screenshot);
            using var image = await Image.LoadAsync<Rgba32>(ms, cancellationToken);
            
            // Resize to 8x8 for pHash
            image.Mutate(x => x.Resize(8, 8).Grayscale());

            // Calculate average pixel value
            var pixels = new byte[64];
            var sum = 0.0;
            
            for (int y = 0; y < 8; y++)
            {
                for (int x = 0; x < 8; x++)
                {
                    var pixel = image[x, y];
                    var gray = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                    pixels[y * 8 + x] = gray;
                    sum += gray;
                }
            }

            var average = sum / 64;

            // Create hash based on average
            var hash = new byte[8];
            for (int i = 0; i < 64; i++)
            {
                if (pixels[i] >= average)
                {
                    hash[i / 8] |= (byte)(1 << (7 - (i % 8)));
                }
            }

            return hash;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating perceptual hash");
            return Array.Empty<byte>();
        }
    }

    /// <summary>
    /// Compares two perceptual hashes and returns a similarity score.
    /// </summary>
    /// <param name="hash1">First perceptual hash.</param>
    /// <param name="hash2">Second perceptual hash.</param>
    /// <returns>Similarity score between 0.0 and 1.0.</returns>
    public double ComparePerceptualHashes(byte[] hash1, byte[] hash2)
    {
        if (hash1 == null || hash1.Length == 0 || hash2 == null || hash2.Length == 0)
            return 0.0;

        if (hash1.Length != hash2.Length)
            return 0.0;

        // Calculate Hamming distance
        int hammingDistance = 0;
        for (int i = 0; i < hash1.Length; i++)
        {
            byte xor = (byte)(hash1[i] ^ hash2[i]);
            hammingDistance += CountBits(xor);
        }

        // Convert to similarity score (max distance is 64 bits)
        var maxDistance = hash1.Length * 8;
        var similarity = 1.0 - ((double)hammingDistance / maxDistance);
        
        return Math.Max(0.0, similarity);
    }

    /// <summary>
    /// Finds the best matching element based on visual similarity.
    /// </summary>
    /// <param name="expectedScreenshot">Screenshot of the expected element.</param>
    /// <param name="elements">List of candidate elements with their visual data.</param>
    /// <param name="threshold">Minimum similarity threshold (0.0 to 1.0).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The best matching candidate or null if none meet the threshold.</returns>
    public async Task<SelectorCandidate?> FindBestVisualMatchAsync(
        byte[] expectedScreenshot,
        List<ElementInfo> elements,
        double threshold = 0.7,
        CancellationToken cancellationToken = default)
    {
        if (expectedScreenshot == null || expectedScreenshot.Length == 0)
            return null;

        if (elements == null || elements.Count == 0)
            return null;

        SelectorCandidate? bestMatch = null;
        double bestScore = threshold;

        try
        {
            var expectedHash = await CalculatePerceptualHashAsync(expectedScreenshot, cancellationToken);

            foreach (var element in elements.Where(e => e.IsVisible))
            {
                // For now, use perceptual hash comparison
                // In a full implementation, you would capture element screenshots
                // and compare them directly
                
                // Placeholder: Use text similarity as a proxy for visual similarity
                var textScore = element.Text != null ? 
                    CalculateTextSimilarity(expectedScreenshot.ToString(), element.Text) : 0.0;

                if (textScore > bestScore)
                {
                    bestScore = textScore;
                    bestMatch = new SelectorCandidate
                    {
                        Selector = element.Selector,
                        Strategy = HealingStrategy.VisualSimilarity,
                        BaseConfidence = textScore,
                        VisualSimilarityScore = textScore,
                        ElementInfo = element,
                        ElementText = element.Text
                    };
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding best visual match");
        }

        return bestMatch;
    }

    /// <summary>
    /// Checks if two elements are visually similar based on their bounding boxes.
    /// </summary>
    /// <param name="box1">First bounding box.</param>
    /// <param name="box2">Second bounding box.</param>
    /// <param name="tolerance">Position tolerance in pixels.</param>
    /// <returns>True if elements are in similar positions.</returns>
    public bool ArePositionsSimilar(BoundingBox box1, BoundingBox box2, double tolerance = 50.0)
    {
        if (box1 == null || box2 == null)
            return false;

        var xDiff = Math.Abs(box1.X - box2.X);
        var yDiff = Math.Abs(box1.Y - box2.Y);
        var widthDiff = Math.Abs(box1.Width - box2.Width);
        var heightDiff = Math.Abs(box1.Height - box2.Height);

        return xDiff <= tolerance && 
               yDiff <= tolerance && 
               widthDiff <= tolerance && 
               heightDiff <= tolerance;
    }

    /// <summary>
    /// Calculates SSIM (Structural Similarity Index) between two images.
    /// </summary>
    private double CalculateSSIM(Image<Rgba32> image1, Image<Rgba32> image2)
    {
        // Simplified SSIM calculation
        // Full implementation would use luminance, contrast, and structure comparisons
        
        var width = Math.Min(image1.Width, image2.Width);
        var height = Math.Min(image1.Height, image2.Height);

        double sum1 = 0, sum2 = 0, sumSquared1 = 0, sumSquared2 = 0, sumProduct = 0;
        int count = width * height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var pixel1 = image1[x, y];
                var pixel2 = image2[x, y];

                var gray1 = (pixel1.R + pixel1.G + pixel1.B) / 3.0;
                var gray2 = (pixel2.R + pixel2.G + pixel2.B) / 3.0;

                sum1 += gray1;
                sum2 += gray2;
                sumSquared1 += gray1 * gray1;
                sumSquared2 += gray2 * gray2;
                sumProduct += gray1 * gray2;
            }
        }

        var mean1 = sum1 / count;
        var mean2 = sum2 / count;
        var variance1 = (sumSquared1 / count) - (mean1 * mean1);
        var variance2 = (sumSquared2 / count) - (mean2 * mean2);
        var covariance = (sumProduct / count) - (mean1 * mean2);

        // SSIM constants
        const double c1 = 6.5025;
        const double c2 = 58.5225;

        var numerator = (2 * mean1 * mean2 + c1) * (2 * covariance + c2);
        var denominator = (mean1 * mean1 + mean2 * mean2 + c1) * (variance1 + variance2 + c2);

        return denominator != 0 ? numerator / denominator : 0.0;
    }

    /// <summary>
    /// Calculates text similarity using Levenshtein distance.
    /// </summary>
    private double CalculateTextSimilarity(string text1, string text2)
    {
        if (string.IsNullOrEmpty(text1) || string.IsNullOrEmpty(text2))
            return 0.0;

        text1 = text1.ToLowerInvariant().Trim();
        text2 = text2.ToLowerInvariant().Trim();

        if (text1 == text2)
            return 1.0;

        var distance = LevenshteinDistance(text1, text2);
        var maxLength = Math.Max(text1.Length, text2.Length);
        
        return maxLength > 0 ? 1.0 - ((double)distance / maxLength) : 0.0;
    }

    /// <summary>
    /// Calculates Levenshtein distance between two strings.
    /// </summary>
    private int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= s2.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= s1.Length; i++)
        {
            for (int j = 1; j <= s2.Length; j++)
            {
                var cost = s1[i - 1] == s2[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[s1.Length, s2.Length];
    }

    /// <summary>
    /// Counts the number of set bits in a byte.
    /// </summary>
    private int CountBits(byte b)
    {
        int count = 0;
        while (b != 0)
        {
            count += b & 1;
            b >>= 1;
        }
        return count;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
    }
}
