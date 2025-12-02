using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using EvoAITest.Core.Models;
using EvoAITest.LLM.Abstractions;
using EvoAITest.LLM.Models;
using Microsoft.Extensions.Logging;

namespace EvoAITest.LLM.Prompts;

/// <summary>
/// Default implementation of IPromptBuilder with templates, versioning, and injection protection.
/// </summary>
public sealed partial class DefaultPromptBuilder : IPromptBuilder
{
    private readonly ILogger<DefaultPromptBuilder> _logger;
    private readonly Dictionary<string, List<SystemInstruction>> _systemInstructions = new();
    private readonly Dictionary<string, List<PromptTemplate>> _templates = new();

    public InjectionProtectionOptions InjectionOptions { get; set; } = new();

    public DefaultPromptBuilder(ILogger<DefaultPromptBuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        // Register default system instructions
        RegisterDefaultSystemInstructions();
        
        // Register default templates
        RegisterDefaultTemplates();
    }

    #region Legacy Message Building Methods

    public Message BuildSystemPrompt(string content)
    {
        return new Message
        {
            Role = MessageRole.System,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public Message BuildUserMessage(string content)
    {
        return new Message
        {
            Role = MessageRole.User,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public Message BuildAssistantMessage(string content)
    {
        return new Message
        {
            Role = MessageRole.Assistant,
            Content = content,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    public Conversation BuildConversation(params Message[] messages)
    {
        var conversation = new Conversation();
        foreach (var message in messages)
        {
            conversation.AddMessage(message);
        }
        return conversation;
    }

    public Conversation AddContext(Conversation conversation, Dictionary<string, object> context)
    {
        ArgumentNullException.ThrowIfNull(conversation);
        ArgumentNullException.ThrowIfNull(context);

        foreach (var (key, value) in context)
        {
            conversation.Metadata[key] = value;
        }

        return conversation;
    }

    public string FormatTemplate(string template, Dictionary<string, object> variables)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        var result = template;
        foreach (var (key, value) in variables)
        {
            var placeholder = $"{{{key}}}";
            result = result.Replace(placeholder, value?.ToString() ?? string.Empty);
        }

        return result;
    }

    #endregion

    #region Prompt Creation

    public Prompt CreatePrompt(string systemInstructionKey = "default", string? version = null)
    {
        var instruction = GetSystemInstruction(systemInstructionKey, version);
        
        var prompt = new Prompt
        {
            Id = Guid.NewGuid().ToString(),
            Version = version ?? "1.0",
            InjectionProtectionEnabled = InjectionOptions.Enabled
        };

        if (instruction != null)
        {
            prompt.SystemInstruction = new PromptComponent
            {
                Template = instruction.Content,
                RequiresSanitization = false,
                Priority = 100
            };
        }

        _logger.LogDebug("Created prompt {PromptId} with system instruction '{Key}'", 
            prompt.Id, systemInstructionKey);

        return prompt;
    }

    public Prompt WithSystemInstruction(Prompt prompt, string instruction)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction);

        prompt.SystemInstruction = new PromptComponent
        {
            Template = instruction,
            RequiresSanitization = false,
            Priority = 100
        };

        return prompt;
    }

    public Prompt WithContext(Prompt prompt, string context)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(context);

        prompt.Context = new PromptComponent
        {
            Template = context,
            RequiresSanitization = true,
            Priority = 80
        };

        return prompt;
    }

    public Prompt WithUserInstruction(Prompt prompt, string instruction)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction);

        prompt.UserInstruction = new PromptComponent
        {
            Template = instruction,
            RequiresSanitization = true,
            Priority = 90
        };

        return prompt;
    }

    public Prompt WithTools(Prompt prompt, List<BrowserTool> tools)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(tools);

        var toolsJson = JsonSerializer.Serialize(tools, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        prompt.ToolDefinitions = new PromptComponent
        {
            Template = $@"
Available Tools:
{toolsJson}

You can call these tools by responding with a JSON object containing a 'tool_calls' array.
Each tool call should specify the tool name and parameters.",
            RequiresSanitization = false,
            Priority = 70
        };

        return prompt;
    }

    public Prompt WithExamples(Prompt prompt, string examples)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(examples);

        prompt.Examples = new PromptComponent
        {
            Template = examples,
            RequiresSanitization = false,
            Priority = 60
        };

