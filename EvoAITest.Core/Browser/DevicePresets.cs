namespace EvoAITest.Core.Browser;

using EvoAITest.Core.Models;

/// <summary>
/// Predefined device profiles for common mobile devices, tablets, and desktops.
/// Based on real device specifications and Playwright's device descriptors.
/// </summary>
public static class DevicePresets
{
    #region iOS Devices

    /// <summary>
    /// Apple iPhone 14 Pro (393x852, 3x scale, iOS 16).
    /// </summary>
    public static readonly DeviceProfile iPhone14Pro = new()
    {
        Name = "iPhone 14 Pro",
        UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
        Viewport = new ViewportSize(393, 852),
        Screen = new ScreenSize(393, 852),
        DeviceScaleFactor = 3.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "iOS",
        Locale = "en-US"
    };

    /// <summary>
    /// Apple iPhone 13 (390x844, 3x scale, iOS 15).
    /// </summary>
    public static readonly DeviceProfile iPhone13 = new()
    {
        Name = "iPhone 13",
        UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1",
        Viewport = new ViewportSize(390, 844),
        Screen = new ScreenSize(390, 844),
        DeviceScaleFactor = 3.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "iOS",
        Locale = "en-US"
    };

    /// <summary>
    /// Apple iPhone 12 (390x844, 3x scale, iOS 14).
    /// </summary>
    public static readonly DeviceProfile iPhone12 = new()
    {
        Name = "iPhone 12",
        UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.0 Mobile/15E148 Safari/604.1",
        Viewport = new ViewportSize(390, 844),
        Screen = new ScreenSize(390, 844),
        DeviceScaleFactor = 3.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "iOS",
        Locale = "en-US"
    };

    /// <summary>
    /// Apple iPhone SE (375x667, 2x scale, iOS 15).
    /// Compact 4.7" display.
    /// </summary>
    public static readonly DeviceProfile iPhoneSE = new()
    {
        Name = "iPhone SE",
        UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1",
        Viewport = new ViewportSize(375, 667),
        Screen = new ScreenSize(375, 667),
        DeviceScaleFactor = 2.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "iOS",
        Locale = "en-US"
    };

    /// <summary>
    /// Apple iPad Pro 11" (834x1194, 2x scale, iPadOS 16).
    /// </summary>
    public static readonly DeviceProfile iPadPro11 = new()
    {
        Name = "iPad Pro 11",
        UserAgent = "Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
        Viewport = new ViewportSize(834, 1194),
        Screen = new ScreenSize(834, 1194),
        DeviceScaleFactor = 2.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "iOS",
        Locale = "en-US"
    };

    /// <summary>
    /// Apple iPad Pro 12.9" (1024x1366, 2x scale, iPadOS 16).
    /// </summary>
    public static readonly DeviceProfile iPadPro12_9 = new()
    {
        Name = "iPad Pro 12.9",
        UserAgent = "Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
        Viewport = new ViewportSize(1024, 1366),
        Screen = new ScreenSize(1024, 1366),
        DeviceScaleFactor = 2.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "iOS",
        Locale = "en-US"
    };

    /// <summary>
    /// Apple iPad Air (820x1180, 2x scale, iPadOS 16).
    /// </summary>
    public static readonly DeviceProfile iPadAir = new()
    {
        Name = "iPad Air",
        UserAgent = "Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
        Viewport = new ViewportSize(820, 1180),
        Screen = new ScreenSize(820, 1180),
        DeviceScaleFactor = 2.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "iOS",
        Locale = "en-US"
    };

    #endregion

    #region Android Devices

    /// <summary>
    /// Samsung Galaxy S23 (360x780, 3x scale, Android 13).
    /// </summary>
    public static readonly DeviceProfile GalaxyS23 = new()
    {
        Name = "Samsung Galaxy S23",
        UserAgent = "Mozilla/5.0 (Linux; Android 13; SM-S911B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36",
        Viewport = new ViewportSize(360, 780),
        Screen = new ScreenSize(360, 780),
        DeviceScaleFactor = 3.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "Android",
        Locale = "en-US"
    };

