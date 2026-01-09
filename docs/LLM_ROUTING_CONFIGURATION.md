# Advanced LLM Provider Integration - Configuration Guide

**Document Version:** 1.0.0  
**Status:** Draft  
**Last Updated:** December 2024

---

## ?? Overview

This guide explains how to configure the Advanced LLM Provider Integration feature, including routing strategies, circuit breaker settings, streaming options, and Azure Key Vault integration.

---

## ?? Quick Start Configurations

### Scenario 1: Basic Routing (Development)

**Use Case:** Local development with Ollama fallback

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Routing",
      "EnableMultiModelRouting": true,
      "RoutingStrategy": "TaskBased",
      "EnableProviderFallback": true,
      
      "AzureOpenAI": {
        "Endpoint": "https://your-resource.openai.azure.com",
        "ApiKey": "your-api-key-here",
        "DeploymentName": "gpt-4"
      },
      
      "Ollama": {
        "BaseUrl": "http://localhost:11434",
        "Model": "qwen2.5-7b"
      }
    }
  }
}
```

### Scenario 2: Production with Key Vault

**Use Case:** Production deployment with secure secrets

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Routing",
      "EnableMultiModelRouting": true,
      "RoutingStrategy": "CostOptimized",
      "EnableProviderFallback": true,
      "CircuitBreakerFailureThreshold": 5,
      "CircuitBreakerOpenDurationSeconds": 30,
      
      "AzureOpenAI": {
        "Endpoint": "@Microsoft.KeyVault(SecretUri=https://evoai-kv.vault.azure.net/secrets/OpenAIEndpoint)",
        "ApiKey": "@Microsoft.KeyVault(SecretUri=https://evoai-kv.vault.azure.net/secrets/OpenAIApiKey)",
        "DeploymentName": "gpt-4"
      }
    },
    
    "KeyVault": {
      "VaultUri": "https://evoai-kv.vault.azure.net",
      "Enabled": true,
      "UseManagedIdentity": true
    }
  }
}
```

### Scenario 3: Cost-Optimized Multi-Model

**Use Case:** Minimize costs while maintaining quality

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Routing",
      "RoutingStrategy": "CostOptimized",
      
      "Routing": {
        "Planning": {
          "PrimaryProvider": "AzureOpenAI",
          "PrimaryModel": "gpt-4",
          "CostPer1KTokens": 0.03
        },
        "CodeGeneration": {
          "PrimaryProvider": "Ollama",
          "PrimaryModel": "qwen2.5-7b",
          "CostPer1KTokens": 0.0
        },
        "Analysis": {
          "PrimaryProvider": "AzureOpenAI",
          "PrimaryModel": "gpt-3.5-turbo",
          "CostPer1KTokens": 0.0015
        },
        "Validation": {
          "PrimaryProvider": "Ollama",
          "PrimaryModel": "llama3.2",
          "CostPer1KTokens": 0.0
        }
      }
    }
  }
}
```

---

## ?? Configuration Options Reference

### Core Routing Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `LLMProvider` | string | "AzureOpenAI" | Provider type: "AzureOpenAI", "Ollama", "Routing" |
| `EnableMultiModelRouting` | bool | true | Enable intelligent routing to multiple models |
| `RoutingStrategy` | string | "TaskBased" | Strategy: "TaskBased", "CostOptimized", "PerformanceOptimized" |
| `EnableProviderFallback` | bool | true | Enable automatic fallback when primary fails |

### Circuit Breaker Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `CircuitBreakerFailureThreshold` | int | 5 | Consecutive failures before opening circuit |
| `CircuitBreakerOpenDurationSeconds` | int | 30 | Seconds to wait before testing recovery |

### Route Configuration

Each route supports:

| Option | Type | Required | Description |
|--------|------|----------|-------------|
| `PrimaryProvider` | string | Yes | Primary provider name |
| `PrimaryModel` | string | Yes | Primary model name |
| `FallbackProvider` | string | No | Fallback provider name |
| `FallbackModel` | string | No | Fallback model name |
| `MaxLatencyMs` | int | No | Max latency before fallback (ms) |
| `CostPer1KTokens` | double | No | Cost per 1K tokens (for optimization) |

### Key Vault Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `VaultUri` | string | null | Azure Key Vault URI |
| `Enabled` | bool | true | Enable Key Vault integration |
| `CacheDurationMinutes` | int | 5 | Secret cache duration |
| `UseManagedIdentity` | bool | true | Use managed identity for auth |
| `TenantId` | string | null | Service principal tenant ID |
| `ClientId` | string | null | Service principal client ID |

---

## ??? Routing Strategies

### TaskBased Strategy

Routes requests based on detected or specified task type.

**Configuration:**
```json
{
  "RoutingStrategy": "TaskBased",
  "Routing": {
    "Planning": { "PrimaryProvider": "AzureOpenAI", "PrimaryModel": "gpt-5" },
    "CodeGeneration": { "PrimaryProvider": "Ollama", "PrimaryModel": "qwen2.5-7b" },
    "Analysis": { "PrimaryProvider": "AzureOpenAI", "PrimaryModel": "gpt-4" }
  }
}
```

**Task Type Detection:**
1. Explicit `TaskType` in request (highest priority)
2. Keywords in system prompt ("plan", "code", "analyze")
3. User prompt length and complexity
4. Default to `General`

### CostOptimized Strategy

Minimizes costs by routing to cheapest adequate model.

**Configuration:**
```json
{
  "RoutingStrategy": "CostOptimized",
  "Routing": {
    "Planning": {
      "PrimaryProvider": "AzureOpenAI",
      "PrimaryModel": "gpt-4",
      "CostPer1KTokens": 0.03
    },
    "CodeGeneration": {
      "PrimaryProvider": "Ollama",
      "PrimaryModel": "qwen2.5-7b",
      "CostPer1KTokens": 0.0
    }
  }
}
```

**Cost Optimization Logic:**
1. Sort routes by cost (cheapest first)
2. Select cheapest route that meets quality threshold
3. Track actual costs and adjust

### PerformanceOptimized Strategy

Prioritizes speed and latency.

**Configuration:**
```json
{
  "RoutingStrategy": "PerformanceOptimized",
  "Routing": {
    "Planning": {
      "PrimaryProvider": "Ollama",
      "PrimaryModel": "qwen2.5-7b",
      "MaxLatencyMs": 1000
    }
  }
}
```

**Performance Optimization:**
1. Route to local models first (Ollama)
2. Use cached responses when possible
3. Implement timeout-based fallback

---

## ?? Azure Key Vault Setup

### Step 1: Create Key Vault

```bash
# Create resource group
az group create \
  --name evoaitest-rg \
  --location eastus

