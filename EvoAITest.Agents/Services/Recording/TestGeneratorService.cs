using EvoAITest.Core.Abstractions;
using EvoAITest.Core.Models.Recording;
using EvoAITest.Core.Options;
using EvoAITest.Core.Services.Recording;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;

namespace EvoAITest.Agents.Services.Recording;

/// <summary>
/// Service for generating test code from recorded user interactions
/// </summary>
public sealed class TestGeneratorService : ITestGenerator
{
    private readonly ILogger<TestGeneratorService> _logger;
    private readonly RecordingOptions _options;

    public TestGeneratorService(
        ILogger<TestGeneratorService> logger,
        IOptions<RecordingOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task<GeneratedTest> GenerateTestAsync(
        RecordingSession session,
        TestGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= CreateDefaultOptions();

        _logger.LogInformation(
            "Generating test for session {SessionId} with {Framework}",
            session.Id,
            options.TestFramework);

        var className = options.ClassName ?? SanitizeClassName(session.Name);
        var methods = new List<GeneratedTestMethod>();

        // Filter interactions by confidence threshold
        var filteredInteractions = session.Interactions
            .Where(i => i.IncludeInTest && i.IntentConfidence >= options.MinimumConfidenceThreshold)
            .ToList();

        if (!filteredInteractions.Any())
        {
            throw new InvalidOperationException("No interactions meet the confidence threshold for test generation");
        }

        // Generate test method
        var testMethod = await GenerateTestMethodFromInteractionsAsync(
            filteredInteractions,
            session.Name,
            options,
            cancellationToken);

        methods.Add(testMethod);

        // Build complete test class
        var testCode = BuildCompleteTestClass(
            className,
            options.Namespace,
            session.Description ?? session.Name,
            methods,
            session.ViewportSize,
            options);

        var generatedTest = new GeneratedTest
        {
            SessionId = session.Id,
            Code = testCode,
            ClassName = className,
            Namespace = options.Namespace,
            Methods = methods,
            Imports = GetRequiredImports(options),
            Metrics = CalculateQualityMetrics(testCode, methods, filteredInteractions)
        };

        _logger.LogInformation(
            "Generated test with {Methods} methods, {Lines} lines of code",
            generatedTest.Methods.Count,
            generatedTest.Metrics.LinesOfCode);

        return generatedTest;
    }

    public async Task<string> GenerateCodeForInteractionAsync(
        UserInteraction interaction,
        TestGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= CreateDefaultOptions();

        var code = interaction.ActionType switch
        {
            ActionType.Click => GenerateClickCode(interaction),
            ActionType.DoubleClick => GenerateDoubleClickCode(interaction),
            ActionType.Input => GenerateInputCode(interaction),
            ActionType.Navigation => GenerateNavigationCode(interaction),
            ActionType.Select => GenerateSelectCode(interaction),
            ActionType.Toggle => GenerateToggleCode(interaction),
            ActionType.Submit => GenerateSubmitCode(interaction),
            _ => GenerateGenericActionCode(interaction)
        };

        if (options.IncludeComments && !string.IsNullOrEmpty(interaction.Description))
        {
            code = $"// {interaction.Description}\n{code}";
        }

        return await Task.FromResult(code);
    }

    public async Task<string> GenerateTestMethodAsync(
        InteractionGroup group,
        TestGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= CreateDefaultOptions();

        var testMethod = await GenerateTestMethodFromInteractionsAsync(
            group.Interactions,
            group.Name,
            options,
            cancellationToken);

        return testMethod.Code;
    }

    public async Task<List<ActionAssertion>> CreateAssertionsAsync(
        UserInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        var assertions = new List<ActionAssertion>();

        switch (interaction.ActionType)
        {
            case ActionType.Navigation:
                assertions.Add(new ActionAssertion
                {
                    Type = AssertionType.UrlEquals,
                    ExpectedValue = interaction.Context.Url,
                    Description = $"Verify navigation to {interaction.Context.Url}",
                    Code = $"Assert.Equal(\"{interaction.Context.Url}\", _page!.Url);"
                });
                break;

            case ActionType.Input:
                if (!string.IsNullOrEmpty(interaction.InputValue) &&
                    !string.IsNullOrEmpty(interaction.Context.TargetSelector))
                {
                    assertions.Add(new ActionAssertion
                    {
                        Type = AssertionType.ValueEquals,
                        Target = interaction.Context.TargetSelector,
                        ExpectedValue = interaction.InputValue,
                        Description = $"Verify input value is '{interaction.InputValue}'",
                        Code = $"await Expect(_page!.Locator(\"{EscapeString(interaction.Context.TargetSelector)}\")).ToHaveValueAsync(\"{EscapeString(interaction.InputValue)}\");"
                    });
                }
                break;
        }

        return await Task.FromResult(assertions);
    }

    public async Task<TestValidationResult> ValidateGeneratedTestAsync(
        GeneratedTest generatedTest,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        var warnings = new List<string>();
        var suggestions = new List<string>();

        if (!generatedTest.Methods.Any())
        {
            errors.Add("No test methods generated");
        }

        var methodsWithoutAssertions = generatedTest.Methods
            .Where(m => !m.Code.Contains("Assert") && !m.Code.Contains("Expect"))
            .ToList();

        foreach (var method in methodsWithoutAssertions)
        {
            warnings.Add($"Method '{method.Name}' has no assertions");
        }

        return await Task.FromResult(new TestValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors,
            Warnings = warnings,
            Suggestions = suggestions
        });
    }

