using EvoAITest.Agents.Abstractions;
using EvoAITest.Agents.Models;
using EvoAITest.Core.Models;
using System.Text;
using System.Text.Json;

namespace EvoAITest.Agents.Services;

/// <summary>
/// Service for generating visualizations of execution plans.
/// Supports multiple formats including Graphviz DOT, Mermaid, JSON, and PlantUML.
/// </summary>
/// <remarks>
/// <para>
/// Plan visualizations help in:
/// - Understanding complex execution flows
/// - Debugging dependency issues
/// - Documentation and communication
/// - Identifying optimization opportunities
/// </para>
/// </remarks>
public sealed class PlanVisualizationService
{
    /// <summary>
    /// Generates a plan visualization in the specified format.
    /// </summary>
    /// <param name="plan">The execution plan to visualize.</param>
    /// <param name="format">The desired output format.</param>
    /// <param name="chainOfThought">Optional chain-of-thought reasoning to include.</param>
    /// <returns>A plan graph object containing the visualization.</returns>
    public PlanGraph GenerateGraph(
        ExecutionPlan plan,
        GraphFormat format,
        ChainOfThought? chainOfThought = null)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return format switch
        {
            GraphFormat.GraphvizDot => GenerateGraphvizDot(plan, chainOfThought),
            GraphFormat.Mermaid => GenerateMermaid(plan, chainOfThought),
            GraphFormat.Json => GenerateJsonGraph(plan, chainOfThought),
            GraphFormat.PlantUml => GeneratePlantUml(plan, chainOfThought),
            GraphFormat.D3Json => GenerateD3Json(plan, chainOfThought),
            _ => throw new NotSupportedException($"Format {format} is not supported")
        };
    }

    /// <summary>
    /// Generates a Graphviz DOT format visualization.
    /// </summary>
    private PlanGraph GenerateGraphvizDot(ExecutionPlan plan, ChainOfThought? chainOfThought)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();
        var sb = new StringBuilder();

        sb.AppendLine("digraph ExecutionPlan {");
        sb.AppendLine("  rankdir=TB;");
        sb.AppendLine("  node [shape=box, style=filled, fontname=\"Arial\"];");
        sb.AppendLine("  edge [fontname=\"Arial\", fontsize=10];");
        sb.AppendLine();

        // Add title
        sb.AppendLine($"  labelloc=\"t\";");
        sb.AppendLine($"  label=\"Execution Plan: {plan.TaskId}\\nConfidence: {plan.Confidence:P0}\";");
        sb.AppendLine();

        // Add nodes for each step
        foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
        {
            var nodeId = $"step{step.StepNumber}";
            var actionType = step.Action?.Type.ToString() ?? "Unknown";
            var label = $"{step.StepNumber}. {actionType}";

            // Color based on action type
            var color = GetColorForActionType(step.Action?.Type);
            
            var node = new GraphNode
            {
                Id = nodeId,
                StepNumber = step.StepNumber,
                ActionType = actionType,
                Label = label,
                Style = new NodeStyle { Color = color }
            };
            nodes.Add(node);

            sb.AppendLine($"  {nodeId} [label=\"{EscapeDot(label)}\\n{EscapeDot(step.Reasoning ?? "")}\", fillcolor=\"{color}\"];");

            // Add edge to next step
            if (step.StepNumber < plan.Steps.Count)
            {
                var nextNodeId = $"step{step.StepNumber + 1}";
                edges.Add(new GraphEdge
                {
                    SourceId = nodeId,
                    TargetId = nextNodeId,
                    RelationType = "sequential",
                    Label = "then"
                });
                
                sb.AppendLine($"  {nodeId} -> {nextNodeId} [label=\"then\"];");
            }
        }

        // Add dependencies if chain of thought is available
        if (chainOfThought != null)
        {
            sb.AppendLine();
            sb.AppendLine("  // Dependencies");
            
            foreach (var dep in chainOfThought.StepDependencies)
            {
                var sourceId = $"step{dep.RequiredStepNumber}";
                var targetId = $"step{dep.DependentStepNumber}";
                
                edges.Add(new GraphEdge
                {
                    SourceId = sourceId,
                    TargetId = targetId,
                    RelationType = dep.Type.ToString(),
                    Label = "requires",
                    Style = new EdgeStyle { LineStyle = "dashed", Color = "#999999" }
                });
                
                sb.AppendLine($"  {sourceId} -> {targetId} [label=\"requires\", style=dashed, color=\"#999999\"];");
            }
        }

        sb.AppendLine("}");

        return new PlanGraph
        {
            PlanId = plan.Id,
            Nodes = nodes,
            Edges = edges,
            Format = GraphFormat.GraphvizDot,
            Content = sb.ToString()
        };
    }

    /// <summary>
    /// Generates a Mermaid diagram format visualization.
    /// </summary>
    private PlanGraph GenerateMermaid(ExecutionPlan plan, ChainOfThought? chainOfThought)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();
        var sb = new StringBuilder();

        sb.AppendLine("graph TD");
        sb.AppendLine($"    Start([\"Start: {EscapeMermaid(plan.TaskId)}\"])");
        
        // Add nodes for each step
        foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
        {
            var nodeId = $"S{step.StepNumber}";
            var actionType = step.Action?.Type.ToString() ?? "Unknown";
            var label = $"{step.StepNumber}. {actionType}";
            
            var node = new GraphNode
            {
                Id = nodeId,
                StepNumber = step.StepNumber,
                ActionType = actionType,
                Label = label
            };
            nodes.Add(node);

            sb.AppendLine($"    {nodeId}[\"{EscapeMermaid(label)}\"]");
            
            // Connect from start or previous step
            if (step.StepNumber == 1)
            {
                edges.Add(new GraphEdge { SourceId = "Start", TargetId = nodeId, RelationType = "start" });
                sb.AppendLine($"    Start --> {nodeId}");
            }
            else
            {
                var prevNodeId = $"S{step.StepNumber - 1}";
                edges.Add(new GraphEdge { SourceId = prevNodeId, TargetId = nodeId, RelationType = "sequential" });
                sb.AppendLine($"    {prevNodeId} --> {nodeId}");
            }
        }

        // Add end node
        var lastNodeId = $"S{plan.Steps.Count}";
        edges.Add(new GraphEdge { SourceId = lastNodeId, TargetId = "End", RelationType = "end" });
        sb.AppendLine($"    {lastNodeId} --> End([\"End\"])");

        // Add dependencies if available
        if (chainOfThought != null && chainOfThought.StepDependencies.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("    %% Dependencies");
            
            foreach (var dep in chainOfThought.StepDependencies)
            {
                var sourceId = $"S{dep.RequiredStepNumber}";
                var targetId = $"S{dep.DependentStepNumber}";
                
                edges.Add(new GraphEdge 
                { 
                    SourceId = sourceId, 
                    TargetId = targetId, 
                    RelationType = dep.Type.ToString(),
                    Label = "requires"
                });
                
                sb.AppendLine($"    {sourceId} -.requires.-> {targetId}");
            }
        }

        return new PlanGraph
        {
            PlanId = plan.Id,
            Nodes = nodes,
            Edges = edges,
            Format = GraphFormat.Mermaid,
            Content = sb.ToString()
        };
    }

    /// <summary>
    /// Generates a JSON graph representation.
    /// </summary>
    private PlanGraph GenerateJsonGraph(ExecutionPlan plan, ChainOfThought? chainOfThought)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        // Create nodes
        foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
        {
            var node = new GraphNode
            {
                Id = $"step-{step.StepNumber}",
                StepNumber = step.StepNumber,
                ActionType = step.Action?.Type.ToString() ?? "Unknown",
                Label = $"Step {step.StepNumber}: {step.Action?.Type}",
                Style = new NodeStyle { Color = GetColorForActionType(step.Action?.Type) }
            };
            nodes.Add(node);

            // Add sequential edge
            if (step.StepNumber < plan.Steps.Count)
            {
                edges.Add(new GraphEdge
                {
                    SourceId = node.Id,
                    TargetId = $"step-{step.StepNumber + 1}",
                    RelationType = "sequential",
                    Label = "next"
                });
            }
        }

        // Add dependency edges
        if (chainOfThought != null)
        {
            foreach (var dep in chainOfThought.StepDependencies)
            {
                edges.Add(new GraphEdge
                {
                    SourceId = $"step-{dep.RequiredStepNumber}",
                    TargetId = $"step-{dep.DependentStepNumber}",
                    RelationType = dep.Type.ToString(),
                    Label = "requires",
                    Style = new EdgeStyle { LineStyle = "dashed" }
                });
            }
        }

        var graphData = new
        {
            planId = plan.Id,
            taskId = plan.TaskId,
            confidence = plan.Confidence,
            nodes = nodes.Select(n => new
            {
                id = n.Id,
                stepNumber = n.StepNumber,
                actionType = n.ActionType,
                label = n.Label,
                style = new
                {
                    color = n.Style.Color,
                    shape = n.Style.Shape
                }
            }),
            edges = edges.Select(e => new
            {
                source = e.SourceId,
                target = e.TargetId,
                type = e.RelationType,
                label = e.Label,
                style = new
                {
                    color = e.Style.Color,
                    lineStyle = e.Style.LineStyle
                }
            })
        };

        var json = JsonSerializer.Serialize(graphData, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        return new PlanGraph
        {
            PlanId = plan.Id,
            Nodes = nodes,
            Edges = edges,
            Format = GraphFormat.Json,
            Content = json
        };
    }

    /// <summary>
    /// Generates a PlantUML format visualization.
    /// </summary>
    private PlanGraph GeneratePlantUml(ExecutionPlan plan, ChainOfThought? chainOfThought)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();
        var sb = new StringBuilder();

        sb.AppendLine("@startuml");
        sb.AppendLine($"title Execution Plan: {plan.TaskId}");
        sb.AppendLine();
        sb.AppendLine("start");

        foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
        {
            var actionType = step.Action?.Type.ToString() ?? "Unknown";
            var label = $"{actionType}\\n{step.Reasoning}";
            
            var node = new GraphNode
            {
                Id = $"step{step.StepNumber}",
                StepNumber = step.StepNumber,
                ActionType = actionType,
                Label = label
            };
            nodes.Add(node);

            sb.AppendLine($":Step {step.StepNumber}\\n{EscapePlantUml(label)};");
            
            if (step.StepNumber < plan.Steps.Count)
            {
                edges.Add(new GraphEdge
                {
                    SourceId = node.Id,
                    TargetId = $"step{step.StepNumber + 1}",
                    RelationType = "sequential"
                });
            }
        }

        sb.AppendLine();
        sb.AppendLine("stop");
        sb.AppendLine("@enduml");

        return new PlanGraph
        {
            PlanId = plan.Id,
            Nodes = nodes,
            Edges = edges,
            Format = GraphFormat.PlantUml,
            Content = sb.ToString()
        };
    }

    /// <summary>
    /// Generates a D3.js compatible JSON format.
    /// </summary>
    private PlanGraph GenerateD3Json(ExecutionPlan plan, ChainOfThought? chainOfThought)
    {
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        // Create D3-friendly structure
        var d3Nodes = new List<object>();
        var d3Links = new List<object>();

        foreach (var step in plan.Steps.OrderBy(s => s.StepNumber))
        {
            var node = new GraphNode
            {
                Id = $"node{step.StepNumber}",
                StepNumber = step.StepNumber,
                ActionType = step.Action?.Type.ToString() ?? "Unknown",
                Label = $"Step {step.StepNumber}",
                Style = new NodeStyle { Color = GetColorForActionType(step.Action?.Type) }
            };
            nodes.Add(node);

            d3Nodes.Add(new
            {
                id = node.Id,
                name = node.Label,
                group = step.Action?.Type.ToString() ?? "Unknown",
                stepNumber = step.StepNumber,
                color = node.Style.Color
            });

            // Add link to next step
            if (step.StepNumber < plan.Steps.Count)
            {
                var edge = new GraphEdge
                {
                    SourceId = node.Id,
                    TargetId = $"node{step.StepNumber + 1}",
                    RelationType = "sequential",
                    Label = "next"
                };
                edges.Add(edge);

                d3Links.Add(new
                {
                    source = node.Id,
                    target = edge.TargetId,
                    value = 1,
                    type = "sequential"
                });
            }
        }

        // Add dependency links
        if (chainOfThought != null)
        {
            foreach (var dep in chainOfThought.StepDependencies)
            {
                var edge = new GraphEdge
                {
                    SourceId = $"node{dep.RequiredStepNumber}",
                    TargetId = $"node{dep.DependentStepNumber}",
                    RelationType = dep.Type.ToString(),
                    Label = "requires"
                };
                edges.Add(edge);

                d3Links.Add(new
                {
                    source = edge.SourceId,
                    target = edge.TargetId,
                    value = 1,
                    type = "dependency"
                });
            }
        }

        var d3Graph = new
        {
            nodes = d3Nodes,
            links = d3Links
        };

        var json = JsonSerializer.Serialize(d3Graph, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });

        return new PlanGraph
        {
            PlanId = plan.Id,
            Nodes = nodes,
            Edges = edges,
            Format = GraphFormat.D3Json,
            Content = json
        };
    }

    /// <summary>
    /// Gets a color for an action type.
    /// </summary>
    private string GetColorForActionType(Core.Models.ActionType? actionType)
    {
        return actionType switch
        {
            Core.Models.ActionType.Navigate => "#4A90E2",  // Blue
            Core.Models.ActionType.Click => "#7ED321",     // Green
            Core.Models.ActionType.Type => "#F5A623",      // Orange
            Core.Models.ActionType.Fill => "#F5A623",      // Orange
            Core.Models.ActionType.Select => "#BD10E0",    // Purple
            Core.Models.ActionType.WaitForElement => "#50E3C2",  // Teal
            Core.Models.ActionType.Screenshot => "#B8E986",      // Light green
            Core.Models.ActionType.ExtractText => "#9013FE",     // Purple
            Core.Models.ActionType.Verify => "#417505",          // Dark green
            _ => "#9B9B9B"                          // Gray
        };
    }

    /// <summary>
    /// Escapes special characters for Graphviz DOT format.
    /// </summary>
    private string EscapeDot(string input)
    {
        return input?
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "") ?? string.Empty;
    }

    /// <summary>
    /// Escapes special characters for Mermaid format.
    /// </summary>
    private string EscapeMermaid(string input)
    {
        return input?
            .Replace("\"", "'")
            .Replace("\n", " ")
            .Replace("\r", "") ?? string.Empty;
    }

    /// <summary>
    /// Escapes special characters for PlantUML format.
    /// </summary>
    private string EscapePlantUml(string input)
    {
        return input?
            .Replace("\\n", "\n")
            .Replace("\r", "") ?? string.Empty;
    }
}
