# Phase 2: Mobile Emulation & Network Interception - Implementation Plan

## Status: ?? **PLANNED** - Ready for Implementation

### Overview

This document outlines the implementation plan for completing the remaining Phase 2 features:
1. **Mobile Browser Emulation** - Playwright ready, needs configuration
2. **Network Interception and Mocking** - Playwright ready, needs implementation

---

## Prerequisites

### Existing Infrastructure
? Playwright browser agent implemented  
? Tool execution framework in place  
? Configuration system established  
? Testing infrastructure ready  
? Documentation structure complete  

### Technical Foundation
- **Playwright Version:** Already installed with mobile device support
- **Browser Context:** Supports device emulation out of the box
- **Route API:** Available for network interception
- **.NET 10:** Full async/await support for interception handlers

---

## Part 1: Mobile Browser Emulation

### Estimated Effort: ~8-10 hours

---

### Step 1: Design Mobile Device Configuration (2 hours)

#### 1.1 Create Device Profile Model

**File:** `EvoAITest.Core/Models/DeviceProfile.cs`

```csharp
namespace EvoAITest.Core.Models;

/// <summary>
/// Represents a mobile device configuration for browser emulation.
/// </summary>
public sealed record DeviceProfile
{
    /// <summary>Device name (e.g., "iPhone 14 Pro", "Galaxy S23").</summary>
    public required string Name { get; init; }
    
    /// <summary>User agent string for the device.</summary>
    public required string UserAgent { get; init; }
    
    /// <summary>Viewport dimensions.</summary>
    public required ViewportSize Viewport { get; init; }
    
    /// <summary>Device pixel ratio (e.g., 2 for Retina, 3 for high-DPI).</summary>
    public double DeviceScaleFactor { get; init; } = 1.0;
    
    /// <summary>Whether the device has touch support.</summary>
    public bool HasTouch { get; init; } = true;
    
    /// <summary>Whether the device is in landscape orientation.</summary>
    public bool IsLandscape { get; init; } = false;
    
    /// <summary>Whether the device is mobile (affects meta viewport and touch events).</summary>
    public bool IsMobile { get; init; } = true;
    
    /// <summary>Platform (iOS, Android, etc.).</summary>
    public string? Platform { get; init; }
    
    /// <summary>Optional screen dimensions (may differ from viewport due to browser chrome).</summary>
    public ScreenSize? Screen { get; init; }
    
    /// <summary>Optional geolocation coordinates.</summary>
    public GeolocationCoordinates? Geolocation { get; init; }
    
    /// <summary>Optional timezone ID (e.g., "America/New_York").</summary>
    public string? TimezoneId { get; init; }
    
    /// <summary>Optional locale (e.g., "en-US").</summary>
    public string? Locale { get; init; }
    
    /// <summary>Optional permissions to grant (e.g., "geolocation", "notifications").</summary>
    public List<string>? Permissions { get; init; }
}

public sealed record ViewportSize(int Width, int Height);
public sealed record ScreenSize(int Width, int Height);
public sealed record GeolocationCoordinates(double Latitude, double Longitude, double? Accuracy = null);
```

#### 1.2 Create Device Presets

**File:** `EvoAITest.Core/Browser/DevicePresets.cs`

```csharp
namespace EvoAITest.Core.Browser;

/// <summary>
/// Predefined device profiles for common mobile devices.
/// Based on Playwright's device descriptors.
/// </summary>
public static class DevicePresets
{
    // iOS Devices
    public static readonly DeviceProfile iPhone14Pro = new()
    {
        Name = "iPhone 14 Pro",
        UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
        Viewport = new ViewportSize(393, 852),
        DeviceScaleFactor = 3.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "iOS"
    };
    
    public static readonly DeviceProfile iPhone13 = new()
    {
        Name = "iPhone 13",
        UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 15_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/15.0 Mobile/15E148 Safari/604.1",
        Viewport = new ViewportSize(390, 844),
        DeviceScaleFactor = 3.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "iOS"
    };
    
    public static readonly DeviceProfile iPadPro = new()
    {
        Name = "iPad Pro 11",
        UserAgent = "Mozilla/5.0 (iPad; CPU OS 16_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.0 Mobile/15E148 Safari/604.1",
        Viewport = new ViewportSize(834, 1194),
        DeviceScaleFactor = 2.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "iOS"
    };
    
    // Android Devices
    public static readonly DeviceProfile GalaxyS23 = new()
    {
        Name = "Samsung Galaxy S23",
        UserAgent = "Mozilla/5.0 (Linux; Android 13; SM-S911B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/112.0.0.0 Mobile Safari/537.36",
        Viewport = new ViewportSize(360, 780),
        DeviceScaleFactor = 3.0,
        HasTouch = true,
        IsMobile = true,
        Platform = "Android"
    };
    
    public static readonly DeviceProfile PixelWhen creating a plan for mobile browser emulation and network interception, I'll provide a comprehensive implementation guide with code examples, configuration, testing strategy, and documentation updates.

Let me create a detailed plan document:

```markdown
# Phase 2 Enhancement: Mobile Emulation & Network Interception
## Implementation Plan

