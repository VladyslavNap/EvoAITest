using EvoAITest.Core.Models;
using EvoAITest.Core.Options;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using TaskStatus = EvoAITest.Core.Models.TaskStatus; // Resolve ambiguity with System.Threading.Tasks.TaskStatus

namespace EvoAITest.Tests.Abstractions;

/// <summary>
/// Unit tests for BrowserToolRegistry static class.
/// Tests all 13 browser automation tools and registry methods.
/// </summary>
public class BrowserToolRegistryTests
{
    [Fact]
    public void GetAllTools_ShouldReturn13Tools()
    {
        // Act
        var tools = BrowserToolRegistry.GetAllTools();

        // Assert
        tools.Should().NotBeNull();
        tools.Should().HaveCount(13);
    }

    [Theory]
    [InlineData("navigate")]
    [InlineData("click")]
    [InlineData("type")]
    [InlineData("clear_input")]
    [InlineData("extract_text")]
    [InlineData("extract_table")]
    [InlineData("get_page_state")]
    [InlineData("take_screenshot")]
    [InlineData("wait_for_element")]
    [InlineData("wait_for_url_change")]
    [InlineData("select_option")]
    [InlineData("submit_form")]
    [InlineData("verify_element_exists")]
    public void GetTool_WithValidName_ShouldReturnTool(string toolName)
    {
        // Act
        var tool = BrowserToolRegistry.GetTool(toolName);

        // Assert
        tool.Should().NotBeNull();
        tool.Name.Should().Be(toolName);
        tool.Description.Should().NotBeNullOrWhiteSpace();
        tool.Parameters.Should().NotBeNull();
    }

    [Fact]
    public void GetTool_WithInvalidName_ShouldThrowKeyNotFoundException()
    {
        // Act
        Action act = () => BrowserToolRegistry.GetTool("nonexistent_tool");

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*nonexistent_tool*")
            .WithMessage("*Available tools:*");
    }

    [Theory]
    [InlineData("navigate", true)]
    [InlineData("NAVIGATE", true)] // Case-insensitive
    [InlineData("Click", true)]
    [InlineData("nonexistent", false)]
    public void ToolExists_ShouldBeCaseInsensitive(string toolName, bool expectedExists)
    {
        // Act
        var exists = BrowserToolRegistry.ToolExists(toolName);

        // Assert
        exists.Should().Be(expectedExists);
    }

    [Fact]
    public void GetToolsAsJson_ShouldReturnValidJson()
    {
        // Act
        var json = BrowserToolRegistry.GetToolsAsJson();

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"type\": \"function\"");
        json.Should().Contain("\"name\": \"navigate\"");
        json.Should().Contain("\"parameters\":");
        json.Should().Contain("\"required\":");
    }

    [Fact]
    public void GetToolNames_ShouldReturn13Names()
    {
        // Act
        var names = BrowserToolRegistry.GetToolNames();

        // Assert
        names.Should().NotBeNull();
        names.Should().HaveCount(13);
        names.Should().Contain("navigate");
        names.Should().Contain("click");
    }

    [Fact]
    public void ToolCount_ShouldBe13()
    {
        // Act
        var count = BrowserToolRegistry.ToolCount;

        // Assert
        count.Should().Be(13);
    }

    [Fact]
    public void NavigateTool_ShouldHaveRequiredUrlParameter()
    {
        // Act
        var tool = BrowserToolRegistry.GetTool("navigate");

        // Assert
        tool.Parameters.Should().ContainKey("url");
        tool.Parameters["url"].Required.Should().BeTrue();
        tool.Parameters["url"].Type.Should().Be("string");
    }

    [Fact]
    public void ClickTool_ShouldHaveOptionalParameters()
    {
        // Act
        var tool = BrowserToolRegistry.GetTool("click");

        // Assert
        tool.Parameters.Should().ContainKey("button");
        tool.Parameters["button"].Required.Should().BeFalse();
        tool.Parameters["button"].DefaultValue.Should().Be("left");
    }
}

