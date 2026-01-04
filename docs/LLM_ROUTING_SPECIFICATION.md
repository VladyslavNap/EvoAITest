# Advanced LLM Provider Integration - Feature Specification

**Feature ID:** LLM-ROUTING-v1.0  
**Version:** 1.0.0  
**Status:** Planning  
**Target Completion:** Week of [Date]

---

## ?? Executive Summary

This feature enhances EvoAITest's LLM capabilities by introducing intelligent multi-model routing, automatic failover, response streaming, and enterprise-grade security. It enables cost optimization by routing different task types to appropriate models while maintaining reliability through circuit breaker patterns and automatic fallback mechanisms.

### Business Value

- **Cost Reduction:** 40-60% reduction in LLM costs by routing to appropriate models
- **Reliability:** 99.9% uptime through automatic failover
- **Performance:** Real-time streaming responses for better UX
- **Security:** Enterprise-grade key management with Azure Key Vault
- **Scalability:** Support for unlimited LLM providers and models

---

## ?? Feature Components

### Component 1: Multi-Model Routing

**Description:** Intelligent routing of LLM requests based on task type, cost, and performance requirements.

**Capabilities:**
- Route planning tasks to high-quality models (GPT-5, Claude Sonnet)
- Route code generation to cost-effective models (Qwen, Mistral)
- Route simple tasks to fast local models
- Support custom routing rules per organization/user

**Routing Strategies:**
1. **TaskBased:** Route by operation type (Planning, CodeGen, Analysis, etc.)
2. **CostOptimized:** Minimize cost while maintaining quality thresholds
3. **PerformanceOptimized:** Prioritize speed and latency
4. **Custom:** User-defined routing rules

**Example:**
```
Planning Request ? GPT-5 (Azure OpenAI) ? High accuracy needed
Code Generation ? Qwen 2.5-7b (Ollama) ? Local, fast, free
Simple Analysis ? GPT-3.5 (Azure OpenAI) ? Fast, cheap
```

---

### Component 2: Automatic Failover & Circuit Breaker

**Description:** Resilient provider management with automatic failover when primary providers fail.

**Circuit Breaker States:**
- **Closed:** Normal operation, all requests go through
- **Open:** Provider failing, route to fallback immediately
- **Half-Open:** Testing recovery, limited requests to primary

**Failure Scenarios:**
1. Rate limiting (429 errors)
2. Service outage (500+ errors)
3. Timeout (no response within threshold)
4. Network issues (connection failures)

**Automatic Actions:**
- Detect failures within 5 attempts
- Open circuit for 30 seconds (configurable)
- Route to fallback provider immediately
- Test recovery periodically
- Log all failover events for analysis

**Example Flow:**
```
1. GPT-5 receives 5 consecutive failures
2. Circuit opens ? All requests to Ollama
3. After 30s, test GPT-5 with 1 request
4. If successful ? Gradually restore traffic
5. If fails ? Keep circuit open, retry later
```

---

### Component 3: Response Streaming

**Description:** Stream LLM responses in real-time for better user experience and lower perceived latency.

**Capabilities:**
- Server-Sent Events (SSE) for HTTP streaming
- SignalR WebSocket support for Blazor UI
- Token-by-token delivery
- Partial response processing
- Cancellation support

**Use Cases:**
1. **Test Generation:** Stream generated code as it's created
2. **Analysis Reports:** Display insights progressively
3. **Long Plans:** Show planning steps as they're formulated
4. **Chat Interfaces:** Real-time conversational responses

**Technical Implementation:**
- `IAsyncEnumerable<string>` for streaming API
- `yield return` for token delivery
- Buffering for network efficiency
- Error handling mid-stream

---

### Component 4: Enterprise Security

**Description:** Production-grade key management using Azure Key Vault and managed identity.

**Security Features:**
- **Azure Key Vault Integration:** All API keys stored securely
- **Managed Identity:** No credentials in code/config
- **Key Rotation:** Automatic support for rotated keys
- **Audit Logging:** Track all key access
- **Environment Separation:** Dev/Staging/Prod isolation

