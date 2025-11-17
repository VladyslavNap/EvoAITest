using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models;
using Microsoft.Extensions.Logging;

namespace EvoAITest.Core.Browser;

/// <summary>
/// Playwright-backed implementation of <see cref="IBrowserAgent"/> used for headless automation scenarios.
/// </summary>
public sealed class PlaywrightBrowserAgent : IBrowserAgent
{
    private const int DefaultTimeoutMs = 30_000;

    private static readonly JsonSerializerOptions AccessibilitySerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string InteractiveElementsScript = @"
() => {
    const attributeWhitelist = ['type','name','value','placeholder','href','id','class','role','aria-label','title'];
    const interactiveSelector = [
        'button',
        'input:not([type=hidden])',
        'textarea',
        'select',
        'a[href]',
        '[role=button]',
        '[role=link]',
        '[contenteditable=""true""]'
    ].join(',');

    const escapeId = (value) => {
        if (window.CSS && typeof window.CSS.escape === 'function') {
            return window.CSS.escape(value);
        }
        return value.replace(/([^\w-])/g, '\\$1');
    };

    const computeSelector = (el) => {
        if (el.id) {
            return `#${escapeId(el.id)}`;
        }

        if (el.name) {
            return `${el.tagName.toLowerCase()}[name=""${el.name}""]`;
        }

        const parts = [];
        let current = el;

        while (current && parts.length < 4) {
            if (!current.tagName) {
                break;
            }

            let index = 1;
            let sibling = current;

            while (sibling.previousElementSibling) {
                sibling = sibling.previousElementSibling;
                if (sibling.tagName === current.tagName) {
                    index += 1;
                }
            }

            parts.unshift(`${current.tagName.toLowerCase()}:nth-of-type(${index})`);
            current = current.parentElement;
        }

        return parts.join(' > ') || el.tagName.toLowerCase();
    };

    return Array.from(document.querySelectorAll(interactiveSelector)).map((el) => {
        const rect = el.getBoundingClientRect();
        const styles = window.getComputedStyle(el);
        const isVisible = rect.width > 0 && rect.height > 0 && styles.visibility !== 'hidden' && styles.display !== 'none';
        const isInteractable = isVisible && !el.disabled && styles.pointerEvents !== 'none';
        const attributes = {};

        attributeWhitelist.forEach((name) => {
            const value = el.getAttribute(name);
            if (value !== null) {
                attributes[name] = value;
            }
        });

        return {
            tagName: el.tagName.toLowerCase(),
            selector: computeSelector(el),
            text: (el.innerText || el.value || '').trim().slice(0, 200),
            isVisible,
            isInteractable,
            attributes,
            boundingBox: {
                x: rect.x,
                y: rect.y,
                width: rect.width,
                height: rect.height
            }
        };
    });
}";

    private readonly ILogger<PlaywrightBrowserAgent> _logger;

    private Microsoft.Playwright.IPlaywright? _playwright;
    private Microsoft.Playwright.IBrowser? _browser;
    private Microsoft.Playwright.IBrowserContext? _context;
    private Microsoft.Playwright.IPage? _page;
    private bool _disposed;

    public PlaywrightBrowserAgent(ILogger<PlaywrightBrowserAgent> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_page is not null)
        {
            throw new InvalidOperationException("Playwright browser agent is already initialized.");
        }

        _logger.LogInformation("Initializing Playwright Chromium browser.");

        try
        {
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);

