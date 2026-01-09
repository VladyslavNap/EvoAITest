using EvoAITest.Core.Options;
using EvoAITest.Core.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace EvoAITest.Tests.Core;

/// <summary>
/// Unit tests for Key Vault secret providers.
/// </summary>
public sealed class SecretProviderTests
{
    private readonly ILogger<KeyVaultSecretProvider> _keyVaultLogger;
    private readonly ILogger<NoOpSecretProvider> _noOpLogger;

    public SecretProviderTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _keyVaultLogger = loggerFactory.CreateLogger<KeyVaultSecretProvider>();
        _noOpLogger = loggerFactory.CreateLogger<NoOpSecretProvider>();
    }

    #region NoOpSecretProvider Tests

    [Fact]
    public void NoOpSecretProvider_Constructor_Succeeds()
    {
        // Act
        var provider = new NoOpSecretProvider(_noOpLogger);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public async Task NoOpSecretProvider_GetSecretAsync_ReturnsNull()
    {
        // Arrange
        var provider = new NoOpSecretProvider(_noOpLogger);

        // Act
        var secret = await provider.GetSecretAsync("test-secret");

        // Assert
        secret.Should().BeNull();
    }

    [Fact]
    public async Task NoOpSecretProvider_GetSecretsAsync_ReturnsEmptyDictionary()
    {
        // Arrange
        var provider = new NoOpSecretProvider(_noOpLogger);
        var secretNames = new[] { "secret1", "secret2", "secret3" };

        // Act
        var secrets = await provider.GetSecretsAsync(secretNames);

        // Assert
        secrets.Should().NotBeNull();
        secrets.Should().BeEmpty();
    }

    [Fact]
    public async Task NoOpSecretProvider_IsAvailableAsync_ReturnsTrue()
    {
        // Arrange
        var provider = new NoOpSecretProvider(_noOpLogger);

        // Act
        var isAvailable = await provider.IsAvailableAsync();

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Fact]
    public void NoOpSecretProvider_InvalidateCache_DoesNotThrow()
    {
        // Arrange
        var provider = new NoOpSecretProvider(_noOpLogger);

        // Act & Assert
        provider.Invoking(p => p.InvalidateCache()).Should().NotThrow();
        provider.Invoking(p => p.InvalidateCache("test-secret")).Should().NotThrow();
    }

    #endregion

    #region KeyVaultOptions Tests

    [Fact]
    public void KeyVaultOptions_Validate_WithEmptyVaultUri_ReturnsErrors()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = true,
            VaultUri = ""
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("VaultUri"));
    }

    [Fact]
    public void KeyVaultOptions_Validate_WithInvalidVaultUri_ReturnsErrors()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = true,
            VaultUri = "not-a-valid-url"
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("HTTPS"));
    }

    [Fact]
    public void KeyVaultOptions_Validate_WithValidConfiguration_ReturnsNoErrors()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = true,
            VaultUri = "https://test-vault.vault.azure.net/",
            EnableCaching = true,
            CacheDuration = TimeSpan.FromHours(1),
            MaxRetries = 3
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void KeyVaultOptions_Validate_WithNegativeCacheDuration_ReturnsErrors()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = true,
            VaultUri = "https://test-vault.vault.azure.net/",
            CacheDuration = TimeSpan.FromMinutes(-10)
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("CacheDuration"));
    }

    [Fact]
    public void KeyVaultOptions_Validate_WithExcessiveRetries_ReturnsErrors()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = true,
            VaultUri = "https://test-vault.vault.azure.net/",
            MaxRetries = 15
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("MaxRetries"));
    }

    [Fact]
    public void KeyVaultOptions_Validate_WhenDisabled_ReturnsValid()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = false,
            VaultUri = "" // Invalid URI but should be ignored when disabled
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void KeyVaultOptions_CreateDevelopmentDefaults_ReturnsValidConfiguration()
    {
        // Act
        var options = KeyVaultOptions.CreateDevelopmentDefaults();

        // Assert
        options.Should().NotBeNull();
        options.Enabled.Should().BeFalse();
        options.EnableCaching.Should().BeFalse();
        options.LogSecretNames.Should().BeTrue();
    }

    [Fact]
    public void KeyVaultOptions_CreateProductionDefaults_ReturnsValidConfiguration()
    {
        // Arrange
        var vaultUri = "https://prod-vault.vault.azure.net/";

        // Act
        var options = KeyVaultOptions.CreateProductionDefaults(vaultUri);

        // Assert
        options.Should().NotBeNull();
        options.Enabled.Should().BeTrue();
        options.VaultUri.Should().Be(vaultUri);
        options.EnableCaching.Should().BeTrue();
        options.CacheDuration.Should().Be(TimeSpan.FromHours(1));
        options.LogSecretNames.Should().BeFalse();
    }

    [Fact]
    public void KeyVaultOptions_Validate_WithInvalidTenantId_ReturnsErrors()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = true,
            VaultUri = "https://test-vault.vault.azure.net/",
            TenantId = "not-a-guid"
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("TenantId"));
    }

    [Fact]
    public void KeyVaultOptions_Validate_WithValidTenantId_ReturnsNoErrors()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = true,
            VaultUri = "https://test-vault.vault.azure.net/",
            TenantId = Guid.NewGuid().ToString()
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void KeyVaultOptions_Validate_WithExcessiveCacheDuration_ReturnsErrors()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = true,
            VaultUri = "https://test-vault.vault.azure.net/",
            CacheDuration = TimeSpan.FromHours(30)
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("CacheDuration"));
    }

    [Fact]
    public void KeyVaultOptions_Validate_WithShortOperationTimeout_ReturnsErrors()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = true,
            VaultUri = "https://test-vault.vault.azure.net/",
            OperationTimeout = TimeSpan.FromMilliseconds(500)
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("OperationTimeout"));
    }

    [Fact]
    public void KeyVaultOptions_Validate_WithExcessiveOperationTimeout_ReturnsErrors()
    {
        // Arrange
        var options = new KeyVaultOptions
        {
            Enabled = true,
            VaultUri = "https://test-vault.vault.azure.net/",
            OperationTimeout = TimeSpan.FromMinutes(10)
        };

        // Act
        var (isValid, errors) = options.Validate();

        // Assert
        isValid.Should().BeFalse();
        errors.Should().Contain(e => e.Contains("OperationTimeout"));
    }

    #endregion

    #region KeyVaultSecretProvider Tests (Basic)

    [Fact]
    public void KeyVaultSecretProvider_Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        Xunit.Assert.Throws<ArgumentNullException>(() =>
            new KeyVaultSecretProvider(null!, _keyVaultLogger));
    }

    [Fact]
    public void KeyVaultSecretProvider_Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var options = CreateValidKeyVaultOptions();

        // Act & Assert
        Xunit.Assert.Throws<ArgumentNullException>(() =>
            new KeyVaultSecretProvider(options, null!));
    }

    [Fact]
    public void KeyVaultSecretProvider_Constructor_WithEmptyVaultUri_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new KeyVaultOptions { VaultUri = "" });

        // Act & Assert
        Xunit.Assert.Throws<InvalidOperationException>(() =>
            new KeyVaultSecretProvider(options, _keyVaultLogger));
    }

    [Fact]
    public async Task KeyVaultSecretProvider_GetSecretAsync_WithNullSecretName_ThrowsArgumentException()
    {
        // Arrange
        var options = CreateValidKeyVaultOptions();
        var provider = new KeyVaultSecretProvider(options, _keyVaultLogger);

        // Act & Assert
        await Xunit.Assert.ThrowsAsync<ArgumentException>(async () =>
            await provider.GetSecretAsync(null!));
    }

    [Fact]
    public async Task KeyVaultSecretProvider_GetSecretsAsync_WithNullSecretNames_ThrowsArgumentNullException()
    {
        // Arrange
        var options = CreateValidKeyVaultOptions();
        var provider = new KeyVaultSecretProvider(options, _keyVaultLogger);

        // Act & Assert
        await Xunit.Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await provider.GetSecretsAsync(null!));
    }

    [Fact]
    public void KeyVaultSecretProvider_InvalidateCache_WithNullSecretName_DoesNotThrow()
    {
        // Arrange
        var options = CreateValidKeyVaultOptions();
        var provider = new KeyVaultSecretProvider(options, _keyVaultLogger);

        // Act & Assert
        provider.Invoking(p => p.InvalidateCache(null)).Should().NotThrow();
    }

    [Fact]
    public void KeyVaultSecretProvider_InvalidateCache_WithSpecificSecret_DoesNotThrow()
    {
        // Arrange
        var options = CreateValidKeyVaultOptions();
        var provider = new KeyVaultSecretProvider(options, _keyVaultLogger);

        // Act & Assert
        provider.Invoking(p => p.InvalidateCache("test-secret")).Should().NotThrow();
    }

    #endregion

    // Helper methods

    private IOptions<KeyVaultOptions> CreateValidKeyVaultOptions()
    {
        return Options.Create(new KeyVaultOptions
        {
            VaultUri = "https://test-vault.vault.azure.net/",
            Enabled = true,
            EnableCaching = true,
            CacheDuration = TimeSpan.FromHours(1),
            MaxRetries = 3,
            OperationTimeout = TimeSpan.FromSeconds(30)
        });
    }
}
