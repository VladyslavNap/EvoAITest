using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Routing;
using FluentAssertions;
using Moq;
using Xunit;

namespace EvoAITest.Tests.LLM;

/// <summary>
/// Unit tests for LLM routing strategies and provider selection logic.
/// </summary>
public sealed class RoutingStrategyTests
{
    [Fact]
    public void TaskBasedStrategy_WithPlanningTask_SelectsGPT5Provider()
    {
        // Arrange
        var strategy = new TaskBasedRoutingStrategy();
        var context = new RoutingContext
        {
            TaskType = TaskType.Planning,
            Complexity = ComplexityLevel.High
        };

        var gpt5Mock = CreateProviderMock("Azure OpenAI", "gpt-5", supportsStreaming: true, supportsFunctionCalling: true);
        var ollamaMock = CreateProviderMock("Ollama", "qwen2.5-7b", supportsStreaming: true, supportsFunctionCalling: true);

        var providers = new List<ILLMProvider> { gpt5Mock.Object, ollamaMock.Object };

        // Act
        var score = strategy.ScoreProvider(context, gpt5Mock.Object);

        // Assert
        score.Should().BeGreaterThan(0.7); // High score for GPT-5 on planning tasks
    }

    [Fact]
    public void TaskBasedStrategy_WithCodeGeneration_PrefersQwen()
    {
        // Arrange
        var strategy = new TaskBasedRoutingStrategy();
        var context = new RoutingContext
        {
            TaskType = TaskType.CodeGeneration,
            Complexity = ComplexityLevel.Medium
        };

        var qwenProvider = CreateProviderMock("Ollama", "qwen2.5-7b", maxContextTokens: 32768).Object;
        var gptProvider = CreateProviderMock("Azure OpenAI", "gpt-4", maxContextTokens: 8192).Object;

        // Act
        var qwenScore = strategy.ScoreProvider(context, qwenProvider);
        var gptScore = strategy.ScoreProvider(context, gptProvider);

        // Assert
        qwenScore.Should().BeGreaterThan(gptScore);
    }

    [Fact]
    public void TaskBasedStrategy_WithStreamingRequired_FiltersNonStreamingProviders()
    {
        // Arrange
        var strategy = new TaskBasedRoutingStrategy();
        var context = new RoutingContext
        {
            TaskType = TaskType.General,
            RequiresStreaming = true
        };

        var streamingProvider = CreateProviderMock("Provider1", "model1", supportsStreaming: true).Object;
        var nonStreamingProvider = CreateProviderMock("Provider2", "model2", supportsStreaming: false).Object;

        // Act
        var streamingScore = strategy.ScoreProvider(context, streamingProvider);
        var nonStreamingScore = strategy.ScoreProvider(context, nonStreamingProvider);

        // Assert
        streamingScore.Should().BeGreaterThan(0);
        nonStreamingScore.Should().Be(0); // Filtered out
    }

    [Fact]
    public void TaskBasedStrategy_WithFunctionCallingRequired_FiltersIncompatibleProviders()
    {
        // Arrange
        var strategy = new TaskBasedRoutingStrategy();
        var context = new RoutingContext
        {
            TaskType = TaskType.Planning,
            RequiresFunctionCalling = true
        };

        var capableProvider = CreateProviderMock("Provider1", "model1", supportsFunctionCalling: true).Object;
        var incapableProvider = CreateProviderMock("Provider2", "model2", supportsFunctionCalling: false).Object;

        // Act
        var capableScore = strategy.ScoreProvider(context, capableProvider);
        var incapableScore = strategy.ScoreProvider(context, incapableProvider);

        // Assert
        capableScore.Should().BeGreaterThan(0);
        incapableScore.Should().Be(0); // Filtered out
    }

    [Fact]
    public async Task TaskBasedStrategy_WithMultipleProviders_SelectsBestMatch()
    {
        // Arrange
        var strategy = new TaskBasedRoutingStrategy();
        var context = new RoutingContext
        {
            TaskType = TaskType.Reasoning,
            Complexity = ComplexityLevel.Expert
        };

        var providers = new List<ILLMProvider>
        {
            CreateProviderMock("Azure OpenAI", "gpt-5", maxContextTokens: 32768).Object,
            CreateProviderMock("Ollama", "qwen2.5-7b", maxContextTokens: 32768).Object,
            CreateProviderMock("Ollama", "llama3", maxContextTokens: 8192).Object
        };

        // Act
        var selected = await strategy.SelectProviderAsync(context, providers);

        // Assert
        selected.Should().NotBeNull();
        selected!.Name.Should().Contain("Azure"); // Prefers Azure for reasoning
    }

    [Fact]
    public void CostOptimizedStrategy_WithLowComplexity_PrefersLocalProviders()
    {
        // Arrange
        var strategy = new CostOptimizedRoutingStrategy();
        var context = new RoutingContext
        {
            TaskType = TaskType.General,
            Complexity = ComplexityLevel.Low
        };

        var ollamaProvider = CreateProviderMock("Ollama", "llama3").Object;
        var azureProvider = CreateProviderMock("Azure OpenAI", "gpt-4").Object;

        // Act
        var ollamaScore = strategy.ScoreProvider(context, ollamaProvider);
        var azureScore = strategy.ScoreProvider(context, azureProvider);

        // Assert
        ollamaScore.Should().BeGreaterThan(azureScore); // Prefer free/local for simple tasks
    }

