# Execution Endpoints API Documentation

## Overview

The Execution Endpoints provide comprehensive APIs for executing automation tasks using the AI-powered agent system. These endpoints integrate the **PlannerAgent** (natural language ? execution plan), **ExecutorAgent** (plan execution), and **HealerAgent** (error recovery) to deliver robust browser automation.

## Base URL

```
/api/tasks
```

## Endpoints

### 1. Execute Task (Synchronous)

Executes a task synchronously by generating a plan and running it. This is the primary endpoint for immediate execution with blocking behavior.

**Endpoint**: `POST /api/tasks/{id}/execute`

**Parameters**:
- `id` (path, required): Task ID (GUID)

**Response**: `200 OK` - Returns `AgentTaskResult`

**Example Request**:
```bash
curl -X POST "https://localhost:7001/api/tasks/123e4567-e89b-12d3-a456-426614174000/execute" \
  -H "Content-Type: application/json"
```

**Example Response**:
```json
{
  "taskId": "123e4567-e89b-12d3-a456-426614174000",
  "success": true,
  "status": "Completed",
  "stepResults": [
    {
      "stepId": "step-1",
      "success": true,
      "durationMs": 2000,
      "executionResult": {
        "success": true,
        "actionId": "step-1",
        "durationMs": 2000
      }
    }
  ],
  "durationMs": 5000,
  "startedAt": "2024-01-15T10:00:00Z",
  "completedAt": "2024-01-15T10:00:05Z",
  "statistics": {
    "totalSteps": 5,
    "successfulSteps": 5,
    "failedSteps": 0,
    "retriedSteps": 0,
    "successRate": 1.0
  },
  "screenshots": ["base64-encoded-screenshot"],
  "metadata": {
    "plan_id": "plan-abc123",
    "session_id": "session-xyz789"
  }
}
```

**Error Responses**:
- `400 Bad Request` - Plan validation failed
- `404 Not Found` - Task not found
- `500 Internal Server Error` - Execution failed

---

### 2. Execute Task (Background)

Starts task execution in the background and returns immediately with a 202 Accepted response. Use the status endpoint to check progress.

**Endpoint**: `POST /api/tasks/{id}/execute/background`

**Parameters**:
- `id` (path, required): Task ID (GUID)

**Response**: `202 Accepted` - Returns `BackgroundExecutionResponse`

**Example Request**:
```bash
curl -X POST "https://localhost:7001/api/tasks/123e4567-e89b-12d3-a456-426614174000/execute/background" \
  -H "Content-Type: application/json"
```

**Example Response**:
```json
{
  "executionId": "exec-abc123",
  "taskId": "123e4567-e89b-12d3-a456-426614174000",
  "statusUrl": "/api/tasks/123e4567-e89b-12d3-a456-426614174000/execute/status?executionId=exec-abc123",
  "message": "Execution started in background"
}
```

---

### 3. Get Execution Status

Retrieves the current status of a background execution.

**Endpoint**: `GET /api/tasks/{id}/execute/status?executionId={executionId}`

**Parameters**:
- `id` (path, required): Task ID (GUID)
- `executionId` (query, required): Execution ID from background response

**Response**: `200 OK` - Returns `ExecutionStatus`

**Example Request**:
```bash
curl "https://localhost:7001/api/tasks/123e4567-e89b-12d3-a456-426614174000/execute/status?executionId=exec-abc123"
```

**Example Response**:
```json
{
  "executionId": "exec-abc123",
  "taskId": "123e4567-e89b-12d3-a456-426614174000",
  "status": "Executing",
  "planId": "plan-xyz789",
  "startedAt": "2024-01-15T10:00:00Z",
  "completedAt": null,
  "success": null,
  "errorMessage": null,
  "durationMs": null
}
```

**Status Values**:
- `Queued` - Execution queued but not started
- `Planning` - Generating execution plan
- `Executing` - Running automation steps
- `Completed` - Execution finished successfully
- `Failed` - Execution failed

---

### 4. Get Execution History

Retrieves all execution attempts for a task, including timestamps, durations, and outcomes.

**Endpoint**: `GET /api/tasks/{id}/executions`

**Parameters**:
- `id` (path, required): Task ID (GUID)

**Response**: `200 OK` - Returns `List<ExecutionHistoryDto>`

**Example Request**:
```bash
curl "https://localhost:7001/api/tasks/123e4567-e89b-12d3-a456-426614174000/executions"
```

**Example Response**:
```json
[
  {
    "executionId": "exec-1",
    "taskId": "123e4567-e89b-12d3-a456-426614174000",
    "startedAt": "2024-01-15T09:00:00Z",
    "completedAt": "2024-01-15T09:05:00Z",
    "status": "Completed",
    "success": true,
    "durationMs": 300000,
    "stepsCompleted": 10,
    "stepsTotal": 10
  },
  {
    "executionId": "exec-2",
    "taskId": "123e4567-e89b-12d3-a456-426614174000",
    "startedAt": "2024-01-15T10:00:00Z",
    "completedAt": "2024-01-15T10:02:30Z",
    "status": "Failed",
    "success": false,
    "durationMs": 150000,
    "stepsCompleted": 5,
    "stepsTotal": 10,
    "errorMessage": "Element not found: #submit-button"
  }
]
```

