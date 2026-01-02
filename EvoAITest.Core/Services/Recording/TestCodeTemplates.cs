namespace EvoAITest.Core.Services.Recording;

/// <summary>
/// Templates for generating test code in different frameworks and languages
/// </summary>
public static class TestCodeTemplates
{
    public const string XUnitPlaywrightTemplate = @"using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace {namespace};

/// <summary>
/// {description}
/// </summary>
public class {className} : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = {viewportWidth}, Height = {viewportHeight} }
        });
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

{testMethods}
}";

    public const string XUnitTestMethodTemplate = @"    /// <summary>
    /// {description}
    /// </summary>
    [Fact]
    public async Task {methodName}()
    {
        // Arrange
{arrangeCode}

        // Act
{actCode}

        // Assert
{assertCode}
    }";

    public const string NUnitPlaywrightTemplate = @"using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using NUnit.Framework;

namespace {namespace};

/// <summary>
/// {description}
/// </summary>
[TestFixture]
public class {className}
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    [SetUp]
    public async Task Setup()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = {viewportWidth}, Height = {viewportHeight} }
        });
        _page = await _context.NewPageAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

{testMethods}
}";

    public const string NUnitTestMethodTemplate = @"    /// <summary>
    /// {description}
    /// </summary>
    [Test]
    public async Task {methodName}()
    {
        // Arrange
{arrangeCode}

        // Act
{actCode}

        // Assert
{assertCode}
    }";

    public const string MSTestPlaywrightTemplate = @"using System;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace {namespace};

/// <summary>
/// {description}
/// </summary>
[TestClass]
public class {className}
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    [TestInitialize]
    public async Task Initialize()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = {viewportWidth}, Height = {viewportHeight} }
        });
        _page = await _context.NewPageAsync();
    }

    [TestCleanup]
    public async Task Cleanup()
    {
        if (_page != null) await _page.CloseAsync();
        if (_context != null) await _context.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

{testMethods}
}";

    public const string MSTestTestMethodTemplate = @"    /// <summary>
    /// {description}
    /// </summary>
    [TestMethod]
    public async Task {methodName}()
    {
        // Arrange
{arrangeCode}

        // Act
{actCode}

        // Assert
{assertCode}
    }";

    /// <summary>
    /// Page Object Model class template
    /// </summary>
    public const string PageObjectTemplate = @"using System.Threading.Tasks;
using Microsoft.Playwright;

namespace {namespace}.PageObjects;

/// <summary>
/// Page Object for {pageName}
/// </summary>
public class {className}
{
    private readonly IPage _page;
    private const string PageUrl = ""{url}"";

{locators}

    public {className}(IPage page)
    {
        _page = page;
    }

    public async Task NavigateAsync()
    {
        await _page.GotoAsync(PageUrl);
    }

{methods}
}";

    public const string PageObjectLocatorTemplate = @"    private ILocator {propertyName} => _page.Locator(""{selector}"");";

    public const string PageObjectMethodTemplate = @"    public async Task {methodName}({parameters})
    {
{body}
    }";
}