    /// <summary>
    /// Samsung Galaxy S22 (360x800, 3x scale, Android 12).
    /// </summary>
    public static readonly DeviceProfile GalaxyS22 = new()
    {
        Name = "Samsung Galaxy S22",
        UserAgent = "Mozilla/5.0 (Linux; Android 12; SM-S901B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.0.0 Mobile Safari/537.36",
        Viewport = new ViewportSize(360, 800),
        Screen = new ScreenSize(360, 800),
        DeviceScaleFactor = 3.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "Android",
        Locale = "en-US"
    };

    /// <summary>
    /// Google Pixel 7 (412x915, 2.625x scale, Android 13).
    /// </summary>
    public static readonly DeviceProfile Pixel7 = new()
    {
        Name = "Google Pixel 7",
        UserAgent = "Mozilla/5.0 (Linux; Android 13; Pixel 7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36",
        Viewport = new ViewportSize(412, 915),
        Screen = new ScreenSize(412, 915),
        DeviceScaleFactor = 2.625,
        HasTouch = true,
        IsMobile = true,
        Platform = "Android",
        Locale = "en-US"
    };

    /// <summary>
    /// Google Pixel 7 Pro (412x892, 3.5x scale, Android 13).
    /// </summary>
    public static readonly DeviceProfile Pixel7Pro = new()
    {
        Name = "Google Pixel 7 Pro",
        UserAgent = "Mozilla/5.0 (Linux; Android 13; Pixel 7 Pro) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36",
        Viewport = new ViewportSize(412, 892),
        Screen = new ScreenSize(412, 892),
        DeviceScaleFactor = 3.5,
        HasTouch = true,
        IsMobile = true,
        Platform = "Android",
        Locale = "en-US"
    };

    /// <summary>
    /// Google Pixel 5 (393x851, 2.75x scale, Android 11).
    /// </summary>
    public static readonly DeviceProfile Pixel5 = new()
    {
        Name = "Google Pixel 5",
        UserAgent = "Mozilla/5.0 (Linux; Android 11; Pixel 5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/90.0.4430.91 Mobile Safari/537.36",
        Viewport = new ViewportSize(393, 851),
        Screen = new ScreenSize(393, 851),
        DeviceScaleFactor = 2.75,
        HasTouch = true,
        IsMobile = true,
        Platform = "Android",
        Locale = "en-US"
    };

    /// <summary>
    /// Samsung Galaxy Tab S8 (800x1280, 2x scale, Android 12).
    /// </summary>
    public static readonly DeviceProfile GalaxyTabS8 = new()
    {
        Name = "Samsung Galaxy Tab S8",
        UserAgent = "Mozilla/5.0 (Linux; Android 12; SM-X706B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/103.0.0.0 Safari/537.36",
        Viewport = new ViewportSize(800, 1280),
        Screen = new ScreenSize(800, 1280),
        DeviceScaleFactor = 2.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "Android",
        Locale = "en-US"
    };

    #endregion

    #region Generic Devices

    /// <summary>
    /// Generic mobile device with common viewport (375x667, 2x scale).
    /// Similar to iPhone 8 dimensions.
    /// </summary>
    public static readonly DeviceProfile GenericMobile = new()
    {
        Name = "Generic Mobile",
        UserAgent = "Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36",
        Viewport = new ViewportSize(375, 667),
        Screen = new ScreenSize(375, 667),
        DeviceScaleFactor = 2.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "Android",
        Locale = "en-US"
    };

    /// <summary>
    /// Generic tablet device with common viewport (768x1024, 2x scale).
    /// </summary>
    public static readonly DeviceProfile GenericTablet = new()
    {
        Name = "Generic Tablet",
        UserAgent = "Mozilla/5.0 (Linux; Android 10) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36",
        Viewport = new ViewportSize(768, 1024),
        Screen = new ScreenSize(768, 1024),
        DeviceScaleFactor = 2.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "Android",
        Locale = "en-US"
    };

    #endregion

    #region Desktop Devices (for comparison)

    /// <summary>
    /// Desktop Chrome browser (1920x1080, 1x scale).
    /// </summary>
    public static readonly DeviceProfile DesktopChrome = new()
    {
        Name = "Desktop Chrome",
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36",
        Viewport = new ViewportSize(1920, 1080),
        Screen = new ScreenSize(1920, 1080),
        DeviceScaleFactor = 1.0,
        HasTouch = false,
        IsMobile = false,
        Platform = "Windows",
        Locale = "en-US"
    };