# Create Key Vault
az keyvault create \
  --name evoai-keyvault \
  --resource-group evoaitest-rg \
  --location eastus \
  --enable-rbac-authorization
```

### Step 2: Add Secrets

```bash
# Add Azure OpenAI secrets
az keyvault secret set \
  --vault-name evoai-keyvault \
  --name OpenAIEndpoint \
  --value "https://your-resource.openai.azure.com"

az keyvault secret set \
  --vault-name evoai-keyvault \
  --name OpenAIApiKey \
  --value "your-api-key-here"
```

### Step 3: Grant Access

**Option A: Managed Identity (Recommended)**
```bash
# Assign Key Vault Secrets User role to App Service
az role assignment create \
  --assignee-object-id <app-service-identity-id> \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/<sub-id>/resourceGroups/evoaitest-rg/providers/Microsoft.KeyVault/vaults/evoai-keyvault
```

**Option B: Service Principal**
```bash
# Create service principal
az ad sp create-for-rbac --name evoaitest-sp

# Assign role
az role assignment create \
  --assignee <service-principal-id> \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/<sub-id>/resourceGroups/evoaitest-rg/providers/Microsoft.KeyVault/vaults/evoai-keyvault
```

### Step 4: Configure Application

**Production (Managed Identity):**
```json
{
  "KeyVault": {
    "VaultUri": "https://evoai-keyvault.vault.azure.net",
    "Enabled": true,
    "UseManagedIdentity": true
  }
}
```

**Development (Service Principal):**
```json
{
  "KeyVault": {
    "VaultUri": "https://evoai-keyvault.vault.azure.net",
    "Enabled": true,
    "UseManagedIdentity": false,
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id"
  },
  "Azure": {
    "ClientSecret": "your-client-secret"  // Use User Secrets for this
  }
}
```

**Local Development (User Secrets):**
```bash
# Store secrets locally
dotnet user-secrets set "EvoAITest:Core:AzureOpenAI:ApiKey" "your-api-key"
dotnet user-secrets set "EvoAITest:Core:AzureOpenAI:Endpoint" "https://..."

# Disable Key Vault
dotnet user-secrets set "EvoAITest:KeyVault:Enabled" "false"
```

---

## ?? Circuit Breaker Configuration

### Default Settings

```json
{
  "CircuitBreakerFailureThreshold": 5,
  "CircuitBreakerOpenDurationSeconds": 30
}
```

### Conservative (Production)

```json
{
  "CircuitBreakerFailureThreshold": 3,
  "CircuitBreakerOpenDurationSeconds": 60
}
```

### Aggressive (High Traffic)

```json
{
  "CircuitBreakerFailureThreshold": 10,
  "CircuitBreakerOpenDurationSeconds": 15
}
```

### Circuit Breaker Behavior

| State | Behavior | Next State |
|-------|----------|------------|
| **Closed** | All requests to primary | Open after N failures |
| **Open** | All requests to fallback | Half-Open after timeout |
| **Half-Open** | Test primary with 1 request | Closed on success, Open on failure |

---

## ?? Monitoring & Telemetry

### Enable Structured Logging

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "EvoAITest.LLM.Routing": "Debug",
      "EvoAITest.LLM.CircuitBreaker": "Warning"
    }
  }
}
```

### Telemetry Events

The system emits these telemetry events:

| Event | Level | Description |
|-------|-------|-------------|
| `llm.routing.decision` | Information | Routing decision made |
| `llm.circuit_breaker.opened` | Warning | Circuit breaker opened |
| `llm.circuit_breaker.closed` | Information | Circuit breaker closed |
| `llm.fallback.used` | Warning | Fallback provider used |
| `llm.streaming.started` | Information | Streaming session started |
| `llm.streaming.completed` | Information | Streaming session completed |

### Application Insights Integration

```csharp
// In Program.cs
builder.Services.AddApplicationInsightsTelemetry(options =>
{
    options.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
});

// Custom telemetry
builder.Services.AddSingleton<ITelemetryInitializer, LLMRoutingTelemetryInitializer>();
```

---

## ?? Environment-Specific Configuration

### Development

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Routing",
      "Routing Strategy": "TaskBased",
      "CircuitBreakerFailureThreshold": 10,  // More lenient
      
      "AzureOpenAI": {
        "Endpoint": "https://dev-resource.openai.azure.com",
        "ApiKey": "dev-api-key"
      },
      
      "Ollama": {
        "BaseUrl": "http://localhost:11434"
      }
    },
    
    "KeyVault": {
      "Enabled": false  // Use user secrets
    }
  }
}
```

### Staging

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Routing",
      "RoutingStrategy": "TaskBased",
      "CircuitBreakerFailureThreshold": 5,
      
      "AzureOpenAI": {
        "Endpoint": "@Microsoft.KeyVault(SecretUri=https://staging-kv.vault.azure.net/secrets/OpenAIEndpoint)",
        "ApiKey": "@Microsoft.KeyVault(SecretUri=https://staging-kv.vault.azure.net/secrets/OpenAIApiKey)"
      }
    },
    
    "KeyVault": {
      "VaultUri": "https://staging-kv.vault.azure.net",
      "Enabled": true,
      "UseManagedIdentity": true
    }
  }
}
```

### Production

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "Routing",
      "RoutingStrategy": "CostOptimized",
      "CircuitBreakerFailureThreshold": 3,
      "CircuitBreakerOpenDurationSeconds": 60,
      
      "AzureOpenAI": {
        "Endpoint": "@Microsoft.KeyVault(SecretUri=https://prod-kv.vault.azure.net/secrets/OpenAIEndpoint)",
        "ApiKey": "@Microsoft.KeyVault(SecretUri=https://prod-kv.vault.azure.net/secrets/OpenAIApiKey)"
      }
    },
    
    "KeyVault": {
      "VaultUri": "https://prod-kv.vault.azure.net",
      "Enabled": true,
      "UseManagedIdentity": true,
      "CacheDurationMinutes": 10  // Longer cache in production
    }
  },
  
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "EvoAITest.LLM": "Information"
    }
  }
}
```

---

## ?? Troubleshooting

### Issue: Circuit Breaker Opens Too Quickly

**Symptom:** Circuit breaker opens after just a few requests

**Solution:**
```json
{
  "CircuitBreakerFailureThreshold": 10,  // Increase threshold
  "CircuitBreakerOpenDurationSeconds": 15  // Reduce wait time
}
```

### Issue: Key Vault Access Denied

**Symptom:** "403 Forbidden" when accessing secrets

**Solution:**
1. Verify managed identity is enabled on App Service
2. Check RBAC role assignment:
```bash
az role assignment list \
  --assignee <app-service-identity-id> \
  --scope /subscriptions/<sub-id>/resourceGroups/evoaitest-rg/providers/Microsoft.KeyVault/vaults/evoai-keyvault
```
3. Ensure "Key Vault Secrets User" role is assigned

### Issue: Routing Not Working

**Symptom:** All requests go to default provider

**Solution:**
1. Verify `EnableMultiModelRouting: true`
2. Check routing configuration exists
3. Enable debug logging:
```json
{
  "Logging": {
    "LogLevel": {
      "EvoAITest.LLM.Routing": "Debug"
    }
  }
}
```

### Issue: Streaming Not Working

**Symptom:** No tokens received or errors during streaming

**Solution:**
1. Verify provider supports streaming: `provider.SupportsStreaming`
2. Check for buffering in middleware/proxies
3. Use chunked transfer encoding
4. Test with simple SSE client first

---

## ? Configuration Validation

### Startup Validation

```csharp
// In Program.cs
builder.Services.AddOptions<LLMRoutingOptions>()
    .Bind(builder.Configuration.GetSection("EvoAITest:Core:Routing"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Manual Validation

```csharp
public class LLMRoutingOptionsValidator : IValidateOptions<LLMRoutingOptions>
{
    public ValidateOptionsResult Validate(string name, LLMRoutingOptions options)
    {
        var errors = new List<string>();
        
        if (options.CircuitBreakerFailureThreshold < 1)
            errors.Add("Failure threshold must be at least 1");
        
        if (options.CircuitBreakerOpenDurationSeconds < 5)
            errors.Add("Open duration must be at least 5 seconds");
        
        if (options.Routes.Count == 0 && options.EnableMultiModelRouting)
            errors.Add("No routes configured for multi-model routing");
        
        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
```

---

**Document Status:** ? Complete  
**Next:** Implementation Checklist  
**Owner:** DevOps Team
