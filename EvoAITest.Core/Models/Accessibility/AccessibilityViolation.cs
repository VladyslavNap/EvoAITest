using System.Text.Json.Serialization;

namespace EvoAITest.Core.Models.Accessibility;

public class AccessibilityViolation
{
    public string Id { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty; // "minor", "moderate", "serious", "critical"
    public string Description { get; set; } = string.Empty;
    public string Help { get; set; } = string.Empty;
    public string HelpUrl { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    
    // HTML snippets or selectors of affected elements
    public List<AccessibilityNode> Nodes { get; set; } = new();
}

public class AccessibilityNode
{
    public string Html { get; set; } = string.Empty;
    public List<string> Target { get; set; } = new(); // CSS selectors
    public string FailureSummary { get; set; } = string.Empty;
}