        return prompt;
    }

    public Prompt WithOutputFormat(Prompt prompt, string format)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(format);

        prompt.OutputFormat = new PromptComponent
        {
            Template = format,
            RequiresSanitization = false,
            Priority = 50
        };

        return prompt;
    }

    public Prompt WithVariable(Prompt prompt, string key, string value)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        prompt.Variables[key] = value ?? string.Empty;
        return prompt;
    }

    public Prompt WithVariables(Prompt prompt, Dictionary<string, string> variables)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(variables);

        foreach (var (key, value) in variables)
        {
            prompt.Variables[key] = value;
        }

        return prompt;
    }

    #endregion

    #region Building

    public PromptBuildResult Build(Prompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        return BuildAsync(prompt, CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task<PromptBuildResult> BuildAsync(Prompt prompt, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var result = new PromptBuildResult
        {
            OriginalPrompt = prompt
        };

        try
        {
            // Validate injection if enabled
            if (prompt.InjectionProtectionEnabled && InjectionOptions.Enabled)
            {
                var injectionIssues = ValidateInjection(prompt);
                if (injectionIssues.Count > 0)
                {
                    result.Warnings.AddRange(injectionIssues);
                    result.SanitizationLog.Add($"Detected {injectionIssues.Count} potential injection patterns");

                    if (InjectionOptions.BlockOnDetection)
                    {
                        result.Success = false;
                        result.Errors.Add("Prompt blocked due to potential injection attack");
                        _logger.LogWarning("Blocked prompt {PromptId} due to injection detection", prompt.Id);
                        return result;
                    }
                }
            }

            // Build components in priority order
            var components = GetOrderedComponents(prompt);
            var builder = new StringBuilder();

            foreach (var (name, component) in components)
            {
                if (string.IsNullOrWhiteSpace(component.Template))
                {
                    continue;
                }

                var processedText = component.Template;

                // Sanitize if required
                if (component.RequiresSanitization && prompt.InjectionProtectionEnabled)
                {
                    var original = processedText;
                    processedText = SanitizeInput(processedText);
                    
                    if (original != processedText)
                    {
                        result.SanitizationLog.Add($"Sanitized component: {name}");
                    }
                }

                // Substitute variables
                processedText = SubstituteVariables(processedText, prompt.Variables);

                // Add to final prompt
                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }

                builder.Append(processedText.Trim());
            }

            result.PromptText = builder.ToString();
            result.EstimatedTokens = EstimateTokens(result.PromptText);
            result.Success = true;

            // Validate prompt length
            if (result.PromptText.Length > InjectionOptions.MaxPromptLength)
            {
                result.Warnings.Add($"Prompt length ({result.PromptText.Length}) exceeds maximum ({InjectionOptions.MaxPromptLength})");
            }

            _logger.LogDebug("Built prompt {PromptId}: {Tokens} tokens, {ComponentCount} components",
                prompt.Id, result.EstimatedTokens, components.Count);

            await Task.CompletedTask; // For async placeholder
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Build failed: {ex.Message}");
            _logger.LogError(ex, "Failed to build prompt {PromptId}", prompt.Id);
            return result;
        }
    }

    #endregion

    #region System Instructions

    public void RegisterSystemInstruction(SystemInstruction instruction)
    {
        ArgumentNullException.ThrowIfNull(instruction);
        ArgumentException.ThrowIfNullOrWhiteSpace(instruction.Key);

        if (!_systemInstructions.ContainsKey(instruction.Key))
        {
            _systemInstructions[instruction.Key] = new List<SystemInstruction>();
        }

        // Remove existing instruction with same version
        _systemInstructions[instruction.Key].RemoveAll(i => i.Version == instruction.Version);
        _systemInstructions[instruction.Key].Add(instruction);

        _logger.LogDebug("Registered system instruction '{Key}' version {Version}",
            instruction.Key, instruction.Version);
    }

    public SystemInstruction? GetSystemInstruction(string key, string? version = null)
    {
        if (!_systemInstructions.TryGetValue(key, out var instructions) || instructions.Count == 0)
        {
            return null;
        }

        if (version != null)
        {
            return instructions.FirstOrDefault(i => i.Version == version);
        }

        // Return highest priority or default
        return instructions
            .OrderByDescending(i => i.IsDefault)
            .ThenByDescending(i => i.Priority)
            .FirstOrDefault();
    }

    public List<SystemInstruction> ListSystemInstructions()
    {
        return _systemInstructions.Values
            .SelectMany(list => list)
            .ToList();
    }

    #endregion

    #region Templates

    public void RegisterTemplate(PromptTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentException.ThrowIfNullOrWhiteSpace(template.Name);

        if (!_templates.ContainsKey(template.Name))
        {
            _templates[template.Name] = new List<PromptTemplate>();
        }

        // Remove existing template with same version
        _templates[template.Name].RemoveAll(t => t.Version == template.Version);
        _templates[template.Name].Add(template);

        _logger.LogDebug("Registered template '{Name}' version {Version}",
            template.Name, template.Version);
    }

    public PromptTemplate? GetTemplate(string name, string? version = null)
    {
        if (!_templates.TryGetValue(name, out var templates) || templates.Count == 0)
        {
            return null;
        }

        if (version != null)
        {
            return templates.FirstOrDefault(t => t.Version == version);
        }

        // Return latest version
        return templates.OrderByDescending(t => t.Version).FirstOrDefault();
    }

    public Prompt FromTemplate(string templateName, Dictionary<string, string>? variables = null, string? version = null)
    {
        var template = GetTemplate(templateName, version);
        if (template == null)
        {
            throw new InvalidOperationException($"Template '{templateName}' not found");
        }

        var prompt = CreatePrompt();
        prompt.UserInstruction = new PromptComponent
        {
            Template = template.Content,
            RequiresSanitization = true,
            Priority = 90
        };

        // Add default values
        foreach (var (key, value) in template.DefaultValues)
        {
            prompt.Variables[key] = value;
        }

        // Override with provided variables
        if (variables != null)
        {
            foreach (var (key, value) in variables)
            {
                prompt.Variables[key] = value;
            }
        }

        // Validate required variables
        var missingVars = template.RequiredVariables
            .Where(v => !prompt.Variables.ContainsKey(v))
            .ToList();

        if (missingVars.Count > 0)
        {
            throw new InvalidOperationException(
                $"Template '{templateName}' requires variables: {string.Join(", ", missingVars)}");
        }

        return prompt;
    }

    #endregion

    #region Injection Protection

    public List<string> ValidateInjection(Prompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);

        var issues = new List<string>();

        // Check all user-provided components
        var componentsToCheck = new[]
        {
            ("Context", prompt.Context),
            ("UserInstruction", prompt.UserInstruction)
        };

        foreach (var (name, component) in componentsToCheck)
        {
            if (component == null || string.IsNullOrWhiteSpace(component.Template))
            {
                continue;
            }

            foreach (var pattern in InjectionOptions.DetectionPatterns)
            {
                try
                {
                    if (Regex.IsMatch(component.Template, pattern, RegexOptions.IgnoreCase))
                    {
                        issues.Add($"Potential injection detected in {name}: matched pattern '{pattern}'");
                        
                        if (InjectionOptions.LogSuspectedInjections)
                        {
                            _logger.LogWarning(
                                "Suspected injection in {Component} for prompt {PromptId}: pattern '{Pattern}'",
                                name, prompt.Id, pattern);
                        }
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    _logger.LogWarning("Regex timeout checking pattern: {Pattern}", pattern);
                }
            }
        }

        return issues;
    }

    public string SanitizeInput(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        var sanitized = input;

        // Escape special characters
        if (InjectionOptions.EscapeSpecialCharacters)
        {
            foreach (var (pattern, replacement) in InjectionOptions.EscapeMap)
            {
                sanitized = sanitized.Replace(pattern, replacement);
            }
        }

        // Truncate if too long
        if (sanitized.Length > InjectionOptions.MaxPromptLength)
        {
            sanitized = sanitized.Substring(0, InjectionOptions.MaxPromptLength);
            _logger.LogWarning("Truncated input from {Original} to {Max} characters",
                input.Length, InjectionOptions.MaxPromptLength);
        }

        return sanitized;
    }

    #endregion

    #region Token Estimation

    public int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        // Rough estimation: ~4 characters per token for English text
        // More accurate for OpenAI models, less so for others
        return text.Length / 4;
    }

    #endregion

    #region Helper Methods

    private List<(string Name, PromptComponent Component)> GetOrderedComponents(Prompt prompt)
    {
        var components = new List<(string Name, PromptComponent Component)>();

        if (prompt.SystemInstruction != null)
            components.Add(("SystemInstruction", prompt.SystemInstruction));
        
        if (prompt.ToolDefinitions != null)
            components.Add(("ToolDefinitions", prompt.ToolDefinitions));
        
        if (prompt.Context != null)
            components.Add(("Context", prompt.Context));
        
        if (prompt.Examples != null)
            components.Add(("Examples", prompt.Examples));
        
        if (prompt.OutputFormat != null)
            components.Add(("OutputFormat", prompt.OutputFormat));
        
        if (prompt.UserInstruction != null)
            components.Add(("UserInstruction", prompt.UserInstruction));

        return components.OrderByDescending(c => c.Component.Priority).ToList();
    }

    private string SubstituteVariables(string template, Dictionary<string, string> variables)
    {
        var result = template;

        foreach (var (key, value) in variables)
        {
            var placeholder = $"{{{key}}}";
            result = result.Replace(placeholder, value);
        }

        // Warn if unreplaced placeholders remain
        var unreplaced = Regex.Matches(result, @"\{[^\{\}]+\}")
            .Cast<Match>()
            .Select(m => m.Value)
            .Distinct()
            .ToList();
        if (unreplaced.Count > 0)
        {
            _logger?.LogWarning("Unreplaced placeholders found in prompt: {Placeholders}", string.Join(", ", unreplaced));
        }
        return result;
    }

    private void RegisterDefaultSystemInstructions()
    {
        // Default system instruction
        RegisterSystemInstruction(new SystemInstruction
        {
            Key = "default",
            Version = "1.0",
            Content = "You are a helpful AI assistant specialized in browser automation.",
            Scenario = "default",
            IsDefault = true,
            Priority = 0
        });

        // Browser automation specialist
        RegisterSystemInstruction(new SystemInstruction
        {
            Key = "browser-automation",
            Version = "1.0",
            Content = @"You are an expert browser automation specialist. Your role is to:
1. Analyze web pages and identify the best way to interact with them
2. Generate precise, reliable automation steps using the provided tools
3. Handle errors gracefully and suggest recovery strategies
4. Provide clear reasoning for each step you recommend

Always prioritize robustness and maintainability in your automation plans.",
            Scenario = "automation",
            IsDefault = false,
            Priority = 10,
            ModelCompatibility = new List<string> { "gpt-4", "gpt-5", "claude-3", "qwen2.5" }
        });

        // Planner agent instruction
        RegisterSystemInstruction(new SystemInstruction
        {
            Key = "planner",
            Version = "1.0",
            Content = @"You are a planning agent for browser automation. Your responsibilities:
1. Break down natural language requests into step-by-step automation plans
2. Select appropriate tools from the available toolkit
3. Provide confidence scores and estimated durations
4. Consider edge cases and potential failure points
5. Generate clear, executable plans with proper sequencing

Respond with structured JSON containing the execution plan.",
            Scenario = "planning",
            IsDefault = false,
            Priority = 10
        });

        // Healer agent instruction
        RegisterSystemInstruction(new SystemInstruction
        {
            Key = "healer",
            Version = "1.0",
            Content = @"You are an error recovery specialist for browser automation. Your role:
1. Analyze execution failures and determine root causes
2. Examine page state and screenshots to understand what went wrong
3. Suggest alternative approaches (different selectors, timing, strategies)
4. Provide modified execution plans that address the failures
5. Learn from repeated failures and escalate if necessary

Always explain your reasoning and provide actionable recovery steps.",
            Scenario = "recovery",
            IsDefault = false,
            Priority = 10
        });
    }

    private void RegisterDefaultTemplates()
    {
        // Login automation template
        RegisterTemplate(new PromptTemplate
        {
            Name = "login-automation",
            Version = "1.0",
            Content = @"Navigate to {url} and perform login:
1. Wait for the login page to load
2. Find and fill the username field with: {username}
3. Find and fill the password field with: {password}
4. Click the submit button
5. Wait for navigation to complete
6. Verify successful login

Take a screenshot after completion.",
            RequiredVariables = new List<string> { "url", "username", "password" },
            Description = "Template for automating login workflows",
            Tags = new List<string> { "authentication", "login", "form" }
        });

        // Data extraction template
        RegisterTemplate(new PromptTemplate
        {
            Name = "data-extraction",
            Version = "1.0",
            Content = @"Navigate to {url} and extract the following data:
{data_fields}

Steps:
1. Wait for the page to load completely
2. Locate each data field using appropriate selectors
3. Extract the text content
4. Format the results as JSON

Return the extracted data in a structured format.",
            RequiredVariables = new List<string> { "url", "data_fields" },
            Description = "Template for extracting structured data from web pages",
            Tags = new List<string> { "extraction", "scraping", "data" }
        });

        // Form filling template
        RegisterTemplate(new PromptTemplate
        {
            Name = "form-filling",
            Version = "1.0",
            Content = @"Navigate to {url} and fill the form:
{form_data}

Process:
1. Wait for the form to be fully loaded
2. Fill each field with the corresponding value
3. Handle dropdowns, checkboxes, and radio buttons appropriately
4. Validate required fields are completed
5. Submit the form
6. Capture confirmation message

Take screenshots before and after submission.",
            RequiredVariables = new List<string> { "url", "form_data" },
            DefaultValues = new Dictionary<string, string>
            {
                ["wait_after_fill"] = "1000"
            },
            Description = "Template for automated form filling",
            Tags = new List<string> { "form", "input", "submission" }
        });
    }

    #endregion
}