### Overview
Complete the remaining Phase 2 features that are "Playwright ready" but need configuration/implementation.

---

## Part 1: Mobile Browser Emulation

### 1.1 Device Configuration Model
**File:** `EvoAITest.Core/Models/DeviceProfile.cs`

Create a model to represent mobile device configurations:

```csharp
public sealed record DeviceProfile
{
    public required string Name { get; init; }
    public required string UserAgent { get; init; }
    public required ViewportSize Viewport { get; init; }
    public double DeviceScaleFactor { get; init; } = 1.0;
    public bool HasTouch { get; init; } = true;
    public bool IsMobile { get; init; } = true;
    public string? Platform { get; init; }
    public GeolocationCoordinates? Geolocation { get; init; }
}
```

### 1.2 Device Presets Library
**File:** `EvoAITest.Core/Browser/DevicePresets.cs`

Implement common mobile device profiles:
- iPhone 14 Pro, iPhone 13, iPhone SE
- Galaxy S23, Pixel 7, Pixel 7 Pro
- iPad Pro, Galaxy Tab S8

### 1.3 Extend IBrowserAgent Interface
**File:** `EvoAITest.Core/Abstractions/IBrowserAgent.cs`

Add methods:
```csharp
Task SetDeviceEmulationAsync(DeviceProfile device, CancellationToken cancellationToken = default);
Task SetGeolocationAsync(double latitude, double longitude, double? accuracy = null, CancellationToken cancellationToken = default);
Task SetTimezoneAsync(string timezoneId, CancellationToken cancellationToken = default);
Task GrantPermissionsAsync(string[] permissions, CancellationToken cancellationToken = default);
```

### 1.4 Implement in PlaywrightBrowserAgent
**File:** `EvoAITest.Core/Browser/PlaywrightBrowserAgent.cs`

Use Playwright's built-in device emulation:
```csharp
await _context.SetViewportSizeAsync(device.Viewport.Width, device.Viewport.Height);
await _context.SetUserAgentAsync(device.UserAgent);
await _context.SetGeolocationAsync(new() { Latitude = lat, Longitude = lon });
```

### 1.5 Add Mobile Tools to Registry
**File:** `EvoAITest.Core/Tools/BrowserToolRegistry.cs`

New tools:
- `set_device_emulation` - Configure mobile device
- `set_geolocation` - Set GPS coordinates
- `set_timezone` - Configure timezone
- `grant_permissions` - Grant browser permissions

### 1.6 Configuration Options
**File:** `appsettings.json`

```json
{
  "EvoAITest": {
    "Mobile": {
      "DefaultDevice": "iPhone14Pro",
      "EnableTouchEvents": true,
      "EnableGeolocation": false,
      "DefaultGeolocation": {
        "Latitude": 37.7749,
        "Longitude": -122.4194
      }
    }
  }
}
```

### 1.7 Testing
**File:** `EvoAITest.Tests/Integration/MobileEmulationTests.cs`

Test cases:
- Device profile application
- Viewport and user agent verification
- Touch event simulation
- Geolocation API
- Orientation changes

**Estimated Time:** 8-10 hours

---

## Part 2: Network Interception and Mocking

### 2.1 Network Interceptor Interface
**File:** `EvoAITest.Core/Abstractions/INetworkInterceptor.cs`