    #region Code Generation Methods

    private async Task<GeneratedTestMethod> GenerateTestMethodFromInteractionsAsync(
        List<UserInteraction> interactions,
        string methodName,
        TestGenerationOptions options,
        CancellationToken cancellationToken)
    {
        var sanitizedName = SanitizeMethodName(methodName);
        var arrangeCode = new StringBuilder();
        var actCode = new StringBuilder();
        var assertCode = new StringBuilder();

        var firstUrl = interactions.First().Context.Url;
        arrangeCode.AppendLine($"        await _page!.GotoAsync(\"{firstUrl}\");");

        foreach (var interaction in interactions)
        {
            var code = await GenerateCodeForInteractionAsync(interaction, options, cancellationToken);

            foreach (var line in code.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                actCode.AppendLine($"        {line.TrimStart()}");
            }

            if (options.AutoGenerateAssertions)
            {
                var assertions = await CreateAssertionsAsync(interaction, cancellationToken);
                var validAssertions = assertions
                    .Where(a => !string.IsNullOrEmpty(a.Code))
                    .ToList();

                foreach (var assertion in validAssertions)
                {
                    assertCode.AppendLine($"        {assertion.Code}");
                }
            }
        }

        if (assertCode.Length == 0)
        {
            assertCode.AppendLine("        // TODO: Add assertions to verify expected behavior");
        }

        var description = interactions.First().Description ?? methodName;
        var methodTemplate = GetMethodTemplate(options.TestFramework);

        var methodCode = methodTemplate
            .Replace("{methodName}", sanitizedName)
            .Replace("{description}", description)
            .Replace("{arrangeCode}", arrangeCode.ToString().TrimEnd())
            .Replace("{actCode}", actCode.ToString().TrimEnd())
            .Replace("{assertCode}", assertCode.ToString().TrimEnd());

        return new GeneratedTestMethod
        {
            Name = sanitizedName,
            Code = methodCode,
            Description = description,
            InteractionIds = interactions.Select(i => i.Id).ToList()
        };
    }

    private string BuildCompleteTestClass(
        string className,
        string namespaceName,
        string description,
        List<GeneratedTestMethod> methods,
        (int Width, int Height) viewportSize,
        TestGenerationOptions options)
    {
        var template = GetClassTemplate(options.TestFramework);
        var methodsCode = string.Join("\n\n", methods.Select(m => m.Code));

        return template
            .Replace("{namespace}", namespaceName)
            .Replace("{className}", className)
            .Replace("{description}", description)
            .Replace("{viewportWidth}", viewportSize.Width.ToString())
            .Replace("{viewportHeight}", viewportSize.Height.ToString())
            .Replace("{testMethods}", methodsCode);
    }

    private string GenerateClickCode(UserInteraction interaction)
    {
        var selector = EscapeString(interaction.Context.TargetSelector ?? interaction.Context.TargetXPath!);
        return $"await _page!.Locator(\"{selector}\").ClickAsync();";
    }

    private string GenerateDoubleClickCode(UserInteraction interaction)
    {
        var selector = EscapeString(interaction.Context.TargetSelector ?? interaction.Context.TargetXPath!);
        return $"await _page!.Locator(\"{selector}\").DblClickAsync();";
    }

    private string GenerateInputCode(UserInteraction interaction)
    {
        var selector = EscapeString(interaction.Context.TargetSelector ?? interaction.Context.TargetXPath!);
        var value = EscapeString(interaction.InputValue ?? string.Empty);
        return $"await _page!.Locator(\"{selector}\").FillAsync(\"{value}\");";
    }