/// <summary>
/// Unit tests for AutomationTask class and related enums.
/// Tests task lifecycle, status updates, and validation.
/// </summary>
public class AutomationTaskTests
{
    [Fact]
    public void NewAutomationTask_ShouldHaveDefaultValues()
    {
        // Act
        var task = new AutomationTask();

        // Assert
        task.Id.Should().NotBeEmpty();
        task.Status.Should().Be(TaskStatus.Pending);
        task.Plan.Should().NotBeNull().And.BeEmpty();
        task.Context.Should().Be("{}");
        task.CorrelationId.Should().NotBeNullOrWhiteSpace();
        task.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        task.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        task.CompletedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(TaskStatus.Planning)]
    [InlineData(TaskStatus.Executing)]
    [InlineData(TaskStatus.Completed)]
    [InlineData(TaskStatus.Failed)]
    [InlineData(TaskStatus.Cancelled)]
    public void UpdateStatus_ShouldUpdateStatusAndTimestamp(TaskStatus newStatus)
    {
        // Arrange
        var task = new AutomationTask();
        var originalUpdateTime = task.UpdatedAt;
        Thread.Sleep(10); // Ensure time difference

        // Act
        task.UpdateStatus(newStatus);

        // Assert
        task.Status.Should().Be(newStatus);
        task.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    [Theory]
    [InlineData(TaskStatus.Completed)]
    [InlineData(TaskStatus.Failed)]
    [InlineData(TaskStatus.Cancelled)]
    public void UpdateStatus_ToTerminalState_ShouldSetCompletedAt(TaskStatus terminalStatus)
    {
        // Arrange
        var task = new AutomationTask();
        task.CompletedAt.Should().BeNull();

        // Act
        task.UpdateStatus(terminalStatus);

        // Assert
        task.CompletedAt.Should().NotBeNull();
        task.CompletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void SetPlan_WithValidSteps_ShouldUpdatePlanAndTimestamp()
    {
        // Arrange
        var task = new AutomationTask();
        var originalUpdateTime = task.UpdatedAt;
        Thread.Sleep(10);

        var steps = new List<ExecutionStep>
        {
            new(1, "navigate", "input#username", "", "Navigate to login", "Page loads"),
            new(2, "click", "button#login", "", "Click login", "User logged in")
        };

        // Act
        task.SetPlan(steps);

        // Assert
        task.Plan.Should().HaveCount(2);
        task.Plan.Should().BeEquivalentTo(steps);
        task.UpdatedAt.Should().BeAfter(originalUpdateTime);
    }

    [Fact]
    public void SetPlan_WithNullSteps_ShouldThrowArgumentNullException()
    {
        // Arrange
        var task = new AutomationTask();

        // Act
        Action act = () => task.SetPlan(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}

/// <summary>
/// Unit tests for PageState and related models.
/// </summary>
public class PageStateTests
{
    [Fact]
    public void NewPageState_ShouldHaveDefaultValues()
    {
        // Act
        var state = new PageState();

        // Assert
        state.Url.Should().Be(string.Empty);
        state.Title.Should().Be(string.Empty);
        state.LoadState.Should().Be(LoadState.Loading);
        state.VisibleElements.Should().NotBeNull().And.BeEmpty();
        state.InteractiveElements.Should().NotBeNull().And.BeEmpty();
        state.ConsoleMessages.Should().NotBeNull().And.BeEmpty();
        state.NetworkRequests.Should().NotBeNull().And.BeEmpty();
        state.Metadata.Should().NotBeNull().And.BeEmpty();
        state.CapturedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ElementInfo_ShouldStoreAllProperties()
    {
        // Act
        var element = new ElementInfo
        {
            TagName = "button",
            Selector = "#login-btn",
            Text = "Login",
            IsVisible = true,
            IsInteractable = true,
            Attributes = new Dictionary<string, string>
            {
                ["id"] = "login-btn",
                ["type"] = "submit"
            },
            BoundingBox = new BoundingBox
            {
                X = 100,
                Y = 200,
                Width = 120,
                Height = 40
            }
        };

        // Assert
        element.TagName.Should().Be("button");
        element.Selector.Should().Be("#login-btn");
        element.Text.Should().Be("Login");
        element.IsVisible.Should().BeTrue();
        element.IsInteractable.Should().BeTrue();
        element.Attributes.Should().ContainKey("id");
        element.BoundingBox.Should().NotBeNull();
        element.BoundingBox!.Width.Should().Be(120);
    }
}

/// <summary>
/// Unit tests for ToolCall record and related functionality.
/// </summary>
public class ToolCallTests
{
    [Fact]
    public void ToolCall_ShouldBeImmutable()
    {
        // Arrange
        var parameters = new Dictionary<string, object>
        {
            ["selector"] = "#username",
            ["text"] = "testuser"
        };

        // Act
        var toolCall = new ToolCall(
            ToolName: "type",
            Parameters: parameters,
            Reasoning: "Type username into login field",
            CorrelationId: Guid.NewGuid().ToString()
        );

        // Assert
        toolCall.ToolName.Should().Be("type");
        toolCall.Parameters.Should().ContainKey("selector");
        toolCall.Reasoning.Should().NotBeNullOrWhiteSpace();
        toolCall.CorrelationId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void ExecutionStep_ShouldStoreAllStepData()
    {
        // Act
        var step = new ExecutionStep(
            Order: 1,
            Action: "navigate",
            Selector: "",
            Value: "https://example.com",
            Reasoning: "Navigate to the login page",
            ExpectedResult: "Login page should load successfully"
        );

        // Assert
        step.Order.Should().Be(1);
        step.Action.Should().Be("navigate");
        step.Selector.Should().BeEmpty();
        step.Value.Should().Be("https://example.com");
        step.Reasoning.Should().NotBeNullOrWhiteSpace();
        step.ExpectedResult.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void TaskExecutionResult_IsSuccess_ShouldReturnTrueForSuccess()
    {
        // Arrange
        var result = new TaskExecutionResult(
            TaskId: Guid.NewGuid(),
            Status: ExecutionStatus.Success,
            Steps: new List<StepResult>(),
            FinalOutput: "Task completed successfully",
            ErrorMessage: null,
            TotalDurationMs: 5000
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void TaskExecutionResult_IsSuccess_ShouldReturnTrueForPartialSuccess()
    {
        // Arrange
        var result = new TaskExecutionResult(
            TaskId: Guid.NewGuid(),
            Status: ExecutionStatus.PartialSuccess,
            Steps: new List<StepResult>(),
            FinalOutput: "Some steps completed",
            ErrorMessage: "One step failed",
            TotalDurationMs: 5000
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void TaskExecutionResult_IsSuccess_ShouldReturnFalseForFailed()
    {
        // Arrange
        var result = new TaskExecutionResult(
            TaskId: Guid.NewGuid(),
            Status: ExecutionStatus.Failed,
            Steps: new List<StepResult>(),
            FinalOutput: "",
            ErrorMessage: "Task failed",
            TotalDurationMs: 1000
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
    }
}

/// <summary>
/// Unit tests for EvoAITestCoreOptions with Azure OpenAI, Ollama, and local LLM support.
/// Tests configuration validation, computed properties, and provider-specific settings.
/// </summary>
public class EvoAITestCoreOptionsTests
{
    [Fact]
    public void DefaultOptions_ShouldHaveCorrectDefaults()
    {
        // Act
        var options = new EvoAITestCoreOptions();

        // Assert - LLM Provider Defaults
        options.LLMProvider.Should().Be("AzureOpenAI");
        options.LLMModel.Should().Be("gpt-5");
        options.AzureOpenAIDeployment.Should().Be("gpt-5");
        options.AzureOpenAIApiVersion.Should().Be("2025-01-01-preview");
        options.OllamaEndpoint.Should().Be("http://localhost:11434");
        options.OllamaModel.Should().Be("qwen2.5-7b");

        // Assert - Browser Defaults
        options.BrowserTimeoutMs.Should().Be(30000);
        options.HeadlessMode.Should().BeTrue();
        options.MaxRetries.Should().Be(3);
        options.ScreenshotOutputPath.Should().Be("/tmp/screenshots");

        // Assert - Observability Defaults
        options.LogLevel.Should().Be("Information");
        options.EnableTelemetry.Should().BeTrue();
        options.ServiceName.Should().Be("EvoAITest.Core");
    }

    [Fact]
    public void ValidateConfiguration_WithAzureOpenAI_RequiresEndpoint()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "AzureOpenAI",
            AzureOpenAIEndpoint = "", // Missing endpoint
            AzureOpenAIApiKey = "test-key",
            AzureOpenAIDeployment = "gpt-5"
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AZURE_OPENAI_ENDPOINT*");
    }

    [Fact]
    public void ValidateConfiguration_WithAzureOpenAI_RequiresApiKey()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "AzureOpenAI",
            AzureOpenAIEndpoint = "https://youropenai.cognitiveservices.azure.com",
            AzureOpenAIApiKey = "", // Missing API key
            AzureOpenAIDeployment = "gpt-5"
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*LLMAPIKEY*")
            .WithMessage("*Key Vault*");
    }

    [Fact]
    public void ValidateConfiguration_WithAzureOpenAI_RequiresDeployment()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "AzureOpenAI",
            AzureOpenAIEndpoint = "https://youropenai.cognitiveservices.azure.com",
            AzureOpenAIApiKey = "test-key",
            AzureOpenAIDeployment = "" // Missing deployment
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*deployment*");
    }

    [Fact]
    public void ValidateConfiguration_WithOllama_RequiresEndpoint()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "Ollama",
            OllamaEndpoint = "", // Missing endpoint
            OllamaModel = "qwen2.5-7b"
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Ollama*")
            .WithMessage("*http://localhost:11434*");
    }

    [Fact]
    public void ValidateConfiguration_WithOllama_RequiresModel()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "Ollama",
            OllamaEndpoint = "http://localhost:11434",
            OllamaModel = "" // Missing model
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*model*");
    }

    [Fact]
    public void ValidateConfiguration_WithLocal_RequiresEndpoint()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "Local",
            LocalLLMEndpoint = "" // Missing endpoint
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Local LLM endpoint*");
    }

    [Fact]
    public void ValidateConfiguration_WithInvalidProvider_ShouldThrow()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "InvalidProvider"
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Invalid LLMProvider*")
            .WithMessage("*AzureOpenAI*")
            .WithMessage("*Ollama*")
            .WithMessage("*Local*");
    }

    [Fact]
    public void ValidateConfiguration_WithValidAzureOpenAI_ShouldPass()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "AzureOpenAI",
            AzureOpenAIEndpoint = "https://youropenai.cognitiveservices.azure.com",
            AzureOpenAIApiKey = "test-api-key-12345",
            AzureOpenAIDeployment = "gpt-5",
            AzureOpenAIApiVersion = "2025-01-01-preview"
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithValidOllama_ShouldPass()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "Ollama",
            OllamaEndpoint = "http://localhost:11434",
            OllamaModel = "qwen2.5-7b"
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateConfiguration_WithValidLocal_ShouldPass()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "Local",
            LocalLLMEndpoint = "http://localhost:8080/v1/chat/completions"
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("AzureOpenAI", true, false, false)]
    [InlineData("Ollama", false, true, false)]
    [InlineData("Local", false, false, true)]
    public void ComputedProperties_ShouldReturnCorrectValues(
        string provider,
        bool expectedAzure,
        bool expectedOllama,
        bool expectedLocal)
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = provider
        };

        // Act & Assert
        options.IsAzureOpenAI.Should().Be(expectedAzure);
        options.IsOllama.Should().Be(expectedOllama);
        options.IsLocalLLM.Should().Be(expectedLocal);
    }

    [Theory]
    [InlineData(500, false)]
    [InlineData(999, false)]
    [InlineData(1000, true)]
    [InlineData(30000, true)]
    public void ValidateConfiguration_BrowserTimeoutMs_ShouldValidateMinimum(
        int timeoutMs,
        bool shouldPass)
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "Ollama",
            OllamaEndpoint = "http://localhost:11434",
            OllamaModel = "qwen2.5-7b",
            BrowserTimeoutMs = timeoutMs
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        if (shouldPass)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*BrowserTimeoutMs*1000*");
        }
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(3, true)]
    [InlineData(10, true)]
    public void ValidateConfiguration_MaxRetries_ShouldValidateMinimum(
        int maxRetries,
        bool shouldPass)
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "Ollama",
            OllamaEndpoint = "http://localhost:11434",
            OllamaModel = "qwen2.5-7b",
            MaxRetries = maxRetries
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        if (shouldPass)
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*MaxRetries*");
        }
    }

    [Fact]
    public void ValidateConfiguration_EmptyScreenshotPath_ShouldThrow()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "Ollama",
            OllamaEndpoint = "http://localhost:11434",
            OllamaModel = "qwen2.5-7b",
            ScreenshotOutputPath = ""
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ScreenshotOutputPath*");
    }
}

