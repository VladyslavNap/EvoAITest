using System.Text;
using System.Text.Json;
using EvoAITest.Core.Models;
using EvoAITest.Core.Models.Vision;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;

namespace EvoAITest.LLM.Vision;

/// <summary>
/// GPT-4 Vision provider for analyzing screenshots and detecting UI elements.
/// Uses Azure OpenAI's GPT-4 Vision capabilities.
/// </summary>
public sealed class GPT4VisionProvider
{
    private readonly ILLMProvider _llmProvider;
    private readonly ILogger<GPT4VisionProvider> _logger;

    public GPT4VisionProvider(
        ILLMProvider llmProvider,
        ILogger<GPT4VisionProvider> logger)
    {
        _llmProvider = llmProvider ?? throw new ArgumentNullException(nameof(llmProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Analyzes a screenshot to detect all interactive UI elements.
    /// </summary>
    public async Task<List<DetectedElement>> DetectElementsAsync(
        byte[] screenshot,
        ElementFilter? filter = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting elements in screenshot ({Size} bytes)", screenshot.Length);

        var base64Image = Convert.ToBase64String(screenshot);
        var prompt = BuildElementDetectionPrompt(filter);

        try
        {
            var response = await SendVisionRequestAsync(base64Image, prompt, cancellationToken);
            var elements = ParseElementDetectionResponse(response);

            _logger.LogInformation("Detected {Count} elements", elements.Count);
            return elements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting elements in screenshot");
            return new List<DetectedElement>();
        }
    }

    /// <summary>
    /// Finds a specific element by natural language description.
    /// </summary>
    public async Task<DetectedElement?> FindElementByDescriptionAsync(
        byte[] screenshot,
        string description,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Finding element by description: {Description}", description);

        var base64Image = Convert.ToBase64String(screenshot);
        var prompt = BuildFindElementPrompt(description);

        try
        {
            var response = await SendVisionRequestAsync(base64Image, prompt, cancellationToken);
            var element = ParseSingleElementResponse(response);

            if (element != null)
            {
                _logger.LogInformation("Found element: {Type} at ({X}, {Y})",
                    element.ElementType, element.BoundingBox.X, element.BoundingBox.Y);
            }
            else
            {
                _logger.LogWarning("Element not found for description: {Description}", description);
            }

            return element;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error finding element by description");
            return null;
        }
    }

    /// <summary>
    /// Extracts all text from a screenshot using OCR.
    /// </summary>
    public async Task<string?> ExtractTextAsync(
        byte[] screenshot,
        ElementBoundingBox? region = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Extracting text from screenshot");

        var base64Image = Convert.ToBase64String(screenshot);
        var prompt = BuildOCRPrompt(region);

        try
        {
            var response = await SendVisionRequestAsync(base64Image, prompt, cancellationToken);
            var text = ParseTextExtractionResponse(response);

            _logger.LogInformation("Extracted {Length} characters of text", text?.Length ?? 0);
            return text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from screenshot");
            return null;
        }
    }

    /// <summary>
    /// Locates an element's bounding box by description.
    /// </summary>
    public async Task<ElementBoundingBox?> LocateElementAsync(
        byte[] screenshot,
        string elementDescription,
        CancellationToken cancellationToken = default)
    {
        var element = await FindElementByDescriptionAsync(screenshot, elementDescription, cancellationToken);
        return element?.BoundingBox;
    }

    /// <summary>
    /// Describes the content of a screenshot in natural language.
    /// </summary>
    public async Task<string> DescribeScreenshotAsync(
        byte[] screenshot,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Describing screenshot content");

        var base64Image = Convert.ToBase64String(screenshot);
        var prompt = "Describe this screenshot in detail. Include the main UI elements, layout, colors, and any text visible.";

        try
        {
            var response = await SendVisionRequestAsync(base64Image, prompt, cancellationToken);
            _logger.LogInformation("Screenshot description generated ({Length} chars)", response.Length);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error describing screenshot");
            return "Error describing screenshot";
        }
    }

    /// <summary>
    /// Sends a vision request to GPT-4 Vision via Azure OpenAI.
    /// </summary>
    private async Task<string> SendVisionRequestAsync(
        string base64Image,
        string prompt,
        CancellationToken cancellationToken)
    {
        var messages = new List<Message>
        {
            new()
            {
                Role = MessageRole.System,
                Content = "You are an expert at analyzing web UI screenshots and identifying interactive elements. " +
                         "You provide accurate bounding box coordinates and element classifications. " +
                         "Return responses in valid JSON format only."
            },
            new()
            {
                Role = MessageRole.User,
                Content = prompt,
                ImageUrl = $"data:image/png;base64,{base64Image}"
            }
        };

        var request = new LLMRequest
        {
            Model = _llmProvider.GetModelName(),
            Messages = messages,
            Temperature = 0.1, // Low temperature for consistent, accurate analysis
            MaxTokens = 4000,
            ResponseFormat = new ResponseFormat { Type = "json_object" }
        };

        var response = await _llmProvider.CompleteAsync(request, cancellationToken);

        if (response.Choices.Count == 0 || string.IsNullOrWhiteSpace(response.Content))
        {
            throw new InvalidOperationException("GPT-4 Vision returned empty response");
        }

        return response.Content;
    }

    /// <summary>
    /// Builds the prompt for detecting all elements in a screenshot.
    /// </summary>
    private string BuildElementDetectionPrompt(ElementFilter? filter)
    {
        var filterText = filter?.ElementType != null
            ? $"Focus on {filter.ElementType} elements. "
            : "Detect all interactive elements. ";

        return $@"{filterText}For each element, provide:
1. Element type (button, input, link, checkbox, select, textarea, etc.)
2. Bounding box coordinates (x, y, width, height) in pixels
3. Visible text or label
4. Confidence score (0.0 to 1.0)
5. Whether it's currently visible and interactable

Return JSON in this format:
{{
  ""elements"": [
    {{
      ""type"": ""button"",
      ""text"": ""Submit"",
      ""boundingBox"": {{ ""x"": 100, ""y"": 200, ""width"": 80, ""height"": 32 }},
      ""confidence"": 0.95,
      ""isVisible"": true,
      ""isInteractable"": true
    }}
  ]
}}";
    }

    /// <summary>
    /// Builds the prompt for finding a specific element by description.
    /// </summary>
    private string BuildFindElementPrompt(string description)
    {
        return $@"Find the UI element that matches this description: ""{description}""

Provide the element details in JSON format:
{{
  ""element"": {{
    ""type"": ""button"",
    ""text"": ""Submit"",
    ""boundingBox"": {{ ""x"": 100, ""y"": 200, ""width"": 80, ""height"": 32 }},
    ""confidence"": 0.95,
    ""isVisible"": true,
    ""isInteractable"": true,
    ""description"": ""Blue submit button in the bottom right""
  }}
}}

If the element is not found, return:
{{ ""element"": null }}";
    }

    /// <summary>
    /// Builds the prompt for OCR text extraction.
    /// </summary>
    private string BuildOCRPrompt(ElementBoundingBox? region)
    {
        var regionText = region != null
            ? $"Focus on the region at x={region.X}, y={region.Y}, width={region.Width}, height={region.Height}. "
            : "Extract all visible text from the entire image. ";

        return $@"{regionText}Return all text found in the image.

Return JSON in this format:
{{
  ""text"": ""All the extracted text here...""
}}";
    }

    /// <summary>
    /// Parses the element detection response from GPT-4 Vision.
    /// </summary>
    private List<DetectedElement> ParseElementDetectionResponse(string jsonResponse)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<ElementDetectionResponse>(jsonResponse, options);
            
            if (response?.Elements == null || response.Elements.Count == 0)
            {
                return new List<DetectedElement>();
            }

            return response.Elements.Select(e => new DetectedElement
            {
                ElementType = e.Type ?? "unknown",
                Text = e.Text,
                BoundingBox = new ElementBoundingBox
                {
                    X = e.BoundingBox?.X ?? 0,
                    Y = e.BoundingBox?.Y ?? 0,
                    Width = e.BoundingBox?.Width ?? 0,
                    Height = e.BoundingBox?.Height ?? 0
                },
                Confidence = e.Confidence,
                IsVisible = e.IsVisible,
                IsInteractable = e.IsInteractable ?? true
            }).ToList();
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse element detection response: {Response}", jsonResponse);
            return new List<DetectedElement>();
        }
    }

    /// <summary>
    /// Parses a single element response from GPT-4 Vision.
    /// </summary>
    private DetectedElement? ParseSingleElementResponse(string jsonResponse)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<SingleElementResponse>(jsonResponse, options);
            
            if (response?.Element == null)
            {
                return null;
            }

            var e = response.Element;
            return new DetectedElement
            {
                ElementType = e.Type ?? "unknown",
                Text = e.Text,
                BoundingBox = new ElementBoundingBox
                {
                    X = e.BoundingBox?.X ?? 0,
                    Y = e.BoundingBox?.Y ?? 0,
                    Width = e.BoundingBox?.Width ?? 0,
                    Height = e.BoundingBox?.Height ?? 0
                },
                Confidence = e.Confidence,
                IsVisible = e.IsVisible,
                IsInteractable = e.IsInteractable ?? true,
                Description = e.Description
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse single element response: {Response}", jsonResponse);
            return null;
        }
    }

    /// <summary>
    /// Parses text extraction response from GPT-4 Vision.
    /// </summary>
    private string? ParseTextExtractionResponse(string jsonResponse)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var response = JsonSerializer.Deserialize<TextExtractionResponse>(jsonResponse, options);
            return response?.Text;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse text extraction response: {Response}", jsonResponse);
            return null;
        }
    }

    #region Response Models

    private class ElementDetectionResponse
    {
        public List<ElementDto> Elements { get; set; } = new();
    }

    private class SingleElementResponse
    {
        public ElementDto? Element { get; set; }
    }

    private class TextExtractionResponse
    {
        public string? Text { get; set; }
    }

    private class ElementDto
    {
        public string? Type { get; set; }
        public string? Text { get; set; }
        public BoundingBoxDto? BoundingBox { get; set; }
        public double Confidence { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool? IsInteractable { get; set; }
        public string? Description { get; set; }
    }

    private class BoundingBoxDto
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    #endregion
}
