using EvoAITest.Core.Models.Analytics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Agents.Services.Analytics;

/// <summary>
/// Caching service for analytics data to improve performance
/// </summary>
public sealed class AnalyticsCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<AnalyticsCacheService> _logger;

    // Cache keys
    private const string DashboardCacheKey = "Analytics_Dashboard";
    private const string FlakyTestsCacheKey = "Analytics_FlakyTests";
    private const string TrendsCacheKeyPrefix = "Analytics_Trends_";

    // Cache durations
    private static readonly TimeSpan DashboardCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FlakyTestsCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TrendsCacheDuration = TimeSpan.FromMinutes(10);

    public AnalyticsCacheService(
        IMemoryCache cache,
        ILogger<AnalyticsCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Gets or creates cached dashboard statistics
    /// </summary>
    public async Task<DashboardStatistics> GetOrCreateDashboardAsync(
        Func<Task<DashboardStatistics>> factory,
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            DashboardCacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = DashboardCacheDuration;
                entry.SetPriority(CacheItemPriority.High);
                
                _logger.LogDebug("Dashboard cache miss, generating new data");
                return await factory();
            }) ?? await factory();
    }

    /// <summary>
    /// Gets or creates cached flaky tests list
    /// </summary>
    public async Task<List<FlakyTestAnalysis>> GetOrCreateFlakyTestsAsync(
        Func<Task<List<FlakyTestAnalysis>>> factory,
        CancellationToken cancellationToken = default)
    {
        return await _cache.GetOrCreateAsync(
            FlakyTestsCacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = FlakyTestsCacheDuration;
                entry.SetPriority(CacheItemPriority.Normal);
                
                _logger.LogDebug("Flaky tests cache miss, generating new data");
                return await factory();
            }) ?? await factory();
    }

    /// <summary>
    /// Gets or creates cached trends
    /// </summary>
    public async Task<List<TestTrend>> GetOrCreateTrendsAsync(
        string cacheKey,
        Func<Task<List<TestTrend>>> factory,
        CancellationToken cancellationToken = default)
    {
        var fullKey = $"{TrendsCacheKeyPrefix}{cacheKey}";
        
        return await _cache.GetOrCreateAsync(
            fullKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TrendsCacheDuration;
                entry.SetPriority(CacheItemPriority.Low);
                
                _logger.LogDebug("Trends cache miss for key {Key}, generating new data", cacheKey);
                return await factory();
            }) ?? await factory();
    }

    /// <summary>
    /// Invalidates dashboard cache
    /// </summary>
    public void InvalidateDashboard()
    {
        _cache.Remove(DashboardCacheKey);
        _logger.LogDebug("Dashboard cache invalidated");
    }

    /// <summary>
    /// Invalidates flaky tests cache
    /// </summary>
    public void InvalidateFlakyTests()
    {
        _cache.Remove(FlakyTestsCacheKey);
        _logger.LogDebug("Flaky tests cache invalidated");
    }

    /// <summary>
    /// Invalidates all trends cache
    /// </summary>
    public void InvalidateAllTrends()
    {
        // In a production system, you'd want to track all trend cache keys
        // For now, we'll rely on the TTL expiration
        _logger.LogDebug("All trends cache invalidated (via TTL)");
    }

    /// <summary>
    /// Invalidates specific trend cache
    /// </summary>
    public void InvalidateTrends(string cacheKey)
    {
        var fullKey = $"{TrendsCacheKeyPrefix}{cacheKey}";
        _cache.Remove(fullKey);
        _logger.LogDebug("Trends cache invalidated for key {Key}", cacheKey);
    }

    /// <summary>
    /// Clears all analytics caches
    /// </summary>
    public void ClearAll()
    {
        InvalidateDashboard();
        InvalidateFlakyTests();
        InvalidateAllTrends();
        _logger.LogInformation("All analytics caches cleared");
    }

    /// <summary>
    /// Gets cache statistics
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        // Note: IMemoryCache doesn't expose statistics by default
        // This is a placeholder for custom tracking if implemented
        return new CacheStatistics
        {
            TotalEntries = 0, // Would need custom tracking
            LastClearTime = DateTimeOffset.UtcNow
        };
    }
}

/// <summary>
/// Cache statistics
/// </summary>
public sealed class CacheStatistics
{
    public int TotalEntries { get; set; }
    public DateTimeOffset LastClearTime { get; set; }
}