**Configuration Pattern:**
```json
{
  "AzureOpenAI": {
    "Endpoint": "@Microsoft.KeyVault(SecretUri=https://vault.azure.net/secrets/Endpoint)",
    "ApiKey": "@Microsoft.KeyVault(SecretUri=https://vault.azure.net/secrets/ApiKey)"
  }
}
```

**Development Fallback:**
- Local development uses .NET User Secrets
- CI/CD uses service principals
- Production uses managed identity

---

## ?? User Experience

### For End Users

**Before:**
- Single LLM provider (Azure OpenAI)
- No fallback if service is down
- Waiting for complete responses
- High costs for all operations

**After:**
- Automatic model selection (transparent)
- Uninterrupted service (failover is invisible)
- Real-time streaming responses
- Optimized costs based on task complexity

### For Developers

**Before:**
```csharp
var response = await llmProvider.CompleteAsync(request);
// Wait for full response
// Handle failures manually
```

**After:**
```csharp
// Option 1: Complete response
var response = await llmProvider.CompleteAsync(request);

// Option 2: Streaming
await foreach (var token in llmProvider.CompleteStreamAsync(request))
{
    Console.Write(token);
}

// Routing & failover automatic
// Circuit breaker handles failures
// No code changes needed
```

### For Administrators

**New Capabilities:**
- Configure routing strategies via appsettings
- Monitor failover events in telemetry
- Adjust circuit breaker thresholds
- Manage API keys in Key Vault
- View cost breakdown by model

---

## ?? Technical Requirements

### Functional Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-1 | Route requests by task type | P0 | 95%+ routing accuracy |
| FR-2 | Automatic failover on errors | P0 | Failover < 100ms |
| FR-3 | Circuit breaker pattern | P0 | Open circuit after 5 failures |
| FR-4 | Response streaming support | P1 | First token < 200ms |
| FR-5 | Azure Key Vault integration | P0 | Zero secrets in config |
| FR-6 | Configuration validation | P1 | Invalid config rejected at startup |
| FR-7 | Telemetry for routing decisions | P1 | All decisions logged |
| FR-8 | Cost tracking per model | P2 | Token usage tracked |

### Non-Functional Requirements

| ID | Requirement | Target | Measurement |
|----|-------------|--------|-------------|
| NFR-1 | Routing latency overhead | < 10ms | P95 latency |
| NFR-2 | Failover speed | < 100ms | Time to fallback |
| NFR-3 | Circuit breaker recovery | 30s default | Time to half-open |
| NFR-4 | Streaming latency | < 200ms | First token time |
| NFR-5 | Memory overhead | < 50MB | Process memory |
| NFR-6 | Test coverage | > 90% | Line coverage |
| NFR-7 | Documentation coverage | 100% | All public APIs |

### Constraints

1. **Backward Compatibility:** Existing code must work without changes
2. **No Breaking Changes:** Configuration changes are additive
3. **.NET 10 Only:** Requires .NET 10 runtime
4. **Azure Dependency:** Key Vault requires Azure subscription
5. **Playwright Compatible:** Must work with browser automation

---

## ??? Implementation Roadmap

### Phase 1: Foundation (Days 1-3)
- ? Feature specification (this document)
- ? Architecture documentation
- ? API design
- ? Configuration model (`LLMRoutingOptions`)
- ? Base abstractions (`IRoutingStrategy`)

### Phase 2: Core Routing (Days 4-6)
- ? `RoutingLLMProvider` implementation
- ? Task type detection logic
- ? Routing strategies (TaskBased, CostOptimized)
- ? Unit tests for routing logic

### Phase 3: Circuit Breaker (Days 7-9)
- ? `CircuitBreakerLLMProvider` implementation
- ? Failure detection and tracking
- ? State management (Closed/Open/Half-Open)
- ? Integration with routing provider
- ? Unit tests for circuit breaker

### Phase 4: Streaming (Days 10-12)
- ? Streaming API in `ILLMProvider`
- ? Azure OpenAI streaming implementation
- ? Ollama streaming implementation
- ? SSE endpoint in API service
- ? SignalR hub for WebSocket streaming
- ? Blazor UI streaming support

### Phase 5: Security (Days 13-15)
- ? `ISecretProvider` abstraction
- ? `KeyVaultSecretProvider` implementation
- ? Managed identity configuration
- ? Key Vault configuration provider
- ? Documentation for Key Vault setup