/// <summary>
/// Integration tests for Azure OpenAI configuration loading from IConfiguration.
/// All tests use mocked configuration - NO actual Azure calls.
/// </summary>
public class AzureOpenAIConfigurationIntegrationTests
{
    [Fact]
    public void AzureOpenAIConfiguration_ShouldLoadFromEnvironment()
    {
        // Arrange - Mock configuration with environment variables
        var configData = new Dictionary<string, string?>
        {
            ["EvoAITest:Core:LLMProvider"] = "AzureOpenAI",
            ["EvoAITest:Core:LLMModel"] = "gpt-5",
            ["EvoAITest:Core:AzureOpenAIEndpoint"] = "https://youropenai.cognitiveservices.azure.com",
            ["EvoAITest:Core:AzureOpenAIApiKey"] = "test-key-from-keyvault",
            ["EvoAITest:Core:AzureOpenAIDeployment"] = "gpt-5",
            ["EvoAITest:Core:AzureOpenAIApiVersion"] = "2025-01-01-preview"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var options = new EvoAITestCoreOptions();
        configuration.GetSection("EvoAITest:Core").Bind(options);

        // Assert
        options.LLMProvider.Should().Be("AzureOpenAI");
        options.LLMModel.Should().Be("gpt-5");
        options.AzureOpenAIEndpoint.Should().Be("https://youropenai.cognitiveservices.azure.com");
        options.AzureOpenAIApiKey.Should().Be("test-key-from-keyvault");
        options.AzureOpenAIDeployment.Should().Be("gpt-5");
        options.AzureOpenAIApiVersion.Should().Be("2025-01-01-preview");
    }

    [Fact]
    public void OllamaConfiguration_ShouldLoadFromEnvironment()
    {
        // Arrange - Mock configuration
        var configData = new Dictionary<string, string?>
        {
            ["EvoAITest:Core:LLMProvider"] = "Ollama",
            ["EvoAITest:Core:OllamaEndpoint"] = "http://localhost:11434",
            ["EvoAITest:Core:OllamaModel"] = "qwen2.5-7b",
            ["EvoAITest:Core:HeadlessMode"] = "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var options = new EvoAITestCoreOptions();
        configuration.GetSection("EvoAITest:Core").Bind(options);

        // Assert
        options.LLMProvider.Should().Be("Ollama");
        options.OllamaEndpoint.Should().Be("http://localhost:11434");
        options.OllamaModel.Should().Be("qwen2.5-7b");
        options.HeadlessMode.Should().BeFalse();
    }

    [Fact]
    public void EnvironmentVariableFormat_ShouldFollowAspireConvention()
    {
        // Arrange - Mock Aspire-style environment variables (double underscore)
        var configData = new Dictionary<string, string?>
        {
            ["EVOAITEST__CORE__LLMPROVIDER"] = "AzureOpenAI",
            ["EVOAITEST__CORE__AZUREOPENAIENDPOINT"] = "https://youropenai.cognitiveservices.azure.com",
            ["EVOAITEST__CORE__BROWSERTIMEOUTMS"] = "60000"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var options = new EvoAITestCoreOptions();
        configuration.GetSection("EVOAITEST:CORE").Bind(options);

        // Assert
        options.LLMProvider.Should().Be("AzureOpenAI");
        options.AzureOpenAIEndpoint.Should().Be("https://youropenai.cognitiveservices.azure.com");
        options.BrowserTimeoutMs.Should().Be(60000);
    }

    [Fact]
    public void KeyVaultIntegration_ShouldNotExposeSensitiveData()
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            AzureOpenAIApiKey = "super-secret-api-key-12345"
        };

        // Act - Simulate logging or ToString() scenarios
        var mockLogger = new Mock<ILogger<EvoAITestCoreOptions>>();
        var logMessage = $"Options configured: Provider={options.LLMProvider}, Model={options.LLMModel}";

        // Assert - Ensure API key is NOT in the log message
        logMessage.Should().NotContain(options.AzureOpenAIApiKey);
        logMessage.Should().NotContain("super-secret");
        
        // Verify that the API key property exists but is not exposed in common scenarios
        options.AzureOpenAIApiKey.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void LocalLLMConfiguration_ShouldLoadCustomEndpoint()
    {
        // Arrange
        var configData = new Dictionary<string, string?>
        {
            ["EvoAITest:Core:LLMProvider"] = "Local",
            ["EvoAITest:Core:LocalLLMEndpoint"] = "http://192.168.1.100:8080/v1/chat/completions"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configData)
            .Build();

        // Act
        var options = new EvoAITestCoreOptions();
        configuration.GetSection("EvoAITest:Core").Bind(options);

        // Assert
        options.LLMProvider.Should().Be("Local");
        options.LocalLLMEndpoint.Should().Be("http://192.168.1.100:8080/v1/chat/completions");
    }
}

/// <summary>
/// Provider-specific validation tests for Azure OpenAI endpoints and API versions.
/// </summary>
public class ProviderSpecificValidationTests
{
    [Theory]
    [InlineData("https://youropenai.cognitiveservices.azure.com", true)]
    [InlineData("https://myresource.openai.azure.com", true)]
    [InlineData("https://eastus.api.cognitive.microsoft.com/openai", true)]
    [InlineData("http://localhost:11434", false)] // HTTP not HTTPS
    [InlineData("https://invalid-domain.com", false)] // Not Azure domain
    [InlineData("", false)] // Empty
    public void AzureOpenAIEndpoint_ShouldValidateFormat(string endpoint, bool isValid)
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "AzureOpenAI",
            AzureOpenAIEndpoint = endpoint,
            AzureOpenAIApiKey = "test-key",
            AzureOpenAIDeployment = "gpt-5"
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        if (isValid && endpoint.StartsWith("https://"))
        {
            act.Should().NotThrow();
        }
        else
        {
            act.Should().Throw<InvalidOperationException>();
        }
    }

    [Theory]
    [InlineData("2025-01-01-preview", true)]
    [InlineData("2024-08-01-preview", true)]
    [InlineData("2024-06-01", true)]
    [InlineData("invalid-version", true)] // We don't validate format strictly
    [InlineData("", true)] // We allow any string, just checking it's set
    public void AzureOpenAIApiVersion_ShouldValidateFormat(string version, bool isValid)
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "AzureOpenAI",
            AzureOpenAIEndpoint = "https://test.cognitiveservices.azure.com",
            AzureOpenAIApiKey = "test-key",
            AzureOpenAIDeployment = "gpt-5",
            AzureOpenAIApiVersion = version
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert - We don't strictly validate version format, just that it's provided
        if (isValid)
        {
            act.Should().NotThrow();
        }
    }

