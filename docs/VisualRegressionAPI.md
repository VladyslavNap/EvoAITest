# Visual Regression Testing - API Documentation

## Table of Contents

1. [Overview](#overview)
2. [Authentication](#authentication)
3. [Endpoints](#endpoints)
4. [Data Models](#data-models)
5. [Error Handling](#error-handling)
6. [Examples](#examples)
7. [Rate Limiting](#rate-limiting)

---

## Overview

### Base URL

```
https://localhost:7001/api/visual
```

**Production:**
```
https://api.evoaitest.com/visual
```

### Content Type

All requests and responses use `application/json` unless otherwise specified.

### Versioning

API Version: `v1.0`

Future versions will be accessible via:
```
https://api.evoaitest.com/v2/visual
```

### OpenAPI/Swagger

Interactive API documentation available at:
```
https://localhost:7001/swagger
```

---

## Authentication

### Development

Authentication is optional in development mode. Requests without authentication use "anonymous-user".

### Production

**Required:** JWT Bearer token

**Headers:**
```
Authorization: Bearer {your-jwt-token}
```

**Obtaining a Token:**

POST `/api/auth/login`
```json
{
  "username": "user@example.com",
  "password": "your-password"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 3600
}
```

**Using the Token:**
```bash
curl -H "Authorization: Bearer eyJhbGciOiJIUzI1NiIs..." \
  https://api.evoaitest.com/visual/tasks/{taskId}/checkpoints
```

---

## Endpoints

### 1. Get Task Checkpoints

Retrieve all visual checkpoints for a task with their baseline status.

**Endpoint:**
```
GET /api/visual/tasks/{taskId}/checkpoints
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| taskId | GUID | Yes | Unique task identifier |

**Response: 200 OK**
```json
{
  "taskId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "checkpoints": [
    {
      "name": "HomePage_Header",
      "type": "Element",
      "tolerance": 0.01,
      "hasBaseline": true,
      "latestComparison": {
        "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
        "taskId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "checkpointName": "HomePage_Header",
        "passed": true,
        "differencePercentage": 0.0032,
        "tolerance": 0.01,
        "pixelsDifferent": 1024,
        "totalPixels": 2073600,
        "ssimScore": 0.9987,
        "differenceType": "Minor",
        "baselineUrl": "/api/visual/images/baselines/...",
        "actualUrl": "/api/visual/images/actual/...",
        "diffUrl": "/api/visual/images/diff/...",
        "regions": [],
        "comparedAt": "2025-12-07T10:30:00Z"
      }
    }
  ]
}
```

**Response: 404 Not Found**
```json
{
  "error": "Task not found"
}
```

**Example:**
```bash
curl -X GET https://localhost:7001/api/visual/tasks/3fa85f64-5717-4562-b3fc-2c963f66afa6/checkpoints
```

---

### 2. Get Comparison History

Retrieve paginated comparison history for a specific checkpoint.

**Endpoint:**
```
GET /api/visual/tasks/{taskId}/checkpoints/{checkpointName}/history
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| taskId | GUID | Yes | Unique task identifier |
| checkpointName | string | Yes | Checkpoint name |

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| page | int | No | 1 | Page number (1-based) |
| pageSize | int | No | 20 | Results per page (1-100) |

**Response: 200 OK**
```json
{
  "comparisons": [
    {
      "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
      "taskId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "checkpointName": "HomePage_Header",
      "passed": false,
      "differencePercentage": 0.0452,
      "tolerance": 0.01,
      "pixelsDifferent": 9824,
      "totalPixels": 2073600,
      "ssimScore": 0.9876,
      "differenceType": "ContentChange",
      "baselineUrl": "/api/visual/images/baselines/task-abc/HomePage_Header.png",
      "actualUrl": "/api/visual/images/actual/task-abc/HomePage_Header_2025-12-07.png",
      "diffUrl": "/api/visual/images/diff/task-abc/HomePage_Header_diff.png",
      "regions": [
        {
          "x": 100,
          "y": 200,
          "width": 150,
          "height": 80,
          "differenceScore": 0.65
        }
      ],
      "comparedAt": "2025-12-07T10:30:00Z"
    }
  ],
  "totalCount": 45,
  "pageSize": 20,
  "currentPage": 1,
  "hasNextPage": true
}
```

**Example:**
```bash
curl -X GET "https://localhost:7001/api/visual/tasks/3fa85f64-5717-4562-b3fc-2c963f66afa6/checkpoints/HomePage_Header/history?page=1&pageSize=10"
```

---

### 3. Get Comparison Details

Retrieve detailed information about a specific comparison.

**Endpoint:**
```
GET /api/visual/comparisons/{comparisonId}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| comparisonId | GUID | Yes | Unique comparison identifier |

**Response: 200 OK**
```json
{
  "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "taskId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "checkpointName": "HomePage_Header",
  "passed": false,
  "differencePercentage": 0.0452,
  "tolerance": 0.01,
  "pixelsDifferent": 9824,
  "totalPixels": 2073600,
  "ssimScore": 0.9876,
  "differenceType": "ContentChange",
  "baselineUrl": "/api/visual/images/baselines/...",
  "actualUrl": "/api/visual/images/actual/...",
  "diffUrl": "/api/visual/images/diff/...",
  "regions": [
    {
      "x": 100,
      "y": 200,
      "width": 150,
      "height": 80,
      "differenceScore": 0.65
    }
  ],
  "comparedAt": "2025-12-07T10:30:00Z"
}
```

**Response: 404 Not Found**
```json
{
  "error": "Comparison not found"
}
```

**Example:**
```bash
curl -X GET https://localhost:7001/api/visual/comparisons/7c9e6679-7425-40de-944b-e07fc1f90ae7
```

---

### 4. Get Baseline

Retrieve the current baseline for a checkpoint configuration.

**Endpoint:**
```
GET /api/visual/tasks/{taskId}/checkpoints/{checkpointName}/baseline
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| taskId | GUID | Yes | Unique task identifier |
| checkpointName | string | Yes | Checkpoint name |

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| environment | string | No | "dev" | Environment (dev, staging, prod) |
| browser | string | No | "chromium" | Browser (chromium, firefox, webkit) |
| viewport | string | No | "1920x1080" | Viewport size |

**Response: 200 OK**
```json
{
  "id": "2b7a8c9d-3e4f-5a6b-7c8d-9e0f1a2b3c4d",
  "taskId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "checkpointName": "HomePage_Header",
  "environment": "dev",
  "browser": "chromium",
  "viewport": "1920x1080",
  "baselineUrl": "/api/visual/images/baselines/...",
  "imageHash": "sha256:a1b2c3d4e5f6...",
  "createdAt": "2025-12-01T08:00:00Z",
  "approvedBy": "john.doe@company.com",
  "gitCommit": "abc123def456",
  "gitBranch": "main"
}
```

**Response: 404 Not Found**
```json
{
  "error": "Baseline not found"
}
```

**Example:**
```bash
curl -X GET "https://localhost:7001/api/visual/tasks/3fa85f64-5717-4562-b3fc-2c963f66afa6/checkpoints/HomePage_Header/baseline?environment=prod&browser=chromium&viewport=1920x1080"
```

---

### 5. Update Tolerance

Update the tolerance for a checkpoint.

**Endpoint:**
```
PUT /api/visual/tasks/{taskId}/checkpoints/{checkpointName}/tolerance
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| taskId | GUID | Yes | Unique task identifier |
| checkpointName | string | Yes | Checkpoint name |

**Request Body:**
```json
{
  "newTolerance": 0.02,
  "applyToAll": false,
  "reason": "Increased due to font rendering differences across environments"
}
```

**Request Body Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| newTolerance | double | Yes | New tolerance (0.0 - 1.0) |
| applyToAll | boolean | No | Apply to all checkpoints in task |
| reason | string | No | Reason for change (audit trail) |

**Response: 200 OK**
```json
{
  "success": true,
  "message": "Tolerance updated successfully"
}
```

**Response: 400 Bad Request**
```json
{
  "error": "Tolerance must be between 0.0 and 1.0"
}
```

**Response: 404 Not Found**
```json
{
  "error": "Checkpoint not found"
}
```

**Example:**
```bash
curl -X PUT https://localhost:7001/api/visual/tasks/3fa85f64-5717-4562-b3fc-2c963f66afa6/checkpoints/HomePage_Header/tolerance \
  -H "Content-Type: application/json" \
  -d '{
    "newTolerance": 0.02,
    "applyToAll": false,
    "reason": "Font rendering differences"
  }'
```

---

### 6. Get Image

Serve an image file (baseline, actual, or diff).

**Endpoint:**
```
GET /api/visual/images/{imagePath}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| imagePath | string | Yes | Relative path to image (catch-all) |

**Response: 200 OK**

Binary image data with appropriate Content-Type:
- `image/png`
- `image/jpeg`
- `image/gif`
- `image/webp`

**Response: 404 Not Found**

Empty response if image not found.

**Security:**
- Path sanitization prevents directory traversal
- `..` sequences removed
- Only files in configured storage directory served

**Examples:**
```bash
# Get baseline image
curl https://localhost:7001/api/visual/images/baselines/task-abc/HomePage_Header.png \
  -o baseline.png

# Get actual screenshot
curl https://localhost:7001/api/visual/images/actual/task-abc/HomePage_Header_2025-12-07.png \
  -o actual.png

# Get diff image
curl https://localhost:7001/api/visual/images/diff/task-abc/HomePage_Header_diff.png \
  -o diff.png
```

---

### 7. Get Failed Comparisons

Retrieve failed comparisons for a task.

**Endpoint:**
```
GET /api/visual/tasks/{taskId}/failures
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| taskId | GUID | Yes | Unique task identifier |

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| limit | int | No | 50 | Maximum results (1-100) |

**Response: 200 OK**
```json
[
  {
    "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "taskId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "checkpointName": "HomePage_Header",
    "passed": false,
    "differencePercentage": 0.0452,
    "tolerance": 0.01,
    "pixelsDifferent": 9824,
    "totalPixels": 2073600,
    "ssimScore": 0.9876,
    "differenceType": "ContentChange",
    "baselineUrl": "/api/visual/images/baselines/...",
    "actualUrl": "/api/visual/images/actual/...",
    "diffUrl": "/api/visual/images/diff/...",
    "regions": [...],
    "comparedAt": "2025-12-07T10:30:00Z"
  }
]
```

**Example:**
```bash
curl -X GET "https://localhost:7001/api/visual/tasks/3fa85f64-5717-4562-b3fc-2c963f66afa6/failures?limit=20"
```

---

## Data Models

### VisualComparisonDto

Complete comparison result with metrics and image URLs.

```typescript
interface VisualComparisonDto {
  id: string;                    // GUID
  taskId: string;                // GUID
  checkpointName: string;        // e.g., "HomePage_Header"
  passed: boolean;               // true if within tolerance
  differencePercentage: number;  // 0.0 - 1.0 (e.g., 0.0452 = 4.52%)
  tolerance: number;             // Configured tolerance
  pixelsDifferent: number;       // Absolute count
  totalPixels: number;           // Total image pixels
  ssimScore: number | null;      // 0.0 - 1.0, null if not calculated
  differenceType: string | null; // "Minor", "ContentChange", etc.
  baselineUrl: string;           // Relative URL to baseline image
  actualUrl: string;             // Relative URL to actual screenshot
  diffUrl: string;               // Relative URL to diff image
  regions: DifferenceRegionDto[]; // Detected difference regions
  comparedAt: string;            // ISO 8601 timestamp
}
```

### DifferenceRegionDto

Individual difference region with coordinates.

```typescript
interface DifferenceRegionDto {
  x: number;              // X coordinate (pixels)
  y: number;              // Y coordinate (pixels)
  width: number;          // Width (pixels)
  height: number;         // Height (pixels)
  differenceScore: number; // 0.0 - 1.0, intensity of difference
}
```

### VisualBaselineDto

Baseline metadata with image information.

```typescript
interface VisualBaselineDto {
  id: string;                // GUID
  taskId: string;            // GUID
  checkpointName: string;    // e.g., "HomePage_Header"
  environment: string;       // "dev", "staging", "prod"
  browser: string;           // "chromium", "firefox", "webkit"
  viewport: string;          // e.g., "1920x1080"
  baselineUrl: string;       // Relative URL to baseline image
  imageHash: string;         // SHA256 hash for integrity
  createdAt: string;         // ISO 8601 timestamp
  approvedBy: string;        // User email who approved
  gitCommit: string | null;  // Git commit hash
  gitBranch: string | null;  // Git branch name
}
```

### UpdateToleranceRequest

Request to update checkpoint tolerance.

```typescript
interface UpdateToleranceRequest {
  newTolerance: number;  // 0.0 - 1.0
  applyToAll: boolean;   // Apply to all checkpoints in task
  reason?: string;       // Optional reason for audit
}
```

### ComparisonHistoryResponse

Paginated list of comparisons.

```typescript
interface ComparisonHistoryResponse {
  comparisons: VisualComparisonDto[];  // Page of results
  totalCount: number;                   // Total matching records
  pageSize: number;                     // Results per page
  currentPage: number;                  // Current page (1-based)
  hasNextPage: boolean;                 // More pages available
}
```

### TaskCheckpointsResponse

List of checkpoints with status.

```typescript
interface TaskCheckpointsResponse {
  taskId: string;                       // GUID
  checkpoints: CheckpointSummaryDto[];  // Checkpoint summaries
}
```

### CheckpointSummaryDto

Checkpoint summary with baseline status.

```typescript
interface CheckpointSummaryDto {
  name: string;                          // Checkpoint name
  type: string;                          // "FullPage", "Viewport", "Element", "Region"
  tolerance: number;                     // Current tolerance
  hasBaseline: boolean;                  // Baseline exists
  latestComparison: VisualComparisonDto | null;  // Latest comparison result
}
```

---

## Error Handling

### HTTP Status Codes

| Code | Meaning | Description |
|------|---------|-------------|
| 200 | OK | Request successful |
| 400 | Bad Request | Invalid parameters or request body |
| 401 | Unauthorized | Missing or invalid authentication |
| 403 | Forbidden | Insufficient permissions |
| 404 | Not Found | Resource not found |
| 429 | Too Many Requests | Rate limit exceeded |
| 500 | Internal Server Error | Server error |
| 503 | Service Unavailable | Service temporarily unavailable |

### Error Response Format

All error responses follow this structure:

```json
{
  "error": "Human-readable error message",
  "details": "Optional detailed information",
  "errorCode": "OPTIONAL_ERROR_CODE",
  "timestamp": "2025-12-07T10:30:00Z"
}
```

### Common Errors

#### 400 Bad Request

**Causes:**
- Invalid GUID format
- Tolerance out of range (0.0 - 1.0)
- Invalid page/pageSize
- Missing required fields

**Example:**
```json
{
  "error": "Tolerance must be between 0.0 and 1.0",
  "details": "Provided value: 1.5"
}
```

#### 404 Not Found

**Causes:**
- Task doesn't exist
- Checkpoint doesn't exist
- Comparison doesn't exist
- Baseline doesn't exist
- Image file not found

**Example:**
```json
{
  "error": "Task not found",
  "details": "TaskId: 3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

#### 500 Internal Server Error

**Causes:**
- Database connection failure
- File system errors
- Unexpected exceptions

**Example:**
```json
{
  "error": "Failed to update tolerance",
  "details": "Database connection timeout"
}
```

### Retry Logic

For 5xx errors, implement exponential backoff:

```javascript
async function retryRequest(url, maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    try {
      const response = await fetch(url);
      if (response.ok) return response;
      if (response.status < 500) throw new Error(`HTTP ${response.status}`);
    } catch (error) {
      if (i === maxRetries - 1) throw error;
      await new Promise(resolve => setTimeout(resolve, 1000 * Math.pow(2, i)));
    }
  }
}
```

---

## Examples

### Complete Workflow Example (JavaScript)

```javascript
const API_BASE = 'https://localhost:7001/api/visual';

// 1. Get checkpoints for a task
async function getCheckpoints(taskId) {
  const response = await fetch(`${API_BASE}/tasks/${taskId}/checkpoints`);
  return await response.json();
}

// 2. Get comparison history
async function getHistory(taskId, checkpointName, page = 1) {
  const response = await fetch(
    `${API_BASE}/tasks/${taskId}/checkpoints/${checkpointName}/history?page=${page}&pageSize=20`
  );
  return await response.json();
}

// 3. Get comparison details
async function getComparison(comparisonId) {
  const response = await fetch(`${API_BASE}/comparisons/${comparisonId}`);
  return await response.json();
}

// 4. Update tolerance
async function updateTolerance(taskId, checkpointName, newTolerance) {
  const response = await fetch(
    `${API_BASE}/tasks/${taskId}/checkpoints/${checkpointName}/tolerance`,
    {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        newTolerance,
        applyToAll: false,
        reason: 'Adjusted via API'
      })
    }
  );
  return await response.json();
}

// 5. Get failed comparisons
async function getFailures(taskId) {
  const response = await fetch(`${API_BASE}/tasks/${taskId}/failures?limit=50`);
  return await response.json();
}

// Usage
const taskId = '3fa85f64-5717-4562-b3fc-2c963f66afa6';

const checkpoints = await getCheckpoints(taskId);
console.log('Checkpoints:', checkpoints);

const history = await getHistory(taskId, 'HomePage_Header');
console.log('History:', history);

const failures = await getFailures(taskId);
console.log('Failures:', failures);
```

### Python Example

```python
import requests
from typing import Dict, List

API_BASE = 'https://localhost:7001/api/visual'

class VisualRegressionClient:
    def __init__(self, base_url: str = API_BASE):
        self.base_url = base_url
        
    def get_checkpoints(self, task_id: str) -> Dict:
        """Get all checkpoints for a task"""
        url = f"{self.base_url}/tasks/{task_id}/checkpoints"
        response = requests.get(url)
        response.raise_for_status()
        return response.json()
    
    def get_history(self, task_id: str, checkpoint_name: str, 
                    page: int = 1, page_size: int = 20) -> Dict:
        """Get comparison history"""
        url = f"{self.base_url}/tasks/{task_id}/checkpoints/{checkpoint_name}/history"
        params = {'page': page, 'pageSize': page_size}
        response = requests.get(url, params=params)
        response.raise_for_status()
        return response.json()
    
    def update_tolerance(self, task_id: str, checkpoint_name: str, 
                        new_tolerance: float, apply_to_all: bool = False,
                        reason: str = None) -> Dict:
        """Update checkpoint tolerance"""
        url = f"{self.base_url}/tasks/{task_id}/checkpoints/{checkpoint_name}/tolerance"
        payload = {
            'newTolerance': new_tolerance,
            'applyToAll': apply_to_all
        }
        if reason:
            payload['reason'] = reason
        
        response = requests.put(url, json=payload)
        response.raise_for_status()
        return response.json()
    
    def download_image(self, image_path: str, output_file: str):
        """Download an image file"""
        url = f"{self.base_url}/images/{image_path}"
        response = requests.get(url)
        response.raise_for_status()
        
        with open(output_file, 'wb') as f:
            f.write(response.content)

# Usage
client = VisualRegressionClient()
task_id = '3fa85f64-5717-4562-b3fc-2c963f66afa6'

checkpoints = client.get_checkpoints(task_id)
print(f"Found {len(checkpoints['checkpoints'])} checkpoints")

# Download diff image
client.download_image(
    'diff/task-abc/HomePage_Header_diff.png',
    'local_diff.png'
)
```

### C# Example

```csharp
using System.Net.Http.Json;

public class VisualRegressionClient
{
    private readonly HttpClient _httpClient;
    private const string ApiBase = "https://localhost:7001/api/visual";

    public VisualRegressionClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TaskCheckpointsResponse> GetCheckpointsAsync(Guid taskId)
    {
        var response = await _httpClient.GetAsync($"{ApiBase}/tasks/{taskId}/checkpoints");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskCheckpointsResponse>();
    }

    public async Task<ComparisonHistoryResponse> GetHistoryAsync(
        Guid taskId, string checkpointName, int page = 1, int pageSize = 20)
    {
        var url = $"{ApiBase}/tasks/{taskId}/checkpoints/{checkpointName}/history?page={page}&pageSize={pageSize}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComparisonHistoryResponse>();
    }

    public async Task UpdateToleranceAsync(
        Guid taskId, string checkpointName, double newTolerance, 
        bool applyToAll = false, string reason = null)
    {
        var request = new UpdateToleranceRequest
        {
            NewTolerance = newTolerance,
            ApplyToAll = applyToAll,
            Reason = reason
        };

        var url = $"{ApiBase}/tasks/{taskId}/checkpoints/{checkpointName}/tolerance";
        var response = await _httpClient.PutAsJsonAsync(url, request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> DownloadImageAsync(string imagePath)
    {
        var url = $"{ApiBase}/images/{imagePath}";
        return await _httpClient.GetByteArrayAsync(url);
    }
}

// Usage
var client = new VisualRegressionClient(new HttpClient());
var taskId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

var checkpoints = await client.GetCheckpointsAsync(taskId);
Console.WriteLine($"Found {checkpoints.Checkpoints.Count} checkpoints");

await client.UpdateToleranceAsync(taskId, "HomePage_Header", 0.02, false, "Font rendering");
```

---

## Rate Limiting

### Development

No rate limiting in development mode.

### Production

**Limits:**
- 100 requests per minute per IP
- 1,000 requests per hour per user
- 10,000 requests per day per organization

**Headers:**

Response includes rate limit information:
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1638360000
```

**429 Response:**
```json
{
  "error": "Rate limit exceeded",
  "details": "100 requests per minute allowed",
  "retryAfter": 42
}
```

**Best Practices:**
- Cache responses when possible
- Use pagination to reduce request count
- Implement exponential backoff
- Monitor `X-RateLimit-Remaining` header

---

## Appendix

### SDK Libraries

Official SDKs available:
- JavaScript/TypeScript: `@evoaitest/visual-regression-js`
- Python: `evoaitest-visual-regression`
- C#: `EvoAITest.VisualRegression.Client`

### Postman Collection

Import collection: [VisualRegression.postman_collection.json](VisualRegression.postman_collection.json)

### OpenAPI Specification

Download spec: `https://localhost:7001/swagger/v1/swagger.json`

### Support

- API Issues: [GitHub Issues](https://github.com/YourOrg/EvoAITest/issues)
- Documentation: [docs.evoaitest.com](https://docs.evoaitest.com)
- Email: api-support@evoaitest.com

---

**Version:** 1.0  
**Last Updated:** 2025-12-07  
**Changelog:** [CHANGELOG.md](CHANGELOG.md)
