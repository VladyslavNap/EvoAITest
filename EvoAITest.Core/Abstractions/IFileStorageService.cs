namespace EvoAITest.Core.Abstractions;

/// <summary>
/// Service for storing and retrieving image files.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Saves an image to storage and returns the storage path.
    /// </summary>
    /// <param name="image">The image bytes to save.</param>
    /// <param name="relativePath">The relative path where the image should be stored.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The full storage path or URL of the saved image.</returns>
    Task<string> SaveImageAsync(byte[] image, string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an image from storage.
    /// </summary>
    /// <param name="path">The path or URL of the image to load.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The image bytes.</returns>
    Task<byte[]> LoadImageAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an image from storage.
    /// </summary>
    /// <param name="path">The path or URL of the image to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteImageAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a public URL for accessing an image.
    /// </summary>
    /// <param name="path">The storage path of the image.</param>
    /// <returns>The public URL for accessing the image.</returns>
    string GetImageUrl(string path);

    /// <summary>
    /// Checks if an image exists in storage.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the image exists; otherwise, false.</returns>
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in storage.
    /// </summary>
    Task<bool> FileExistsAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a file from storage.
    /// </summary>
    Task<byte[]> ReadFileAsync(string path, CancellationToken cancellationToken = default);
}