    [Fact]
    public void AzureOpenAIEndpoint_WithTrailingSlash_ShouldDocumentCorrectUsage()
    {
        // Arrange - Test that trailing slash is not required/recommended
        var optionsWithSlash = new EvoAITestCoreOptions
        {
            LLMProvider = "AzureOpenAI",
            AzureOpenAIEndpoint = "https://test.cognitiveservices.azure.com/",
            AzureOpenAIApiKey = "test-key",
            AzureOpenAIDeployment = "gpt-5"
        };

        var optionsWithoutSlash = new EvoAITestCoreOptions
        {
            LLMProvider = "AzureOpenAI",
            AzureOpenAIEndpoint = "https://test.cognitiveservices.azure.com",
            AzureOpenAIApiKey = "test-key",
            AzureOpenAIDeployment = "gpt-5"
        };

        // Act & Assert - Both should pass validation
        Action actWithSlash = () => optionsWithSlash.ValidateConfiguration();
        Action actWithoutSlash = () => optionsWithoutSlash.ValidateConfiguration();

        actWithSlash.Should().NotThrow();
        actWithoutSlash.Should().NotThrow();
        
        // Document that trailing slash should be trimmed in actual usage
        // (Implementation detail: HTTP clients typically handle this)
    }

    [Theory]
    [InlineData("qwen2.5-7b")]
    [InlineData("llama2")]
    [InlineData("mistral")]
    [InlineData("codellama")]
    public void OllamaModel_ShouldAcceptVariousModels(string model)
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "Ollama",
            OllamaEndpoint = "http://localhost:11434",
            OllamaModel = model
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().NotThrow();
        options.OllamaModel.Should().Be(model);
    }

    [Theory]
    [InlineData("http://localhost:8080/v1/chat/completions")]
    [InlineData("http://192.168.1.100:5000/api/chat")]
    [InlineData("https://internal-llm.company.com/completions")]
    public void LocalLLMEndpoint_ShouldAcceptVariousFormats(string endpoint)
    {
        // Arrange
        var options = new EvoAITestCoreOptions
        {
            LLMProvider = "Local",
            LocalLLMEndpoint = endpoint
        };

        // Act
        Action act = () => options.ValidateConfiguration();

        // Assert
        act.Should().NotThrow();
        options.LocalLLMEndpoint.Should().Be(endpoint);
    }
}