```csharp
public interface INetworkInterceptor
{
    Task InterceptRequestAsync(string pattern, Func<InterceptedRequest, Task<InterceptedResponse?>> handler);
    Task BlockRequestAsync(string pattern);
    Task MockResponseAsync(string pattern, MockResponse response);
    Task<List<NetworkLog>> GetNetworkLogsAsync();
    Task ClearInterceptionsAsync();
}
```

### 2.2 Request/Response Models
**File:** `EvoAITest.Core/Models/NetworkModels.cs`

```csharp
public sealed record InterceptedRequest
{
    public required string Url { get; init; }
    public required string Method { get; init; }
    public Dictionary<string, string> Headers { get; init; } = new();
    public string? PostData { get; init; }
    public string? ResourceType { get; init; }
}

public sealed record InterceptedResponse
{
    public int Status { get; init; } = 200;
    public Dictionary<string, string> Headers { get; init; } = new();
    public string? Body { get; init; }
    public byte[]? BodyBytes { get; init; }
    public string? ContentType { get; init; }
}

public sealed record MockResponse
{
    public int Status { get; init; } = 200;
    public string? Body { get; init; }
    public Dictionary<string, string>? Headers { get; init; }
    public int? DelayMs { get; init; }
}
```

### 2.3 Implement PlaywrightNetworkInterceptor
**File:** `EvoAITest.Core/Browser/PlaywrightNetworkInterceptor.cs`

```csharp
public sealed class PlaywrightNetworkInterceptor : INetworkInterceptor
{
    private readonly IPage _page;
    private readonly List<NetworkLog> _logs = new();
    
    public async Task InterceptRequestAsync(string pattern, Func<InterceptedRequest, Task<InterceptedResponse?>> handler)
    {
        await _page.RouteAsync(pattern, async route =>
        {
            var request = new InterceptedRequest
            {
                Url = route.Request.Url,
                Method = route.Request.Method,
                Headers = route.Request.Headers,
                PostData = route.Request.PostData
            };
            
            var response = await handler(request);
            
            if (response != null)
            {
                await route.FulfillAsync(new()
                {
                    Status = response.Status,
                    Headers = response.Headers,
                    Body = response.Body
                });
            }
            else
            {
                await route.ContinueAsync();
            }
        });
    }
    
    public async Task BlockRequestAsync(string pattern)
    {
        await _page.RouteAsync(pattern, route => route.AbortAsync());
    }
    
    public async Task MockResponseAsync(string pattern, MockResponse response)
    {
        await _page.RouteAsync(pattern, async route =>
        {
            if (response.DelayMs.HasValue)
            {
                await Task.Delay(response.DelayMs.Value);
            }
            
            await route.FulfillAsync(new()
            {
                Status = response.Status,
                Body = response.Body,
                Headers = response.Headers
            });
        });
    }
}
```

### 2.4 Add Network Tools to Registry
**File:** `EvoAITest.Core/Tools/BrowserToolRegistry.cs`

New tools:
- `mock_response` - Mock API responses
- `block_request` - Block specific requests (ads, trackers)
- `intercept_request` - Modify requests in flight
- `get_network_logs` - Retrieve network activity
- `clear_interceptions` - Clear all route handlers

### 2.5 Integration with Tool Executor
**File:** `EvoAITest.Core/Services/DefaultToolExecutor.cs`

Add support for network tools:
```csharp
case "mock_response":
    var pattern = parameters["pattern"].ToString();
    var mockResponse = JsonSerializer.Deserialize<MockResponse>(parameters["response"].ToString());
    await _networkInterceptor.MockResponseAsync(pattern, mockResponse);
    break;
```

### 2.6 Configuration Options
**File:** `appsettings.json`

```json
{
  "EvoAITest": {
    "Network": {
      "EnableInterception": true,
      "LogAllRequests": false,
      "BlockTrackers": false,
      "MockApiEndpoint": null,
      "DefaultMockResponses": []
    }
  }
}
```

### 2.7 Testing
**File:** `EvoAITest.Tests/Integration/NetworkInterceptionTests.cs`

Test cases:
- Request interception and modification
- Response mocking with different status codes
- Request blocking (ads, images, CSS)
- Network logging
- Simulating API failures
- Latency injection

**Estimated Time:** 10-12 hours

---

## Part 3: Integration and Documentation

### 3.1 DI Registration
**File:** `EvoAITest.Core/ServiceCollectionExtensions.cs`

