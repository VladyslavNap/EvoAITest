EvoAITest Development Prompts: Days 9-20

Claude Sonnet 4.5 Prompt Guide for .NET Aspire Browser Automation Framework

Project Context:
Solution: EvoAITest (.NET 10 Aspire)
Azure OpenAI: GPT-5 deployment at twazncopenai2.cognitiveservices.azure.com
Key Vault: evoai-keyvault.vault.azure.net (secret: LLMAPIKEY)
Local Dev: Ollama with qwen2.5-7b
Container Target: Azure Container Apps


Week 2: AI Agent Implementation (Days 9-11)

Day 9: Planner Agent (Natural Language → Execution Plan)

Context-Setting Prompt for Claude

I'm building the Planner Agent for EvoAITest, a .NET 10 Aspire browser automation framewoCurrent Progress:
✅ Day 1-8: Core abstractions (IBrowserAgent, ILLMProvider, models, DI setup)
✅ LLM integration ready (Azure OpenAI GPT-5 + Ollama for local dev)
✅ BrowserToolRegistry with 13 tools (navigate, click, type, extract_text, etc.)
Day 9 Goal: Implement the Planner Agent- Takes natural language input from user ("Login to example.com with username 'test@examp- Uses Azure OpenAI GPT-5 to generate structured execution plan- Returns list of ExecutionStep objects with browser tool calls- Handles context and reasoning for each stepProject Structure:
- File: src/EvoAITest.Agents/Agents/PlannerAgent.cs- Namespace: EvoAITest.Agents.Agents- Dependencies: ILLMProvider (from EvoAITest.LLM), BrowserToolRegistry (from EvoAITest.CoRequirements:
- Use Azure OpenAI GPT-5 in production, Ollama in development- Support tool calling (GPT-5 function calling)
- Return typed ExecutionStep list- Include error handling and logging- Support cancellation tokens for Aspire graceful shutdown
Main Implementation Prompt

Generate the PlannerAgent class for EvoAITest browser automation framework.
File: src/EvoAITest.Agents/Agents/PlannerAgent.csNamespace: EvoAITest.Agents.AgentsRequirements:
1. Class Structure: 
- public class PlannerAgent 
- Constructor: PlannerAgent(ILLMProvider llmProvider, ILogger&lt;PlannerAgent&gt; logg 
- Main method: Task&lt;List&lt;ExecutionStep&gt;&gt; PlanAsync(string naturalLanguageP2. PlanAsync Implementation: 
a) Create system prompt explaining agent's role: 
- You are a browser automation planner 
- Convert natural language to browser automation steps 
- Use available tools from BrowserToolRegistry 
- Return structured JSON with steps 
b) Build user prompt with: 
- Natural language input from user 
- Available tools (from BrowserToolRegistry.GetToolsAsJson()) 
- Examples of valid plans 
c) Call LLM with tool definitions: 
- Use ILLMProvider.GenerateAsync() 
- Pass BrowserToolRegistry tools for function calling 
- Request structured JSON response 
d) Parse LLM response: 
- Extract tool calls from response 
- Convert to List&lt;ExecutionStep&gt; 
- Add Order field (1, 2, 3...) 
- Include reasoning for each step 
e) Validate plan: 
- Check all tools exist in registry 
- Validate required parameters present 
- Log plan details for debugging3. Error Handling: 
- Catch LLM failures (timeout, rate limit, invalid response) 
- Catch parsing errors (malformed JSON) 
- Log errors with correlation IDs 
- Re-throw with helpful error messages4. Example Input/Output: 
Input: "Login to example.com with username test@example.com and password SecurePass123 
Output: List&lt;ExecutionStep&gt; 
[ 
{ Order: 1, Action: "navigate", Selector: "", Value: "https://example.com", Reasonin 
{ Order: 2, Action: "wait_for_element", Selector: "#username", Value: "", Reasoning: 
{ Order: 3, Action: "type", Selector: "#username", Value: "test@example.com", Reason
 { Order: 4, Action: "type", Selector: "#password", Value: "SecurePass123", Reasoning 
{ Order: 5, Action: "click", Selector: "button[type='submit']", Reasoning: "Submit l 
]
5. XML Documentation: 
- Class summary: Purpose and usage 
- Method documentation: Parameters, returns, exceptions 
- Include example usage in remarks6. Logging: 
- Log planning start (with prompt) 
- Log LLM call details (model, tokens) 
- Log generated plan (step count) 
- Log errors with full contextDependencies to import:
- EvoAITest.Core.Models (ExecutionStep, BrowserTool)
- EvoAITest.LLM.Abstractions (ILLMProvider)
- Microsoft.Extensions.Logging- System.Text.Json (for parsing)
Generate complete, production-ready code with comprehensive error handling.
Unit Tests Prompt

Generate unit tests for PlannerAgent.
File: src/EvoAITest.Tests/Agents/PlannerAgentTests.csNamespace: EvoAITest.Tests.AgentsTest cases:
1. [Fact] public async Task PlanAsync_WithValidPrompt_ShouldReturnExecutionPlan() 
- Mock ILLMProvider to return valid JSON plan 
- Call PlanAsync with simple login prompt 
- Assert returned list has &gt; 0 steps 
- Assert steps have sequential Order values 
- Assert all Actions are valid tool names2. [Fact] public async Task PlanAsync_WithComplexPrompt_ShouldHandleMultipleSteps() 
- Mock ILLMProvider for multi-step scenario 
- Verify plan has correct sequence 
- Verify reasoning is populated3. [Fact] public async Task PlanAsync_WithLLMFailure_ShouldThrowException() 
- Mock ILLMProvider to throw exception 
- Assert PlanAsync throws 
- Verify error logged4. [Fact] public async Task PlanAsync_WithInvalidJSON_ShouldThrowException() 
- Mock ILLMProvider to return malformed JSON 
- Assert parsing error handling5. [Fact] public async Task PlanAsync_WithCancellation_ShouldRespectToken() 
- Create cancelled CancellationToken
 - Call PlanAsync 
- Assert OperationCanceledException thrownUse Moq for ILLMProvider mocking.
Use FluentAssertions for readable assertions.

Day 15: Task CRUD API (REST Endpoints)
--------------------------------------

**Context:** Repositories, DbContext, and migrations exist. We now need minimal APIs inside `EvoAITest.ApiService` so users can create/list/update/delete tasks and inspect execution history. Endpoints must respect authorization, return DTOs, and emit OpenAPI metadata.

### Implementation Prompt

```
Generate the TaskEndpoints extension class.
File: src/EvoAITest.ApiService/Endpoints/TaskEndpoints.cs
Namespace: EvoAITest.ApiService.Endpoints

Requirements:
1. Add `MapTaskEndpoints(this WebApplication app)` that:
   - Creates a route group `/api/tasks`
   - Adds OpenAPI tagging, summaries, and response metadata
   - Requires an authenticated user (fall back to dev user if claims missing)
2. Routes:
   - POST `/api/tasks` -> Create task from CreateTaskRequest; returns 201 + TaskResponse
   - GET `/api/tasks` -> List current user's tasks, optional `status` query
   - GET `/api/tasks/{id}` -> Fetch single task (200/404/403)
   - PUT `/api/tasks/{id}` -> Update name/description/status
   - DELETE `/api/tasks/{id}` -> Cascade delete task + executions
   - GET `/api/tasks/{id}/history` -> Return execution history ordered by `StartedAt`
3. Each handler should:
   - Resolve `IAutomationTaskRepository`
   - Extract `userId` from `ClaimTypes.NameIdentifier` or `sub`, fallback `anonymous-user`
   - Log start/end via `ILogger<Program>`
   - Handle not-found/user mismatch with 404/403 responses
   - Convert entities to DTOs using helpers (see TaskModels)
4. Add request/response DTOs in `EvoAITest.ApiService/Models/TaskModels.cs` with data annotations + static `FromEntity` helpers.
5. Register endpoints in `Program.cs` using `app.MapTaskEndpoints();`
```

### Testing Prompt

```
Describe manual/integration steps:
- POST returns 201 + Location header, payload validated (name + prompt required)
- GET `/api/tasks?status=Executing` filters via repository composite index
- PUT rejects updates for other users (403) and logs warning
- DELETE removes task + related execution history (verify via repository)
- GET history returns newest-first and includes metadata fields.
Mention Day 16 follow-up: WebApplicationFactory-based integration tests + execution control endpoints.
```
Day 10: Executor Agent (Run Automation Steps)

Context-Setting Prompt

Day 10: Implement the Executor Agent for EvoAITest.
Current Progress:
✅ Days 1-9: Planner Agent generates execution plans from natural languageDay 10 Goal: Implement Executor Agent- Takes execution plan (List&lt;ExecutionStep&gt;) from Planner- Executes each step using IBrowserAgent- Captures results and screenshots- Handles errors and retries- Returns ExecutionResult with step outcomesFile: src/EvoAITest.Agents/Agents/ExecutorAgent.csNamespace: EvoAITest.Agents.Agents
Main Implementation Prompt

Generate the ExecutorAgent class.
File: src/EvoAITest.Agents/Agents/ExecutorAgent.csNamespace: EvoAITest.Agents.AgentsRequirements:
1. Class Structure: 
- public class ExecutorAgent 
- Constructor: ExecutorAgent(IBrowserAgent browserAgent, ILogger&lt;ExecutorAgent&gt; 
- Main method: Task&lt;ExecutionResult&gt; ExecuteAsync(List&lt;ExecutionStep&gt; plan2. ExecuteAsync Implementation: 
a) Initialize browser: 
- Call browserAgent.InitializeAsync() 
- Handle initialization errors 
b) Loop through execution steps: 
- For each step in plan (ordered): 
* Log step start 
* Execute step based on Action property 
* Measure execution time (DurationMs) 
* Capture step result (success/failure) 
* Take screenshot on errors
→ browserAgent.GetPageStateAsync() 
- "take_screenshot" → browserAgent.TakeScreenshotAsync() 
- [All 13 tools from BrowserToolRegistry] 
d) Build ExecutionResult: 
- TaskId: from plan context 
- Status: Success/PartialSuccess/Failed based on step outcomes 
- Steps: list of StepResult for each step 
- FinalOutput: summary or extracted data 
- ErrorMessage: if execution failed 
- TotalDurationMs: sum of all step durations3. Error Handling: 
- Try-catch around each step execution 
- On error: capture screenshot, log error, continue or abort based on severity 
- Differentiate between: 
* Recoverable errors (element not found - might succeed with retry) 
* Fatal errors (network failure - abort immediately) 
* Store StepResult 
c) Map Action to IBrowserAgent methods: 
- "navigate" → browserAgent.NavigateAsync(step.Value) 
- "click" → browserAgent.ClickAsync(step.Selector) 
- "type" → browserAgent.TypeAsync(step.Selector, step.Value) 
- "wait_for_element" → browserAgent.WaitForElementAsync(step.Selector) 
- "get_page_state" 
4. Retry Logic: 
- For certain actions (click, wait_for_element), implement retry 
- Use exponential backoff (1s, 2s, 4s) 
- Max retries from configuration5. Screenshot Capture: 
- On step failure 
- On final step (success or failure) 
- Upload to Azure Blob Storage (or save to /tmp/screenshots) 
- Return screenshot URL in StepResult6. Example Execution Flow: 
Input: Plan with 5 steps (navigate, wait, type username, type password, click submit) 
Process: 
- Step 1: Navigate → Success (200ms) 
- Step 2: Wait for element → Success (1500ms) 
- Step 3: Type username → Success (100ms) 
- Step 4: Type password → Success (100ms) 
- Step 5: Click submit → Success (50ms) 
Output: ExecutionResult 
{ 
Status: Success, 
Steps: [5 StepResults], 
TotalDurationMs: 1950, 
FinalOutput: "Login successful" 
}
Dependencies:
- EvoAITest.Core.Abstractions (IBrowserAgent)
- EvoAITest.Core.Models (ExecutionStep, ExecutionResult, StepResult)
- Microsoft.Extensions.LoggingGenerate complete code with comprehensive logging and error handling.
Unit Tests Prompt

Generate unit tests for ExecutorAgent.
File: src/EvoAITest.Tests/Agents/ExecutorAgentTests.csNamespace: EvoAITest.Tests.AgentsTest cases:
1. [Fact] public async Task ExecuteAsync_WithValidPlan_ShouldReturnSuccess() 
- Mock IBrowserAgent methods 
- Create simple plan (navigate, click) 
- Execute plan 
- Assert Status == Success 
- Assert all steps have Success = true2. [Fact] public async Task ExecuteAsync_WithStepFailure_ShouldCaptureError() 
- Mock one step to throw exception 
- Execute plan 
- Assert Status == Failed or PartialSuccess 
- Assert failed step has ErrorMessage populated3. [Fact] public async Task ExecuteAsync_WithRetry_ShouldRetryFailedSteps() 
- Mock step to fail first time, succeed second time 
- Execute plan 
- Verify retry occurred (2 calls to browser method)
4. [Fact] public async Task ExecuteAsync_CapturesScreenshotOnFailure() 
- Mock step to fail 
- Execute plan 
- Verify TakeScreenshotAsync called 
- Assert StepResult has ScreenshotUrl5. [Fact] public async Task ExecuteAsync_MeasuresStepDuration() 
- Execute plan 
- Assert each StepResult has DurationMs &gt; 0 
- Assert TotalDurationMs == sum of step durationsMock IBrowserAgent using Moq.
Day 11: Healer Agent (Error Recovery)

Context-Setting Prompt

Day 11: Implement the Healer Agent for EvoAITest.
Current Progress:
✅ Days 1-10: Planner creates plans, Executor runs themDay 11 Goal: Implement Healer Agent- Analyzes execution failures from Executor- Uses LLM to diagnose root cause- Suggests alternative approaches- Optionally re-plans and retries with modificationsFile: src/EvoAITest.Agents/Agents/HealerAgent.csNamespace: EvoAITest.Agents.Agents
Main Implementation Prompt

Generate the HealerAgent class.
File: src/EvoAITest.Agents/Agents/HealerAgent.csNamespace: EvoAITest.Agents.AgentsRequirements:
1. Class Structure: 
- public class HealerAgent 
- Constructor: HealerAgent(ILLMProvider llmProvider, IBrowserAgent browserAgent, ILogg 
- Main method: Task&lt;List&lt;ExecutionStep&gt;&gt; HealAsync(ExecutionResult failedR2. HealAsync Implementation: 
a) Analyze failure: 
- Extract failed steps from ExecutionResult 
- Get error messages and screenshot URLs 
- Get page state at failure point (if available) 
b) Build diagnostic prompt for LLM: 
- Original user intent 
- Execution plan that failed 
- Failed step details (which step, error message) 
- Screenshot analysis (if available) 
- Current page state 
c) Request LLM analysis: 
- Why did execution fail? 
- What likely caused the error? (element not found, timeout, wrong selector, etc.) 
- How to fix the plan? 
d) Generate alternative plan: 
- LLM suggests modified steps 
- Use alternative selectors (try xpath instead of CSS, try text content)
 - Page state verification: Add get_page_state before critical steps4. Example Healing Scenario: 
Original Plan: 
- Step 1: Navigate to example.com ✅ 
- Step 2: Click #login-button ❌ (Element not found) 
Healer Analysis: 
- Error: Element with selector "#login-button" not found 
- Screenshot shows button has class="btn-login" 
- LLM suggests: Use class selector instead 
Healed Plan: 
- Step 1: Navigate to example.com 
- Step 2: Wait for element .btn-login (NEW - ensure element loaded) 
- Step 3: Click .btn-login (MODIFIED - use class selector)
5. Max Healing Attempts: 
- Track healing attempts (don't heal infinitely) 
- Max 2-3 healing attempts 
- If still failing, return diagnostic report instead of new plan6. Logging: 
- Log failure analysis 
- Log LLM diagnostic response 
- Log suggested changes 
- Log healed planDependencies:
- EvoAITest.LLM.Abstractions (ILLMProvider) 
- Add wait steps before problematic actions 
- Break complex steps into smaller steps 
e) Return healed plan: 
- New List&lt;ExecutionStep&gt; with modifications 
- Include reasoning explaining changes 
- Mark as "healed" attempt in metadata3. Healing Strategies: 
- Selector alternatives: If CSS selector fails, try XPath, text content, etc. 
- Timing adjustments: Add longer waits, explicit wait_for_element 
- Step decomposition: Break "type and submit" into separate type + click- EvoAITest.Core.Abstractions (IBrowserAgent)
- EvoAITest.Core.Models (ExecutionResult, ExecutionStep)
- Microsoft.Extensions.LoggingGenerate complete code with intelligent error analysis and recovery.
Unit Tests Prompt

Generate unit tests for HealerAgent.
File: src/EvoAITest.Tests/Agents/HealerAgentTests.csNamespace: EvoAITest.Tests.Agents
Test cases:
1. [Fact] public async Task HealAsync_WithFailedResult_ShouldReturnModifiedPlan() 
- Create ExecutionResult with failed step 
- Mock LLM to suggest alternative selector 
- Call HealAsync 
- Assert returned plan has modifications 
- Assert reasoning mentions the fix2. [Fact] public async Task HealAsync_WithSelectorFailure_ShouldSuggestAlternatives() 
- Failed step: "Element not found" 
- Mock LLM to analyze and suggest xpath 
- Verify healed plan uses different selector3. [Fact] public async Task HealAsync_WithTimingIssue_ShouldAddWaitSteps() 
- Failed step: Timeout waiting for element 
- Mock LLM to suggest longer wait 
- Verify healed plan includes explicit wait step4. [Fact] public async Task HealAsync_AfterMaxAttempts_ShouldReturnDiagnostic() 
- Simulate 3 failed healing attempts 
- Verify returns diagnostic instead of new plan 
- Verify appropriate error message5. [Fact] public async Task HealAsync_AnalyzesPageState_ForBetterDiagnosis() 
- Include page state in failed result 
- Mock LLM to reference page state in analysis 
- Verify diagnostic uses page contextMock ILLMProvider and IBrowserAgent.
Week 3: Database & Repository Layer (Days 12-
14)

Day 12: EF Core Database Models

Context-Setting Prompt

Day 12: Implement Entity Framework Core models for EvoAITest.
Current Progress:
✅ Days 1-11: AI agents implemented (Planner, Executor, Healer)
Day 12 Goal: Define database models with EF Core- AutomationTask entity (for persistence)
- ExecutionHistory entity- Configure relationships- Add indexes for performanceDatabase: Azure SQL Database (via Aspire resource)
ORM: Entity Framework Core 9
Main Implementation Prompt

Generate EF Core database models and DbContext for EvoAITest.
File: src/EvoAITest.Core/Data/EvoAIDbContext.csNamespace: EvoAITest.Core.DataRequirements:
1. DbContext Class: 
- public class EvoAIDbContext : DbContext 
- Constructor: EvoAIDbContext(DbContextOptions&lt;EvoAIDbContext&gt; options) 
- DbSets for AutomationTask and ExecutionHistory2. AutomationTask Entity (update existing model): 
- Add EF Core attributes: 
* [Table("AutomationTasks")] 
* [Key] on Id (Guid) 
* [Required] on Name, NaturalLanguagePrompt 
* [MaxLength(500)] on Name 
* [Column(TypeName = "nvarchar(max)")] for Plan (JSON) 
- Add navigation properties: 
* public ICollection&lt;ExecutionHistory&gt; Executions { get; set; } 
- Add indexes: 
* Index on UserId 
* Index on Status 
* Index on CreatedAt 
* Composite index on (UserId, Status) for filtering3. ExecutionHistory Entity (new model): 
- File: src/EvoAITest.Core/Models/ExecutionHistory.cs 
- Properties: 
* Id (Guid) - Primary Key 
* TaskId (Guid) - Foreign Key to AutomationTask 
* ExecutionStatus (enum: Success, Failed, PartialSuccess) 
* StartedAt (DateTimeOffset) 
* CompletedAt (DateTimeOffset?) 
* DurationMs (int) 
* StepResults (string) - JSON serialized List&lt;StepResult&gt; 
* FinalOutput (string) 
* ErrorMessage (string?) 
* ScreenshotUrls (string) - JSON array of URLs 
* CorrelationId (string) - for distributed tracing 
- Navigation properties: 
* public AutomationTask Task { get; set; } 
- Indexes: 
* Index on TaskId 
* Index on ExecutionStatus 
* Index on StartedAt (for time-based queries)
4. OnModelCreating Configuration: 
- Configure AutomationTask:
 * Set Plan as JSON column 
* Configure Status enum as string 
* Set up cascade delete for ExecutionHistory 
- Configure ExecutionHistory: 
* Set StepResults as JSON column 
* Set ScreenshotUrls as JSON column 
* Configure relationship to AutomationTask5. Connection String Configuration: 
- Add in appsettings.json: 
```json 
"ConnectionStrings": { 
"EvoAIDatabase": "Server=(localdb)\\mssqllocaldb;Database=EvoAITest;Trusted_Connec 
} 
``` 
- For Azure SQL (production): 
* Use environment variable 
* Reference from Aspire SQL resource6. Register DbContext in DI: 
- Update ServiceCollectionExtensions.cs: 
```csharp 
services.AddDbContext&lt;EvoAIDbContext&gt;(options =&gt; 
options.UseSqlServer(configuration.GetConnectionString("EvoAIDatabase"))); 
```
Dependencies:
- Microsoft.EntityFrameworkCore- Microsoft.EntityFrameworkCore.SqlServer- EvoAITest.Core.ModelsGenerate complete DbContext and entity configurations.
Day 13: Database Migrations

Implementation Prompt

Generate Entity Framework Core migrations for EvoAITest database.
Requirements:
1. Initial Migration: 
- Run command: dotnet ef migrations add InitialCreate --project src/EvoAITest.Core 
- Review generated migration file 
- Ensure tables created: AutomationTasks, ExecutionHistory 
- Verify indexes created 
- Verify foreign key constraints2. Migration Files to Generate: 
- Migrations/YYYYMMDDHHMMSS_InitialCreate.cs 
- Migrations/EvoAIDbContextModelSnapshot.cs
3. Apply Migration (local): 
- dotnet ef database update --project src/EvoAITest.Core 
- Verify database created in localdb 
- Verify tables and indexes present4. Production Migration Strategy: 
- Use migration bundles for Azure deployment 
- Generate SQL scripts for review: 
* dotnet ef migrations script --output migration.sql 
- Apply during azd deployment5. Add to Program.cs (for auto-migration in development): 
```csharp 
if (app.Environment.IsDevelopment()) 
{ 
using var scope = app.Services.CreateScope(); 
var dbContext = scope.ServiceProvider.GetRequiredService&lt;EvoAIDbContext&gt;(); 
await dbContext.Database.MigrateAsync(); 
}
Aspire Integration:
In EvoAITest.AppHost, add SQL resource:
var sql = builder.AddSqlServer("sql") 
.AddDatabase("evoaidb");
var apiService = builder.AddProject&lt;Projects.EvoAITest_ApiService&gt;("api") 
.WithReference(sql);





Document migration commands and production deployment process.
---
## Day 14: Repository Pattern### Implementation PromptGenerate Repository pattern implementation for EvoAITest.
File: src/EvoAITest.Core/Repositories/IAutomationTaskRepository.cssrc/EvoAITest.Core/Repositories/AutomationTaskRepository.csRequirements:
IAutomationTaskRepository Interface:
Task<AutomationTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken =
default)






Task<List<AutomationTask>> GetByUserIdAsync(string userId, CancellationTokencancellationToken = default)
Task<List<AutomationTask>> GetByStatusAsync(TaskStatus status, CancellationTokencancellationToken = default)
Task<AutomationTask> CreateAsync(AutomationTask task, CancellationTokencancellationToken = default)
Task UpdateAsync(AutomationTask task, CancellationToken cancellationToken = default)
Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
Task<List<ExecutionHistory>> GetExecutionHistoryAsync(Guid taskId, CancellationTokencancellationToken = default)
Task AddExecutionHistoryAsync(ExecutionHistory history, CancellationTokencancellationToken = default)



AutomationTaskRepository Implementation:
Constructor: AutomationTaskRepository(EvoAIDbContext context,
ILogger<AutomationTaskRepository> logger)
Implement all interface methods using EF Core
Use AsNoTracking for read-only queries
Include related entities where needed (Include(t => t.Executions))
Add logging for all operations
Handle concurrency conflicts



Example Implementation Pattern:
public async Task&lt;AutomationTask?&gt; GetByIdAsync(Guid id, CancellationToken canc{ 
_logger.LogInformation("Retrieving task {TaskId}", id); 
return await _context.AutomationTasks 
.Include(t =&gt; t.Executions) 
.AsNoTracking() 
.FirstOrDefaultAsync(t =&gt; t.Id == id, cancellationToken);
}
Register in DI:
services.AddScoped<IAutomationTaskRepository, AutomationTaskRepository>()



Unit Tests:
Use InMemory database for testing
Test CRUD operations
Test querying by UserId and Status
Test execution history operations





Generate complete repository implementation with comprehensive error handling.
---
# Week 4: API Development (Days 15-16)
## Day 15: API Endpoints (Task CRUD)
### Implementation Prompt
Generate REST API endpoints for AutomationTask CRUD operations.
File: src/EvoAITest.ApiService/Endpoints/TaskEndpoints.csNamespace: EvoAITest.ApiService.EndpointsRequirements:
Minimal API Endpoints:
POST /api/tasks - Create new task
GET /api/tasks - Get all tasks for user
GET /api/tasks/{id} - Get task by ID
PUT /api/tasks/{id} - Update task
DELETE /api/tasks/{id} - Delete task
GET /api/tasks/{id}/history - Get execution history



POST /api/tasks Implementation:
Request body: CreateTaskRequest { Name, Description, NaturalLanguagePrompt }
Validate input (required fields)
Create AutomationTask entity
Set UserId from authenticated user (or claim)
Save to database via repository
Return 201 Created with task details



GET /api/tasks Implementation:
Query parameter: status (optional filter)
Get UserId from claims
Query repository for user's tasks
Return 200 OK with list of tasks



GET /api/tasks/{id} Implementation:
Validate user owns task (authorization check)
Get task from repository
Return 200 OK or 404 Not Found






PUT /api/tasks/{id} Implementation:
Request body: UpdateTaskRequest { Name, Description, Status }
Validate user owns task
Update task entity
Save changes via repository
Return 200 OK



DELETE /api/tasks/{id} Implementation:
Validate user owns task
Delete via repository
Return 204 No Content



Register Endpoints:
In Program.cs:
app.MapTaskEndpoints();



Add Authentication/Authorization:
Use minimal API authorization:
.RequireAuthorization()
Extract UserId from claims



Example Endpoint Pattern:
app.MapPost("/api/tasks", async ( 
CreateTaskRequest request, 
IAutomationTaskRepository repository, 
ILogger&lt;Program&gt; logger, 
ClaimsPrincipal user, 
CancellationToken cancellationToken) =&gt;
{ 
var userId = user.FindFirstValue(ClaimTypes.NameIdentifier); 
// Implementation... 
return Results.Created($"/api/tasks/{task.Id}", task);
})
.RequireAuthorization();


Generate complete API endpoints with validation, authorization, and error handling.
---
## Day 16: API Endpoints (Execution)
### Implementation Prompt
Generate API endpoints for task execution workflow.
File: src/EvoAITest.ApiService/Endpoints/ExecutionEndpoints.csRequirements:
POST /api/tasks/{id}/execute - Start task execution:
Get task from repository
Validate user owns task
Call PlannerAgent to generate plan
Call ExecutorAgent to run plan
If execution fails, optionally call HealerAgent
Save ExecutionHistory to database
Return ExecutionResult



GET /api/tasks/{id}/executions - Get execution history:
Return all execution attempts for task
Include success/failure status
Include duration and error messages



GET /api/executions/{executionId} - Get specific execution:
Return detailed execution result
Include step-by-step results
Include screenshot URLs



POST /api/tasks/{id}/execute/heal - Retry with healing:
Get last failed execution
Call HealerAgent to analyze and fix
Execute healed plan
Return new ExecutionResult



Execution Flow:
app.MapPost("/api/tasks/{id}/execute", async ( 
Guid id, 
IAutomationTaskRepository repository, 
PlannerAgent planner, 
ExecutorAgent executor, 
HealerAgent healer, 
ILogger logger, 
CancellationToken cancellationToken) =&gt;
{ 
// 1. Get task 
var task = await repository.GetByIdAsync(id, cancellationToken); 
if (task == null) return Results.NotFound(); 
// 2. Plan execution



 var plan = await planner.PlanAsync(task.NaturalLanguagePrompt, cancellationToken) 
task.SetPlan(plan); 
// 3. Execute 
var result = await executor.ExecuteAsync(plan, cancellationToken); 
// 4. If failed, try healing (optional) 
if (result.Status == ExecutionStatus.Failed) 
{ 
var healedPlan = await healer.HealAsync(result, task.NaturalLanguagePrompt, c 
result = await executor.ExecuteAsync(healedPlan, cancellationToken); 
} 
// 5. Save execution history 
var history = new ExecutionHistory { /* map from result */ }; 
await repository.AddExecutionHistoryAsync(history, cancellationToken); 
// 6. Update task status 
task.UpdateStatus(result.Status == ExecutionStatus.Success ? TaskStatus.Completed 
await repository.UpdateAsync(task, cancellationToken); 
return Results.Ok(result);
});
Background Execution (optional):
Use IHostedService for long-running executions
Return 202 Accepted immediately
Provide status endpoint to check progress





Generate complete execution endpoints with comprehensive error handling.
---
# Week 5: DevOps &amp; Testing (Days 17-20)
## Day 17: Docker Containerization### Implementation PromptGenerate Dockerfile and container configuration for EvoAITest Aspire app.
Requirements:
Dockerfile for ApiService:
File: src/EvoAITest.ApiService/Dockerfile
Base image: mcr.microsoft.com/dotnet/aspnet:10.0
SDK image: mcr.microsoft.com/dotnet/sdk:10.0
Install Playwright dependencies for browser automation






Copy all projects and restore dependencies
Build and publish ApiService
Set environment variables for production
Expose port 8080
Health check endpoint



Install Playwright in Container:
RUN apt-get update &amp;&amp; apt-get install -y \ 
wget \ 
gnupg \ 
&amp;&amp; wget -q -O - https://dl.google.com/linux/linux_signing_key.pub | apt-k 
&amp;&amp; echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable m 
&amp;&amp; apt-get update \ 
&amp;&amp; apt-get install -y google-chrome-stableRUN pwsh -Command "&amp; { . ./playwright.ps1; Install-Playwright chromium }"
Aspire Manifest for Container Apps:
Generate: dotnet run --project src/EvoAITest.AppHost -- --publisher manifest --output-pathaspire-manifest.json
Review generated manifest
Customize for Azure Container Apps



Docker Compose (local development):
File: docker-compose.yml
Services: api, web, database, redis
Volumes for data persistence
Network configuration



Container Registry:
Push to Azure Container Registry:
az acr build --registry evoaiacr \ 
--image evoaitest-api:latest \ 
--file src/EvoAITest.ApiService/Dockerfile .





Generate complete Docker configuration for production deployment.
---
## Day 18: CI/CD Pipeline### Implementation PromptGenerate GitHub Actions CI/CD pipeline for EvoAITest.
File: .github/workflows/deploy.ymlRequirements:
Trigger: On push to main branch, on pull request
Jobs:
a) Build and Test:
Checkout code
Setup .NET 10 SDK
Restore dependencies
Build solution
Run unit tests
Generate test coverage report


b) Build Docker Images:
Login to Azure Container Registry
Build ApiService image
Build Web image
Push to registry with tags (latest, git SHA)


c) Deploy to Azure:
Login to Azure with service principal
Run azd deploy
Apply database migrations
Verify deployment health



Secrets Configuration:
AZURE_CREDENTIALS (service principal JSON)
AZURE_CONTAINER_REGISTRY_LOGIN
LLMAPIKEY (for production LLM access)



Example Workflow:
name: Deploy EvoAITeston: 
push: 
branches: [main] 
pull_request: 
branches: [main]
jobs: 
build-test: 
runs-on: ubuntu-latest 
steps: 
- uses: actions/checkout@v4



 - uses: actions/setup-dotnet@v4 
with: 
dotnet-version: '10.0.x' 
- run: dotnet restore 
- run: dotnet build --no-restore 
- run: dotnet test --no-build --verbosity normal 
deploy: 
needs: build-test 
if: github.ref == 'refs/heads/main' 
runs-on: ubuntu-latest 
steps: 
- uses: azure/login@v1 
with: 
creds: ${{ secrets.AZURE_CREDENTIALS }} 
- run: azd deploy


Generate complete CI/CD pipeline with all deployment steps.
---
## Day 19: Integration Tests

### Implementation Prompt
Generate integration tests for EvoAITest using WebApplicationFactory.
File: src/EvoAITest.Tests/Integration/ApiIntegrationTests.csRequirements:
Test Setup:
Use WebApplicationFactory<Program>
Configure test database (InMemory or TestContainers)
Mock external dependencies (Azure OpenAI, Ollama)
Seed test data



Test Cases:
a) Task CRUD Integration Tests:
[Fact] public async Task CreateTask_ReturnsCreated()
[Fact] public async Task GetTasks_ReturnsUserTasks()
[Fact] public async Task UpdateTask_UpdatesSuccessfully()
[Fact] public async Task DeleteTask_DeletesSuccessfully()


b) Execution Flow Integration Tests:
[Fact] public async Task ExecuteTask_WithValidTask_ReturnsSuccess()
[Fact] public async Task ExecuteTask_WithInvalidSelector_FailsGracefully()






[Fact] public async Task ExecuteTask_WithHealing_RecoversFromFailure()


c) End-to-End Scenarios:
[Fact] public async Task LoginAutomation_CompleteFlow_Succeeds()
Create task with "Login to example.com"
Execute task
Verify execution history created
Verify task status updated






Test WebApplicationFactory:
public class EvoAITestFactory : WebApplicationFactory&lt;Program&gt;
{ 
protected override void ConfigureWebHost(IWebHostBuilder builder) 
{ 
builder.ConfigureServices(services =&gt; 
{ 
// Replace real DbContext with InMemory 
services.AddDbContext&lt;EvoAIDbContext&gt;(options =&gt; 
options.UseInMemoryDatabase("TestDb")); 
// Mock LLM provider 
services.AddSingleton&lt;ILLMProvider, MockLLMProvider&gt;(); 
// Mock browser agent for faster tests 
services.AddScoped&lt;IBrowserAgent, MockBrowserAgent&gt;(); 
}); 
}
}
Helper Classes:
MockLLMProvider: Returns predefined plans
MockBrowserAgent: Simulates browser actions without real browser





Generate comprehensive integration test suite.
---
## Day 20: Example Automation (Login)
### Implementation Prompt
Generate a complete working example: Login automation for example.com.
File: examples/LoginAutomationExample.csRequirements:
Example Code:



public class LoginAutomationExample{ 
private readonly IAutomationTaskRepository _repository; 
private readonly PlannerAgent _planner; 
private readonly ExecutorAgent _executor; 
public async Task RunLoginExample() 
{ 
// 1. Create task 
var task = new AutomationTask 
{ 
Id = Guid.NewGuid(), 
UserId = "example-user", 
Name = "Login to Example.com", 
Description = "Automated login test", 
NaturalLanguagePrompt = "Go to https://example.com and login with usernam 
Status = TaskStatus.Pending 
}; 
await _repository.CreateAsync(task); 
// 2. Plan execution 
var plan = await _planner.PlanAsync(task.NaturalLanguagePrompt); 
task.SetPlan(plan); 
await _repository.UpdateAsync(task); 
// 3. Execute 
var result = await _executor.ExecuteAsync(plan); 
// 4. Save results 
var history = new ExecutionHistory 
{ 
TaskId = task.Id, 
ExecutionStatus = result.Status, 
DurationMs = result.TotalDurationMs, 
FinalOutput = result.FinalOutput 
}; 
await _repository.AddExecutionHistoryAsync(history); 
// 5. Print results 
Console.WriteLine($"Execution Status: {result.Status}"); 
Console.WriteLine($"Duration: {result.TotalDurationMs}ms"); 
Console.WriteLine($"Steps executed: {result.Steps.Count}"); 
}
}
Documentation:
README.md explaining the example
Prerequisites (Ollama or Azure OpenAI configured)
How to run: dotnet run --project examples/LoginExample
Expected output and screenshots



Sample Output:



Starting login automation example...
Planning execution...
✅ Plan generated: 6 stepsExecuting steps...
✅ Step 1: Navigate to https://example.com (250ms)
✅ Step 2: Wait for login form (500ms)
✅ Step 3: Type username (100ms)
✅ Step 4: Type password (100ms)
✅ Step 5: Click login button (50ms)
✅ Step 6: Verify welcome message (300ms)
Execution Status: SuccessTotal Duration: 1300msTask saved to database with ID: a1b2c3d4...


Generate complete working example with comprehensive documentation.
---
# Summary: Days 9-20 Development Plan## Week 2: AI Agents (Days 9-11)
- **Day 9:** PlannerAgent - Converts natural language to execution plans using GPT-5- **Day 10:** ExecutorAgent - Runs browser automation steps with retry logic- **Day 11:** HealerAgent - Analyzes failures and suggests fixes using AI## Week 3: Data Layer (Days 12-14)
- **Day 12:** EF Core models and DbContext for Azure SQL- **Day 13:** Database migrations and Aspire SQL integration- **Day 14:** Repository pattern for clean data access## Week 4: API Layer (Days 15-16)
- **Day 15:** REST API endpoints for task CRUD operations- **Day 16:** Execution endpoints with healing workflow## Week 5: DevOps (Days 17-20)
- **Day 17:** Docker containerization with Playwright support- **Day 18:** GitHub Actions CI/CD pipeline with Azure deployment- **Day 19:** Integration tests with WebApplicationFactory- **Day 20:** Complete login automation example---
**All prompts are designed for Claude Sonnet 4.5 with your specific EvoAITest project str