---

### 5. Get Execution Details

Retrieves detailed execution results including step-by-step outcomes, screenshots, and metadata.

**Endpoint**: `GET /api/executions/{executionId}`

**Parameters**:
- `executionId` (path, required): Execution ID (GUID)

**Response**: `200 OK` - Returns `ExecutionDetailsDto`

**Example Request**:
```bash
curl "https://localhost:7001/api/executions/exec-abc123"
```

**Example Response**:
```json
{
  "executionId": "exec-abc123",
  "taskId": "123e4567-e89b-12d3-a456-426614174000",
  "startedAt": "2024-01-15T10:00:00Z",
  "completedAt": "2024-01-15T10:03:00Z",
  "status": "Completed",
  "success": true,
  "durationMs": 180000,
  "stepResults": [
    {
      "stepNumber": 1,
      "action": "navigate",
      "success": true,
      "durationMs": 2000,
      "output": "Navigated to https://example.com"
    },
    {
      "stepNumber": 2,
      "action": "click",
      "success": true,
      "durationMs": 500,
      "output": "Clicked button",
      "screenshotUrl": "/screenshots/step-2.png"
    }
  ],
  "screenshots": [
    "/screenshots/exec-123-final.png"
  ],
  "metadata": {
    "plan_id": "plan-xyz789",
    "user_agent": "Mozilla/5.0..."
  }
}
```

---

### 6. Execute with Healing

Retries task execution with healing analysis on the last failed execution. The healer agent analyzes failures and applies recovery strategies.

**Endpoint**: `POST /api/tasks/{id}/execute/heal`

**Parameters**:
- `id` (path, required): Task ID (GUID)

**Response**: `200 OK` - Returns `AgentTaskResult`

**Example Request**:
```bash
curl -X POST "https://localhost:7001/api/tasks/123e4567-e89b-12d3-a456-426614174000/execute/heal" \
  -H "Content-Type: application/json"
```

**Example Response**:
```json
{
  "taskId": "123e4567-e89b-12d3-a456-426614174000",
  "success": true,
  "status": "Completed",
  "stepResults": [...],
  "durationMs": 8000,
  "metadata": {
    "healing_applied": true,
    "original_plan_id": "plan-abc123",
    "refined_plan_id": "plan-xyz789"
  }
}
```

**Healing Process**:
1. Retrieves last failed execution
2. Analyzes failure reasons
3. Generates refined execution plan
4. Retries with healed plan
5. Returns new execution result

---

### 7. Cancel Execution

Cancels a currently running task execution.

**Endpoint**: `POST /api/tasks/{id}/execute/cancel`

**Parameters**:
- `id` (path, required): Task ID (GUID)

**Response**: `204 No Content` - Successfully cancelled

**Example Request**:
```bash
curl -X POST "https://localhost:7001/api/tasks/123e4567-e89b-12d3-a456-426614174000/execute/cancel"
```

**Error Responses**:
- `404 Not Found` - No active execution found for this task

---

## Data Models

### AgentTaskResult

```csharp
{
  "taskId": "string",
  "success": boolean,
  "status": "Pending|Planning|Executing|Paused|Completed|Failed|Cancelled",
  "stepResults": [StepResult],
  "extractedData": { ... },
  "error": Exception?,
  "errorMessage": "string?",
  "screenshots": ["string"],
  "statistics": ExecutionStatistics,
  "metadata": { ... },
  "startedAt": "DateTimeOffset",
  "completedAt": "DateTimeOffset",
  "durationMs": long
}
```

### ExecutionStatistics

```csharp
{
  "totalSteps": int,
  "successfulSteps": int,
  "failedSteps": int,
  "retriedSteps": int,
  "healedSteps": int,
  "totalRetries": int,
  "averageStepDurationMs": double,
  "totalWaitTimeMs": long,
  "successRate": double
}
```

---

## Usage Examples

### Execute a Login Task