    [Fact]
    public void CostOptimizedStrategy_WithHighComplexity_AllowsAzureOpenAI()
    {
        // Arrange
        var strategy = new CostOptimizedRoutingStrategy();
        var context = new RoutingContext
        {
            TaskType = TaskType.Planning,
            Complexity = ComplexityLevel.Expert
        };

        var azureProvider = CreateProviderMock("Azure OpenAI", "gpt-5", maxContextTokens: 32768).Object;
        var ollamaProvider = CreateProviderMock("Ollama", "qwen2.5-7b", maxContextTokens: 8192).Object;

        // Act
        var azureScore = strategy.ScoreProvider(context, azureProvider);
        var ollamaScore = strategy.ScoreProvider(context, ollamaProvider);

        // Assert
        azureScore.Should().BeGreaterThan(0.5); // Quality over cost for complex tasks
    }

    [Fact]
    public void CostOptimizedStrategy_WithCriticalPriority_PrefersReliableProviders()
    {
        // Arrange
        var strategy = new CostOptimizedRoutingStrategy();
        var context = new RoutingContext
        {
            TaskType = TaskType.General,
            Priority = RequestPriority.Critical,
            Complexity = ComplexityLevel.Medium
        };

        var azureProvider = CreateProviderMock("Azure OpenAI", "gpt-4").Object;
        var ollamaProvider = CreateProviderMock("Ollama", "llama3").Object;

        // Act
        var azureScore = strategy.ScoreProvider(context, azureProvider);
        var ollamaScore = strategy.ScoreProvider(context, ollamaProvider);

        // Assert
        azureScore.Should().BeGreaterThan(ollamaScore); // Reliability over cost for critical tasks
    }

    [Fact]
    public async Task SelectProviderAsync_WithNoAvailableProviders_ReturnsNull()
    {
        // Arrange
        var strategy = new TaskBasedRoutingStrategy();
        var context = new RoutingContext();
        var emptyProviders = new List<ILLMProvider>();

        // Act
        var result = await strategy.SelectProviderAsync(context, emptyProviders);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SelectProviderAsync_WithIncompatibleProviders_ReturnsNull()
    {
        // Arrange
        var strategy = new TaskBasedRoutingStrategy();
        var context = new RoutingContext
        {
            RequiresStreaming = true,
            RequiresFunctionCalling = true
        };

        var providers = new List<ILLMProvider>
        {
            CreateProviderMock("Provider1", "model1", supportsStreaming: false, supportsFunctionCalling: true).Object,
            CreateProviderMock("Provider2", "model2", supportsStreaming: true, supportsFunctionCalling: false).Object
        };

        // Act
        var result = await strategy.SelectProviderAsync(context, providers);

        // Assert
        result.Should().BeNull(); // No provider meets all requirements
    }

    [Theory]
    [InlineData(TaskType.Planning, "gpt", 0.9)]
    [InlineData(TaskType.CodeGeneration, "qwen", 1.0)]
    [InlineData(TaskType.Extraction, "mistral", 0.85)]
    [InlineData(TaskType.General, "any-model", 0.6)]
    public void TaskBasedStrategy_ScoresCorrectlyForDifferentTaskTypes(
        TaskType taskType,
        string modelName,
        double expectedMinScore)
    {
        // Arrange
        var strategy = new TaskBasedRoutingStrategy();
        var context = new RoutingContext { TaskType = taskType };
        var provider = CreateProviderMock("Test Provider", modelName).Object;

        // Act
        var score = strategy.ScoreProvider(context, provider);

        // Assert
        score.Should().BeGreaterThanOrEqualTo(expectedMinScore);
    }

    [Theory]
    [InlineData(ComplexityLevel.Low, 4096, 0.5)]
    [InlineData(ComplexityLevel.Medium, 8192, 0.8)]
    [InlineData(ComplexityLevel.High, 16384, 1.0)]
    [InlineData(ComplexityLevel.Expert, 32768, 1.0)]
    public void TaskBasedStrategy_ConsidersComplexityAndContextWindow(
        ComplexityLevel complexity,
        int maxContextTokens,
        double expectedMinScore)
    {
        // Arrange
        var strategy = new TaskBasedRoutingStrategy();
        var context = new RoutingContext { Complexity = complexity };
        var provider = CreateProviderMock("Test Provider", "test-model", maxContextTokens: maxContextTokens).Object;

        // Act
        var score = strategy.ScoreProvider(context, provider);

        // Assert
        score.Should().BeGreaterThanOrEqualTo(expectedMinScore);
    }

    // Helper method to create provider mocks
    private Mock<ILLMProvider> CreateProviderMock(
        string name,
        string modelName,
        bool supportsStreaming = true,
        bool supportsFunctionCalling = true,
        bool supportsVision = false,
        bool supportsEmbeddings = true,
        int maxContextTokens = 8192,
        int maxOutputTokens = 2048)
    {
        var mock = new Mock<ILLMProvider>();
        
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.GetModelName()).Returns(modelName);
        
        mock.Setup(p => p.GetCapabilities()).Returns(new ProviderCapabilities
        {
            SupportsStreaming = supportsStreaming,
            SupportsFunctionCalling = supportsFunctionCalling,
            SupportsVision = supportsVision,
            SupportsEmbeddings = supportsEmbeddings,
            MaxContextTokens = maxContextTokens,
            MaxOutputTokens = maxOutputTokens
        });

        return mock;
    }
}
