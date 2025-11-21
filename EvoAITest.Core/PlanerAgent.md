# Planner Agent for EvoAITest (.NET 10 Aspire Browser Automation Framework)

## Current Progress
- ✅ **Day 1-8:** Core abstractions implemented (`IBrowserAgent`, `ILLMProvider`, models, DI setup)
- ✅ LLM integration ready (Azure OpenAI GPT-5 + Ollama for local dev)
- ✅ `BrowserToolRegistry` with 13 tools (navigate, click, type, extract_text, etc.)

## Day 9 Goal: Implement the Planner Agent
- Takes natural language input from user (e.g., "Login to example.com with username 'test@example.com'")
- Uses Azure OpenAI GPT-5 to generate structured execution plan
- Returns list of `ExecutionStep` objects with browser tool calls
- Handles context and reasoning for each step

## Project Structure
- **File:** `src/EvoAITest.Agents/Agents/PlannerAgent.cs`
- **Namespace:** `EvoAITest.Agents.Agents`
- **Dependencies:** 
    - `ILLMProvider` (from `EvoAITest.LLM`)
    - `BrowserToolRegistry` (from `EvoAITest.Core`)

## Requirements
- Use Azure OpenAI GPT-5 in production, Ollama in development
- Support tool calling (GPT-5 function calling)
- Return typed `ExecutionStep` list
- Include error handling and logging
- Support cancellation tokens for Aspire graceful shutdown
EvoAITest Development Prompts: Days 9-20
Claude Sonnet 4.5 Prompt Guide for .NET Aspire Browser Automation Framework
Solution: EvoAITest (.NET 10 Aspire)
Azure OpenAI: GPT-5 deployment at
twazncopenai2.cognitiveservices.azure.com
Key Vault:
evoai-keyvault.vault.azure.net
(secret: LLMAPIKEY)
Local Dev: Ollama with qwen2.5-7b
Container Target: Azure Container Apps
Week 2: AI Agent Implementation (Days 9-11)
Day 9: Planner Agent (Natural Language → Execution Plan)
Context-Setting Prompt for Claude