using System.Security.Cryptography;
using EvoAITest.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Core.Services;

/// <summary>
/// Local filesystem implementation of file storage service for visual regression images.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly string _basePath;
    private readonly string _baseUrl;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalFileStorageService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="basePath">Base directory path for storing images. Defaults to "./visual-storage".</param>
    /// <param name="baseUrl">Base URL for accessing images. Defaults to "/visual-storage".</param>
    public LocalFileStorageService(
        ILogger<LocalFileStorageService> logger,
        string? basePath = null,
        string? baseUrl = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _basePath = basePath ?? Path.Combine(Directory.GetCurrentDirectory(), "visual-storage");
        _baseUrl = baseUrl ?? "/visual-storage";

        // Ensure base directory exists
        if (!Directory.Exists(_basePath))
        {
            Directory.CreateDirectory(_basePath);
            _logger.LogInformation("Created visual storage directory: {BasePath}", _basePath);
        }
    }

    /// <inheritdoc/>
    public async Task<string> SaveImageAsync(
        byte[] image,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        try
        {
            // Sanitize the relative path to prevent directory traversal
            var sanitizedPath = SanitizePath(relativePath);
            var fullPath = Path.Combine(_basePath, sanitizedPath);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Save the image
            await File.WriteAllBytesAsync(fullPath, image, cancellationToken);

            _logger.LogDebug("Saved image to {Path} ({Size} bytes)", fullPath, image.Length);

            return sanitizedPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save image to {Path}", relativePath);
            throw new InvalidOperationException($"Failed to save image to {relativePath}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<byte[]> LoadImageAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            var sanitizedPath = SanitizePath(path);
            var fullPath = Path.Combine(_basePath, sanitizedPath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Image not found at path: {path}");
            }

            var image = await File.ReadAllBytesAsync(fullPath, cancellationToken);

            _logger.LogDebug("Loaded image from {Path} ({Size} bytes)", fullPath, image.Length);

            return image;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load image from {Path}", path);
            throw new InvalidOperationException($"Failed to load image from {path}", ex);
        }
    }

    /// <inheritdoc/>
    public Task DeleteImageAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            var sanitizedPath = SanitizePath(path);
            var fullPath = Path.Combine(_basePath, sanitizedPath);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogDebug("Deleted image at {Path}", fullPath);
            }
            else
            {
                _logger.LogWarning("Attempted to delete non-existent image at {Path}", fullPath);
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image at {Path}", path);
            throw new InvalidOperationException($"Failed to delete image at {path}", ex);
        }
    }

    /// <inheritdoc/>
    public string GetImageUrl(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var sanitizedPath = SanitizePath(path);
        return $"{_baseUrl}/{sanitizedPath.Replace('\\', '/')}";
    }

    /// <inheritdoc/>
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var sanitizedPath = SanitizePath(path);
        var fullPath = Path.Combine(_basePath, sanitizedPath);
        return Task.FromResult(File.Exists(fullPath));
    }

    /// <summary>
    /// Sanitizes a path to prevent directory traversal attacks.
    /// </summary>
    private static string SanitizePath(string path)
    {
        // Remove any leading/trailing whitespace
        path = path.Trim();

        // Remove any absolute path indicators
        path = path.TrimStart('/', '\\');

        // Replace any directory traversal attempts
        path = path.Replace("..", string.Empty);

        // Normalize path separators
        path = path.Replace('/', Path.DirectorySeparatorChar);

        return path;
    }
}