### Phase 6: Testing & Polish (Days 16-18)
- ? Integration tests
- ? Load tests for streaming
- ? End-to-end tests
- ? Performance benchmarks
- ? Documentation completion

---

## ?? Success Metrics

### Phase 1 (MVP - 2 weeks)
- ? Multi-model routing working
- ? Basic failover implemented
- ? 80% unit test coverage
- ? Core documentation complete

### Phase 2 (Beta - 4 weeks)
- ? Streaming fully functional
- ? Key Vault integration complete
- ? 90% test coverage
- ? User documentation complete
- ? Performance benchmarks met

### Phase 3 (GA - 6 weeks)
- ? Production deployment successful
- ? Zero P0 bugs in 2 weeks
- ? 95% test coverage
- ? Cost reduction validated (40%+)
- ? User feedback positive (NPS > 8)

---

## ?? Dependencies

### Internal Dependencies
- `EvoAITest.LLM` - LLM provider infrastructure
- `EvoAITest.Core` - Core options and abstractions
- `EvoAITest.ApiService` - API endpoints
- `EvoAITest.Web` - Blazor UI components

### External Dependencies
- Azure.Security.KeyVault.Secrets (>= 4.6.0)
- Azure.Identity (>= 1.12.0)
- Microsoft.Extensions.Configuration.AzureKeyVault (>= 7.0.0)
- OpenAI SDK (existing)
- System.Threading.Channels (for streaming)

### Service Dependencies
- Azure Key Vault (production)
- Azure OpenAI Service (optional)
- Ollama (optional local)

---

## ?? Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Circuit breaker too aggressive | High | Medium | Configurable thresholds, gradual rollout |
| Streaming breaks existing code | High | Low | Backward compatible API, optional feature |
| Key Vault latency | Medium | Low | Cache secrets, fallback to user secrets |
| Routing accuracy issues | High | Medium | Comprehensive testing, ML classification |
| Cost tracking inaccurate | Medium | Medium | Use official token counters, validate |
| Breaking changes in providers | High | Low | Version pin, integration tests |

---

## ?? Related Documentation

- [Architecture Document](./LLM_ROUTING_ARCHITECTURE.md)
- [API Design](./LLM_ROUTING_API_DESIGN.md)
- [Configuration Guide](./LLM_ROUTING_CONFIGURATION.md)
- [Implementation Checklist](./LLM_ROUTING_CHECKLIST.md)
- [Azure Key Vault Setup](./KEY_VAULT_SETUP.md)

---

## ?? Appendix A: Task Type Classification

| Task Type | Description | Recommended Models | Characteristics |
|-----------|-------------|-------------------|-----------------|
| **Planning** | Create automation plans | GPT-5, Claude Sonnet | High accuracy, complex reasoning |
| **CodeGeneration** | Generate test code | Qwen 2.5, CodeLlama | Fast, code-optimized |
| **Analysis** | Analyze recorded actions | GPT-4, Claude | Pattern recognition |
| **IntentDetection** | Detect user intent | GPT-4, Claude | Context understanding |
| **Validation** | Validate outputs | GPT-3.5, Llama | Quick verification |
| **Summarization** | Summarize results | GPT-3.5, Qwen | Fast, extractive |
| **Translation** | Translate content | Specialized models | Language-specific |
| **Classification** | Classify content | Fast models | Low latency |

---

## ?? Appendix B: Cost Analysis

### Current State (Without Routing)
- **All tasks use GPT-4:** $0.03/1K tokens (input), $0.06/1K tokens (output)
- **Average monthly usage:** 10M tokens
- **Monthly cost:** ~$450

### Future State (With Routing)
- **Planning (20%):** GPT-4 ? $90
- **Code Gen (40%):** Qwen (free) ? $0
- **Analysis (30%):** GPT-3.5 ($0.0015/1K) ? $15
- **Other (10%):** GPT-3.5 ? $5
- **Monthly cost:** ~$110
- **Savings:** $340/month (75% reduction)

---

**Document Status:** ? Draft Complete  
**Next Review:** Architecture Documentation  
**Owner:** Development Team  
**Last Updated:** December 2024