```csharp
services.AddScoped<INetworkInterceptor, PlaywrightNetworkInterceptor>();
services.Configure<MobileDeviceOptions>(configuration.GetSection("EvoAITest:Mobile"));
services.Configure<NetworkOptions>(configuration.GetSection("EvoAITest:Network"));
```

### 3.2 Update Documentation

#### README.md
Update roadmap section:
```markdown
### ? Phase 2: Enhanced Automation (COMPLETE)
- [x] Mobile browser emulation
- [x] Network interception and mocking
```

#### Create New Guides
1. **Mobile Testing Guide** (`docs/MobileTesting.md`)
   - Device emulation setup
   - Touch event simulation
   - Geolocation testing
   - Orientation changes
   - Mobile-specific selectors

2. **Network Mocking Guide** (`docs/NetworkMocking.md`)
   - Request interception patterns
   - Response mocking examples
   - Simulating network failures
   - Testing offline scenarios
   - API stubbing strategies

3. **API Documentation Updates** (`docs/VisualRegressionAPI.md`)
   - Document new mobile tools
   - Document network interception tools
   - Add code examples (JavaScript, Python, C#)

### 3.3 Examples

#### Mobile Example
**File:** `examples/MobileExample/Program.cs`

```csharp
// Mobile responsive testing example
var task = new AutomationTask
{
    Name = "Mobile Responsive Test",
    NaturalLanguagePrompt = @"
        1. Set device emulation to iPhone 14 Pro
        2. Navigate to example.com
        3. Verify mobile menu is visible
        4. Take screenshot
        5. Switch to iPad Pro
        6. Verify tablet layout
        7. Take screenshot
    "
};
```

#### Network Mocking Example
**File:** `examples/NetworkMockingExample/Program.cs`

```csharp
// API mocking example
var task = new AutomationTask
{
    Name = "API Mocking Test",
    NaturalLanguagePrompt = @"
        1. Mock API endpoint /api/users to return test data
        2. Navigate to dashboard.com
        3. Verify mocked data is displayed
        4. Simulate API failure (500 error)
        5. Verify error handling
    "
};
```

### 3.4 Integration Tests

**File:** `EvoAITest.Tests/Integration/MobileNetworkE2ETests.cs`

Combined scenarios:
- Mobile device with mocked API responses
- Tablet emulation with network latency
- Geolocation + offline mode
- Touch events + slow network

**Estimated Time:** 6-8 hours

---

## Implementation Timeline

### Sprint Breakdown

**Week 1: Mobile Emulation (8-10 hours)**
- Day 1-2: Models and presets (2 hours)
- Day 2-3: PlaywrightBrowserAgent implementation (3 hours)
- Day 3-4: Tool registry and configuration (2 hours)
- Day 4-5: Testing (3 hours)

**Week 2: Network Interception (10-12 hours)**
- Day 1-2: Interfaces and models (2 hours)
- Day 2-4: PlaywrightNetworkInterceptor (4 hours)
- Day 4-5: Tool integration (2 hours)
- Day 5-6: Testing (4 hours)

**Week 3: Integration and Documentation (6-8 hours)**
- Day 1-2: DI and configuration (2 hours)
- Day 2-4: Documentation guides (3 hours)
- Day 4-5: Examples and E2E tests (3 hours)

**Total Estimated Effort:** 24-30 hours (3-4 weeks part-time)

---

## Success Criteria

### Mobile Emulation
? Support for 10+ device presets (iOS, Android, tablets)  
? Viewport, user agent, and device metrics configuration  
? Touch event simulation  
? Geolocation API mocking  
? Orientation change support  
? Platform-specific behavior  

### Network Interception
? Pattern-based request matching  
? Request blocking (ads, trackers)  
? Response mocking (status, headers, body)  
? Request modification in flight  
? Network activity logging  
? Latency injection  
? Offline mode simulation  

### Integration
? Natural language support via AI agents  
? Tool-based execution  
? Configuration-driven defaults  
? Comprehensive test coverage (>90%)  
? Production-ready documentation  

### Documentation
? Mobile testing guide (2,000+ lines)  
? Network mocking guide (2,000+ lines)  
? API documentation updated  
? Code examples (3 languages)  
? Troubleshooting section  

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Playwright API changes | Low | Medium | Pin Playwright version, monitor releases |
| Device profile accuracy | Medium | Low | Use Playwright's built-in profiles, validate with real devices |
| Network interception complexity | Medium | High | Comprehensive testing, clear documentation |
| Performance overhead | Low | Medium | Benchmarking, optional feature flags |
| Testing coverage gaps | Medium | Medium | Integration tests with real browser scenarios |

---

## Dependencies

### External
- Playwright (already installed) - No additional dependencies needed
- .NET 10 - Already in use

### Internal
- IBrowserAgent interface (exists)
- BrowserToolRegistry (exists)
- DefaultToolExecutor (exists)
- Configuration system (exists)
- Testing infrastructure (exists)

---

## Next Steps After Completion

1. ? Update README.md roadmap
2. ? Update DOCUMENTATION_INDEX.md
3. ? Update CHANGELOG.md with v1.1.0 features
4. ? Tag release as v1.1.0
5. ? Announce new features
6. ? Begin Phase 8: CI/CD Integration (if desired)

---

## Code Review Checklist

### Mobile Emulation
- [ ] Device profiles match Playwright's descriptors
- [ ] All device properties properly applied
- [ ] Touch events work correctly
- [ ] Geolocation API functional
- [ ] Configuration validated
- [ ] Tests cover all device types
- [ ] Documentation complete

### Network Interception
- [ ] Route patterns match correctly
- [ ] Request/response modification works
- [ ] Blocking doesn't crash browser
- [ ] Logging captures all requests
- [ ] Mock responses return correctly
- [ ] Latency injection accurate
- [ ] Tests cover all scenarios
- [ ] Documentation with examples

### Integration
- [ ] DI registration correct
- [ ] Configuration binds properly
- [ ] Tool registry updated
- [ ] Natural language support works
- [ ] No breaking changes to existing features
- [ ] Build successful (0 errors, 0 warnings)
- [ ] All tests pass

---

## Performance Benchmarks (Target)

### Mobile Emulation
- Device profile application: <100ms
- Orientation change: <200ms
- Geolocation set: <50ms
- Memory overhead: <10MB per device

### Network Interception
- Route registration: <50ms
- Request interception: <10ms overhead
- Response mocking: <5ms overhead
- Network logging: <1MB memory per 1000 requests

---

## Documentation Updates

### Files to Create
1. `docs/MobileTesting.md` (~2,000 lines)
2. `docs/NetworkMocking.md` (~2,000 lines)
3. `examples/MobileExample/README.md` (~500 lines)
4. `examples/NetworkMockingExample/README.md` (~500 lines)

### Files to Update
1. `README.md` - Update roadmap, add features
2. `DOCUMENTATION_INDEX.md` - Add new guides
3. `CHANGELOG.md` - Add v1.1.0 entry
4. `docs/VisualRegressionAPI.md` - Document new tools
5. `docs/Troubleshooting.md` - Add mobile/network issues

**Total New Documentation:** ~5,000 lines  
**Total Updated Documentation:** ~1,000 lines  

---

## Conclusion

This implementation plan provides a comprehensive roadmap for completing the remaining Phase 2 features. The estimated effort is 24-30 hours over 3-4 weeks, resulting in:

- **Production-ready mobile emulation** with 10+ device presets
- **Full network interception** capabilities for testing and mocking
- **5,000+ lines of documentation** across 4 new guides
- **Comprehensive test coverage** (>90%)
- **Zero breaking changes** to existing functionality

Upon completion, EvoAITest will have best-in-class mobile and network testing capabilities, positioning it as a comprehensive browser automation solution.

---

**Status:** ?? Ready for Implementation  
**Priority:** Medium (completes Phase 2)  
**Estimated Completion:** 3-4 weeks (part-time)  
**Documentation:** High priority (2,000+ lines per feature)  

---

*For questions or clarifications, see the main [README.md](README.md) or [DOCUMENTATION_INDEX.md](DOCUMENTATION_INDEX.md)*
```

This plan provides:

1. **Detailed implementation steps** for both features
2. **Code examples** showing exactly what to implement
3. **Configuration options** for easy customization
4. **Comprehensive testing strategy**
5. **Documentation requirements** (5,000+ new lines)
6. **Timeline** (24-30 hours over 3-4 weeks)
7. **Success criteria** and performance benchmarks
8. **Risk assessment** and mitigation strategies

The plan is production-ready and can be executed immediately. Would you like me to start implementing any specific part of this plan?