    /// <summary>
    /// Desktop Firefox browser (1920x1080, 1x scale).
    /// </summary>
    public static readonly DeviceProfile DesktopFirefox = new()
    {
        Name = "Desktop Firefox",
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/112.0",
        Viewport = new ViewportSize(1920, 1080),
        Screen = new ScreenSize(1920, 1080),
        DeviceScaleFactor = 1.0,
        HasTouch = false,
        IsMobile = false,
        Platform = "Windows",
        Locale = "en-US"
    };

    /// <summary>
    /// Laptop with common viewport (1366x768, 1x scale).
    /// </summary>
    public static readonly DeviceProfile Laptop = new()
    {
        Name = "Laptop 1366x768",
        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Safari/537.36",
        Viewport = new ViewportSize(1366, 768),
        Screen = new ScreenSize(1366, 768),
        DeviceScaleFactor = 1.0,
        HasTouch = false,
        IsMobile = false,
        Platform = "Windows",
        Locale = "en-US"
    };

    #endregion

    #region Device Collections

    /// <summary>
    /// Gets all available device presets.
    /// </summary>
    /// <returns>Dictionary of device names to profiles.</returns>
    public static Dictionary<string, DeviceProfile> GetAllDevices()
    {
        return new Dictionary<string, DeviceProfile>(StringComparer.OrdinalIgnoreCase)
        {
            // iOS
            [iPhone14Pro.Name] = iPhone14Pro,
            [iPhone13.Name] = iPhone13,
            [iPhone12.Name] = iPhone12,
            [iPhoneSE.Name] = iPhoneSE,
            [iPadPro11.Name] = iPadPro11,
            [iPadPro12_9.Name] = iPadPro12_9,
            [iPadAir.Name] = iPadAir,

            // Android
            [GalaxyS23.Name] = GalaxyS23,
            [GalaxyS22.Name] = GalaxyS22,
            [Pixel7.Name] = Pixel7,
            [Pixel7Pro.Name] = Pixel7Pro,
            [Pixel5.Name] = Pixel5,
            [GalaxyTabS8.Name] = GalaxyTabS8,

            // Generic
            [GenericMobile.Name] = GenericMobile,
            [GenericTablet.Name] = GenericTablet,

            // Desktop
            [DesktopChrome.Name] = DesktopChrome,
            [DesktopFirefox.Name] = DesktopFirefox,
            [Laptop.Name] = Laptop
        };
    }

    /// <summary>
    /// Gets a device profile by name.
    /// </summary>
    /// <param name="name">Device name (case-insensitive).</param>
    /// <returns>DeviceProfile if found; otherwise, null.</returns>
    public static DeviceProfile? GetDevice(string name)
    {
        var devices = GetAllDevices();
        return devices.TryGetValue(name, out var device) ? device : null;
    }

    /// <summary>
    /// Gets all iOS device presets.
    /// </summary>
    public static List<DeviceProfile> GetIOSDevices()
    {
        return new List<DeviceProfile>
        {
            iPhone14Pro,
            iPhone13,
            iPhone12,
            iPhoneSE,
            iPadPro11,
            iPadPro12_9,
            iPadAir
        };
    }

    /// <summary>
    /// Gets all Android device presets.
    /// </summary>
    public static List<DeviceProfile> GetAndroidDevices()
    {
        return new List<DeviceProfile>
        {
            GalaxyS23,
            GalaxyS22,
            Pixel7,
            Pixel7Pro,
            Pixel5,
            GalaxyTabS8
        };
    }

    /// <summary>
    /// Gets all mobile phone presets (excluding tablets).
    /// </summary>
    public static List<DeviceProfile> GetMobilePhones()
    {
        return new List<DeviceProfile>
        {
            iPhone14Pro,
            iPhone13,
            iPhone12,
            iPhoneSE,
            GalaxyS23,
            GalaxyS22,
            Pixel7,
            Pixel7Pro,
            Pixel5,
            GenericMobile
        };
    }

    /// <summary>
    /// Gets all tablet presets.
    /// </summary>
    public static List<DeviceProfile> GetTablets()
    {
        return new List<DeviceProfile>
        {
            iPadPro11,
            iPadPro12_9,
            iPadAir,
            GalaxyTabS8,
            GenericTablet
        };
    }

    #endregion
}