            _browser = await _playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions
            {
                Headless = true,
                Args = new[] { "--disable-blink-features=AutomationControlled" }
            }).ConfigureAwait(false);

            _context = await _browser.NewContextAsync(new Microsoft.Playwright.BrowserNewContextOptions
            {
                ViewportSize = new Microsoft.Playwright.ViewportSize { Width = 1920, Height = 1080 },
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                IgnoreHTTPSErrors = true
            }).ConfigureAwait(false);

            _context.SetDefaultTimeout(DefaultTimeoutMs);
            _context.SetDefaultNavigationTimeout(DefaultTimeoutMs);

            _page = await _context.NewPageAsync().ConfigureAwait(false);

            _logger.LogInformation("Playwright browser initialized successfully.");
        }
        catch
        {
            await DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PageState> GetPageStateAsync(CancellationToken cancellationToken = default)
    {
        var page = EnsurePage();
        cancellationToken.ThrowIfCancellationRequested();

        var titleTask = page.TitleAsync();
        var loadStateTask = GetCurrentLoadStateAsync(page, cancellationToken);
        var interactiveElementsTask = ExtractInteractiveElementsAsync(page, cancellationToken);
        var accessibilityTreeTask = GetAccessibilityTreeAsync(cancellationToken);

        await Task.WhenAll(titleTask, loadStateTask).ConfigureAwait(false);

        var interactiveElements = await interactiveElementsTask.ConfigureAwait(false);
        var accessibilityTree = await accessibilityTreeTask.ConfigureAwait(false);

        var viewportSize = page.ViewportSize;
        PageDimensions? viewportDimensions = null;
        
        if (viewportSize != null)
        {
            viewportDimensions = new PageDimensions
            {
                Width = viewportSize.Width,
                Height = viewportSize.Height
            };
        }

        var state = new PageState
        {
            Url = page.Url,
            Title = titleTask.Result,
            LoadState = loadStateTask.Result,
            InteractiveElements = interactiveElements,
            VisibleElements = interactiveElements.Where(element => element.IsVisible).ToList(),
            ViewportDimensions = viewportDimensions,
            CapturedAt = DateTimeOffset.UtcNow
        };

        if (!string.IsNullOrWhiteSpace(accessibilityTree))
        {
            state.Metadata["accessibilityTree"] = accessibilityTree;
        }

        return state;
    }

    /// <inheritdoc />
    public async Task NavigateAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ArgumentNullException(nameof(url));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var targetUri) ||
            (targetUri.Scheme != Uri.UriSchemeHttp && targetUri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("URL must be a valid absolute HTTP or HTTPS address.", nameof(url));
        }

        var page = EnsurePage();
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation("Navigating to {Url}", targetUri);

        var response = await page.GotoAsync(targetUri.ToString(), new Microsoft.Playwright.PageGotoOptions
        {
            WaitUntil = Microsoft.Playwright.WaitUntilState.NetworkIdle,
            Timeout = DefaultTimeoutMs
        }).WaitAsync(cancellationToken).ConfigureAwait(false);

        if (response is null)
        {
            _logger.LogWarning("Navigation to {Url} completed without a network response.", targetUri);
        }
    }

    /// <inheritdoc />
    public async Task ClickAsync(string selector, int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var page = EnsurePage();
        cancellationToken.ThrowIfCancellationRequested();

        Exception? lastError = null;

        for (var attempt = 1; attempt <= Math.Max(1, maxRetries); attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await page.WaitForSelectorAsync(selector, new Microsoft.Playwright.PageWaitForSelectorOptions
                {
                    State = Microsoft.Playwright.WaitForSelectorState.Visible,
                    Timeout = DefaultTimeoutMs
                }).WaitAsync(cancellationToken).ConfigureAwait(false);

                await page.ClickAsync(selector, new Microsoft.Playwright.PageClickOptions
                {
                    Timeout = DefaultTimeoutMs
                }).WaitAsync(cancellationToken).ConfigureAwait(false);

                return;
            }
            catch (Exception ex) when (ex is Microsoft.Playwright.PlaywrightException or TimeoutException)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Click attempt {Attempt}/{MaxAttempts} failed for selector {Selector}", attempt, maxRetries, selector);

                if (attempt == maxRetries)
                {
                    break;
                }

                var delay = TimeSpan.FromMilliseconds(Math.Pow(2, attempt - 1) * 500);
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new TimeoutException($"Failed to click '{selector}' after {maxRetries} attempts.", lastError);
    }

    /// <inheritdoc />
    public async Task TypeAsync(string selector, string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            throw new ArgumentNullException(nameof(selector));
        }

        text ??= string.Empty;
        var page = EnsurePage();
        cancellationToken.ThrowIfCancellationRequested();

        await page.FillAsync(selector, text, new Microsoft.Playwright.PageFillOptions
        {
            Timeout = DefaultTimeoutMs
        }).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetTextAsync(string selector, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var page = EnsurePage();
        cancellationToken.ThrowIfCancellationRequested();

        var text = await page.TextContentAsync(selector, new Microsoft.Playwright.PageTextContentOptions
        {
            Timeout = DefaultTimeoutMs
        }).WaitAsync(cancellationToken).ConfigureAwait(false);

        return text?.Trim() ?? string.Empty;
    }

    /// <inheritdoc />
    public async Task<string> TakeScreenshotAsync(CancellationToken cancellationToken = default)
    {
        var page = EnsurePage();
        cancellationToken.ThrowIfCancellationRequested();

        var screenshot = await page.ScreenshotAsync(new Microsoft.Playwright.PageScreenshotOptions
        {
            Type = Microsoft.Playwright.ScreenshotType.Png,
            FullPage = true
        }).WaitAsync(cancellationToken).ConfigureAwait(false);

        return Convert.ToBase64String(screenshot);
    }

    /// <inheritdoc />
    public async Task<string> GetAccessibilityTreeAsync(CancellationToken cancellationToken = default)
    {
        var page = EnsurePage();
        cancellationToken.ThrowIfCancellationRequested();

        // Note: The Playwright Accessibility API is obsolete in newer versions.
        // Instead, we extract accessibility information using ARIA roles and attributes via JavaScript evaluation.
        try
        {
            var accessibilityInfo = await page.EvaluateAsync<object>(@"
                () => {
                    const getAccessibilityTree = (element, depth = 0) => {
                        if (depth > 10) return null; // Prevent deep recursion
                        
                        const role = element.getAttribute('role') || element.tagName.toLowerCase();
                        const ariaLabel = element.getAttribute('aria-label');
                        const ariaDescribedBy = element.getAttribute('aria-describedby');
                        const ariaLabelledBy = element.getAttribute('aria-labelledby');
                        
                        const node = {
                            role: role,
                            name: ariaLabel || element.getAttribute('alt') || element.getAttribute('title') || element.textContent?.substring(0, 50)?.trim() || '',
                            ariaLabel: ariaLabel,
                            ariaDescribedBy: ariaDescribedBy,
                            ariaLabelledBy: ariaLabelledBy,
                            children: []
                        };
                        
                        // Only include elements with meaningful accessibility information
                        const hasAriaInfo = ariaLabel || ariaDescribedBy || ariaLabelledBy || element.hasAttribute('role');
                        const isInteractive = ['button', 'a', 'input', 'select', 'textarea'].includes(element.tagName.toLowerCase());
                        
                        if (hasAriaInfo || isInteractive) {
                            for (const child of element.children) {
                                const childNode = getAccessibilityTree(child, depth + 1);
                                if (childNode) {
                                    node.children.push(childNode);
                                }
                            }
                            return node;
                        }
                        
                        return null;
                    };
                    
                    return getAccessibilityTree(document.body);
                }
            ").WaitAsync(cancellationToken).ConfigureAwait(false);

            return accessibilityInfo is null
                ? string.Empty
                : JsonSerializer.Serialize(accessibilityInfo, AccessibilitySerializerOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract accessibility tree");
            return string.Empty;
        }
    }

    /// <inheritdoc />
    public async Task WaitForElementAsync(string selector, int timeoutMs = DefaultTimeoutMs, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            throw new ArgumentNullException(nameof(selector));
        }

        var page = EnsurePage();
        cancellationToken.ThrowIfCancellationRequested();

        await page.WaitForSelectorAsync(selector, new Microsoft.Playwright.PageWaitForSelectorOptions
        {
            Timeout = timeoutMs,
            State = Microsoft.Playwright.WaitForSelectorState.Visible
        }).WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<string> GetPageHtmlAsync(CancellationToken cancellationToken = default)
    {
        var page = EnsurePage();
        cancellationToken.ThrowIfCancellationRequested();

        return await page.ContentAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        var disposeTasks = new List<Task>();

        if (_page is not null)
        {
            disposeTasks.Add(CloseResourceAsync(() => _page.CloseAsync(), "page"));
        }

        if (_context is not null)
        {
            disposeTasks.Add(CloseResourceAsync(() => _context.CloseAsync(), "context"));
        }

        if (_browser is not null)
        {
            disposeTasks.Add(CloseResourceAsync(() => _browser.CloseAsync(), "browser"));
        }

        if (disposeTasks.Count > 0)
        {
            await Task.WhenAll(disposeTasks).ConfigureAwait(false);
        }

        _playwright?.Dispose();

        _page = null;
        _context = null;
        _browser = null;
        _playwright = null;
    }

    private Microsoft.Playwright.IPage EnsurePage()
    {
        if (_page is null)
        {
            throw new InvalidOperationException("The Playwright browser agent has not been initialized.");
        }

        return _page;
    }

    private async Task<Models.LoadState> GetCurrentLoadStateAsync(Microsoft.Playwright.IPage page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle, new Microsoft.Playwright.PageWaitForLoadStateOptions
            {
                Timeout = 500
            }).WaitAsync(cancellationToken).ConfigureAwait(false);

            return Models.LoadState.NetworkIdle;
        }
        catch (TimeoutException)
        {
            // Ignored – fall back to document.readyState.
        }
        catch (Microsoft.Playwright.PlaywrightException)
        {
            // Ignored – fall back to document.readyState.
        }

        var readyState = await page.EvaluateAsync<string>("document.readyState")
            .WaitAsync(cancellationToken).ConfigureAwait(false);

        return readyState switch
        {
            "complete" => Models.LoadState.Load,
            "interactive" => Models.LoadState.DomContentLoaded,
            _ => Models.LoadState.Loading
        };
    }

    private async Task<List<ElementInfo>> ExtractInteractiveElementsAsync(Microsoft.Playwright.IPage page, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var snapshots = await page.EvaluateAsync<InteractiveElementSnapshot[]>(InteractiveElementsScript)
            .WaitAsync(cancellationToken).ConfigureAwait(false);

        if (snapshots is null || snapshots.Length == 0)
        {
            return new List<ElementInfo>();
        }

        var elements = new List<ElementInfo>(snapshots.Length);

        foreach (var snapshot in snapshots)
        {
            var element = new ElementInfo
            {
                TagName = snapshot.TagName ?? string.Empty,
                Selector = snapshot.Selector,
                Text = snapshot.Text,
                IsVisible = snapshot.IsVisible,
                IsInteractable = snapshot.IsInteractable,
                Attributes = snapshot.Attributes ?? new Dictionary<string, string>()
            };

            if (snapshot.BoundingBox is not null)
            {
                element.BoundingBox = new BoundingBox
                {
                    X = snapshot.BoundingBox.X,
                    Y = snapshot.BoundingBox.Y,
                    Width = snapshot.BoundingBox.Width,
                    Height = snapshot.BoundingBox.Height
                };
            }

            elements.Add(element);
        }

        return elements;
    }

    private async Task CloseResourceAsync(Func<Task> closer, string resourceName)
    {
        try
        {
            await closer().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to close Playwright {Resource}", resourceName);
        }
    }

    private sealed class InteractiveElementSnapshot
    {
        public string TagName { get; init; } = string.Empty;

        public string Selector { get; init; } = string.Empty;

        public string? Text { get; init; }

        public bool IsVisible { get; init; }

        public bool IsInteractable { get; init; }

        public Dictionary<string, string>? Attributes { get; init; }

        public ElementBoundingBoxSnapshot? BoundingBox { get; init; }
    }

    private sealed class ElementBoundingBoxSnapshot
    {
        public double X { get; init; }

        public double Y { get; init; }

        public double Width { get; init; }

        public double Height { get; init; }
    }
}