    private string GenerateNavigationCode(UserInteraction interaction)
    {
        return $"await _page!.GotoAsync(\"{EscapeString(interaction.Context.Url)}\");";
    }

    private string GenerateSelectCode(UserInteraction interaction)
    {
        var selector = EscapeString(interaction.Context.TargetSelector ?? interaction.Context.TargetXPath!);
        var value = EscapeString(interaction.InputValue ?? string.Empty);
        return $"await _page!.Locator(\"{selector}\").SelectOptionAsync(\"{value}\");";
    }

    private string GenerateToggleCode(UserInteraction interaction)
    {
        var selector = EscapeString(interaction.Context.TargetSelector ?? interaction.Context.TargetXPath!);
        return $"await _page!.Locator(\"{selector}\").CheckAsync();";
    }

    private string GenerateSubmitCode(UserInteraction interaction)
    {
        var selector = EscapeString(interaction.Context.TargetSelector ?? "form");
        return $"await _page!.Locator(\"{selector}\").PressAsync(\"Enter\");";
    }

    private string GenerateGenericActionCode(UserInteraction interaction)
    {
        return $"// {interaction.ActionType}: {interaction.Description ?? "Action"}";
    }

    #endregion

    #region Helper Methods

    private string GetClassTemplate(string framework) => framework.ToLowerInvariant() switch
    {
        "xunit" => TestCodeTemplates.XUnitPlaywrightTemplate,
        "nunit" => TestCodeTemplates.NUnitPlaywrightTemplate,
        "mstest" => TestCodeTemplates.MSTestPlaywrightTemplate,
        _ => TestCodeTemplates.XUnitPlaywrightTemplate
    };

    private string GetMethodTemplate(string framework) => framework.ToLowerInvariant() switch
    {
        "xunit" => TestCodeTemplates.XUnitTestMethodTemplate,
        "nunit" => TestCodeTemplates.NUnitTestMethodTemplate,
        "mstest" => TestCodeTemplates.MSTestTestMethodTemplate,
        _ => TestCodeTemplates.XUnitTestMethodTemplate
    };

    private List<string> GetRequiredImports(TestGenerationOptions options)
    {
        return
        [
            "System",
            "System.Threading.Tasks",
            "Microsoft.Playwright",
            options.TestFramework.ToLowerInvariant() switch
            {
                "xunit" => "Xunit",
                "nunit" => "NUnit.Framework",
                "mstest" => "Microsoft.VisualStudio.TestTools.UnitTesting",
                _ => "Xunit"
            }
        ];
    }

    private TestQualityMetrics CalculateQualityMetrics(
        string code,
        List<GeneratedTestMethod> methods,
        List<UserInteraction> interactions)
    {
        var lines = code.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Count();
        var assertionCount = Regex.Matches(code, @"Assert\.|Expect\(").Count;

        var maintainabilityScore = 100.0;
        maintainabilityScore -= Math.Max(0, (lines - 200) * 0.1);
        maintainabilityScore += Math.Min(20, assertionCount * 2);
        maintainabilityScore = Math.Max(0, Math.Min(100, maintainabilityScore));

        return new TestQualityMetrics
        {
            TestMethodCount = methods.Count,
            AssertionCount = assertionCount,
            EstimatedCoverage = (double)interactions.Count / 10 * 100,
            MaintainabilityScore = maintainabilityScore,
            LinesOfCode = lines
        };
    }

    private TestGenerationOptions CreateDefaultOptions()
    {
        return new TestGenerationOptions
        {
            TestFramework = _options.DefaultTestFramework,
            Language = _options.DefaultLanguage,
            AutomationLibrary = _options.DefaultAutomationLibrary,
            MinimumConfidenceThreshold = _options.MinimumConfidenceThreshold
        };
    }

    private string SanitizeClassName(string name)
    {
        var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");
        if (string.IsNullOrEmpty(sanitized) || char.IsDigit(sanitized[0]))
        {
            sanitized = "_" + sanitized;
        }
        return sanitized + "Tests";
    }

    private string SanitizeMethodName(string name)
    {
        var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");
        if (string.IsNullOrEmpty(sanitized) || char.IsDigit(sanitized[0]))
        {
            sanitized = "Test" + sanitized;
        }
        return sanitized;
    }

    private string EscapeString(string value)
    {
        return value.Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
    }

    #endregion
}