```csharp
// 1. Create a task (using Task CRUD API)
var taskRequest = new CreateTaskRequest
{
    Name = "Login to Example.com",
    Description = "Automated login workflow",
    NaturalLanguagePrompt = "Login to example.com with username test@example.com and password SecurePass123"
};

var task = await httpClient.PostAsJsonAsync("/api/tasks", taskRequest);
var taskId = (await task.Content.ReadFromJsonAsync<TaskResponse>()).Id;

// 2. Execute the task
var executeResponse = await httpClient.PostAsync($"/api/tasks/{taskId}/execute", null);
var result = await executeResponse.Content.ReadFromJsonAsync<AgentTaskResult>();

if (result.Success)
{
    Console.WriteLine($"Task completed successfully in {result.DurationMs}ms");
    Console.WriteLine($"Completed {result.Statistics.SuccessfulSteps}/{result.Statistics.TotalSteps} steps");
}
else
{
    Console.WriteLine($"Task failed: {result.ErrorMessage}");
    
    // Try healing
    var healResponse = await httpClient.PostAsync($"/api/tasks/{taskId}/execute/heal", null);
    var healedResult = await healResponse.Content.ReadFromJsonAsync<AgentTaskResult>();
}
```

### Background Execution with Status Polling

```csharp
// 1. Start background execution
var response = await httpClient.PostAsync($"/api/tasks/{taskId}/execute/background", null);
var backgroundExec = await response.Content.ReadFromJsonAsync<BackgroundExecutionResponse>();

Console.WriteLine($"Execution started: {backgroundExec.ExecutionId}");

// 2. Poll for status
while (true)
{
    var statusResponse = await httpClient.GetAsync(backgroundExec.StatusUrl);
    var status = await statusResponse.Content.ReadFromJsonAsync<ExecutionStatus>();
    
    Console.WriteLine($"Status: {status.Status}");
    
    if (status.Status == "Completed" || status.Status == "Failed")
    {
        Console.WriteLine($"Execution finished. Success: {status.Success}");
        break;
    }
    
    await Task.Delay(1000); // Poll every second
}

// 3. Get detailed results
var detailsResponse = await httpClient.GetAsync($"/api/executions/{backgroundExec.ExecutionId}");
var details = await detailsResponse.Content.ReadFromJsonAsync<ExecutionDetailsDto>();
```

---

## Error Handling

All endpoints return standard problem details for errors:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Plan Generation Failed",
  "status": 500,
  "detail": "Failed to generate execution plan: LLM service unavailable"
}
```

### Common Error Scenarios

1. **Plan Generation Failure**
   - Status: `500 Internal Server Error`
   - Cause: LLM provider unavailable or invalid response
   - Solution: Check LLM provider configuration and availability

2. **Plan Validation Failure**
   - Status: `400 Bad Request`
   - Cause: Generated plan doesn't meet validation rules
   - Solution: Review task description, may need more specific instructions

3. **Execution Timeout**
   - Status: `500 Internal Server Error`
   - Cause: Step exceeded timeout limit
   - Solution: Increase timeout in step configuration or optimize automation

4. **Element Not Found**
   - Status: `200 OK` (stepResults contain failure)
   - Cause: Browser element selector didn't match any elements
   - Solution: Use healing endpoint to try alternative selectors

---

## Configuration

### appsettings.json

```json
{
  "EvoAITest": {
    "Core": {
      "LLMProvider": "AzureOpenAI",
      "AzureOpenAIEndpoint": "https://your-endpoint.openai.azure.com",
      "AzureOpenAIApiKey": "your-api-key",
      "AzureOpenAIDeployment": "gpt-5"
    },
    "ToolExecutor": {
      "MaxRetries": 3,
      "InitialRetryDelayMs": 500,
      "UseExponentialBackoff": true,
      "TimeoutPerToolMs": 30000
    }
  }
}
```

---

## Best Practices

1. **Use Background Execution for Long Tasks**
   - Tasks with >10 steps or complex workflows
   - When you need to respond immediately to the client
   - For batch processing scenarios

2. **Implement Timeout Handling**
   - Set appropriate timeouts based on task complexity
   - Use cancellation tokens to stop hung executions
   - Monitor execution history to tune timeout values

3. **Leverage Healing for Resilience**
   - Use healing endpoint when initial execution fails
   - Review healing metadata to understand recovery strategies
   - Track healing success rate for quality monitoring

4. **Monitor Execution History**
   - Track success rates over time
   - Identify patterns in failures
   - Use statistics to optimize automation

5. **Handle Screenshots**
   - Screenshots are returned as base64 strings
   - Store externally for large-scale usage
   - Use for debugging and audit trails

---

## OpenAPI / Swagger

All endpoints are documented in OpenAPI format and available at:

```
https://localhost:7001/openapi/v1.json
```

In development, access the Swagger UI at:

```
https://localhost:7001/swagger
```

---

## Status: ? Complete

All execution endpoints are implemented and ready for use with comprehensive error handling, telemetry, and documentation.

**Related Documentation**:
- [Task CRUD API](TaskEndpoints_README.md)
- [PlannerAgent Guide](../EvoAITest.Agents/Agents/PlannerAgent_README.md)
- [ExecutorAgent Guide](../EvoAITest.Agents/Agents/ExecutorAgent_README.md)
- [HealerAgent Guide](../EvoAITest.Agents/Agents/HealerAgent_README.md)
