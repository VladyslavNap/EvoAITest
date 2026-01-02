# Test Generation from Recordings - API Reference

## Overview

This document provides a complete reference for the Recording API endpoints. All endpoints are RESTful and return JSON responses.

## Base URL

```
https://localhost:5001/api/recordings
```

## Authentication

Currently, the API operates without authentication in development mode. For production deployments, implement JWT Bearer authentication.

## Common Response Codes

| Code | Description |
|------|-------------|
| 200 | Success |
| 201 | Created successfully |
| 204 | No content (successful deletion) |
| 400 | Bad request (validation error) |
| 404 | Resource not found |
| 500 | Internal server error |

---

## Endpoints

### 1. Start Recording

Start a new recording session.

**Endpoint**: `POST /api/recordings/start`

**Request Body**:
```json
{
  "name": "string",                    // Required: Name of the test
  "startUrl": "string",                // Required: Starting URL
  "description": "string",             // Optional: Test description
  "captureScreenshots": true,          // Optional: Default true
  "recordNetwork": false,              // Optional: Default false
  "recordConsoleLogs": false,          // Optional: Default false
  "autoGenerateAssertions": true,      // Optional: Default true
  "useAiIntentDetection": true         // Optional: Default true
}
```

**Response**: `201 Created`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Login Flow Test",
  "description": "Test user login functionality",
  "status": "Recording",
  "startedAt": "2024-12-20T10:30:00Z",
  "endedAt": null,
  "startUrl": "https://example.com",
  "browser": "chromium",
  "viewportSize": {
    "width": 1280,
    "height": 720
  },
  "interactions": [],
  "configuration": {
    "captureScreenshots": true,
    "recordNetwork": false,
    "autoGenerateAssertions": true
  },
  "metrics": {
    "totalInteractions": 0,
    "averageIntentConfidence": 0,
    "actionRecognitionAccuracy": 0
  }
}
```

**Example**:
```bash
curl -X POST "https://localhost:5001/api/recordings/start" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Login Flow Test",
    "startUrl": "https://example.com/login"
  }'
```

---

### 2. Stop Recording

Stop an active recording session.

**Endpoint**: `POST /api/recordings/{id}/stop`

**Path Parameters**:
- `id` (guid): Recording session ID

**Response**: `200 OK`
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Login Flow Test",
  "status": "Stopped",
  "startedAt": "2024-12-20T10:30:00Z",
  "endedAt": "2024-12-20T10:35:00Z",
  "interactions": [
    {
      "id": "guid",
      "sequenceNumber": 1,
      "actionType": "Navigation",
      "timestamp": "2024-12-20T10:30:01Z",
      "context": {
        "url": "https://example.com/login",
        "pageTitle": "Login"
      }
    }
  ],
  "metrics": {
    "totalInteractions": 15,
    "durationMs": 300000
  }
}
```

**Example**:
```bash
curl -X POST "https://localhost:5001/api/recordings/3fa85f64-5717-4562-b3fc-2c963f66afa6/stop"
```

---

### 3. Pause Recording

Temporarily pause an active recording.

**Endpoint**: `POST /api/recordings/{id}/pause`

**Path Parameters**:
- `id` (guid): Recording session ID

**Response**: `204 No Content`

**Example**:
```bash
curl -X POST "https://localhost:5001/api/recordings/3fa85f64-5717-4562-b3fc-2c963f66afa6/pause"
```

---

### 4. Resume Recording

Resume a paused recording session.

**Endpoint**: `POST /api/recordings/{id}/resume`

**Path Parameters**:
- `id` (guid): Recording session ID

**Response**: `204 No Content`

**Example**:
```bash
curl -X POST "https://localhost:5001/api/recordings/3fa85f64-5717-4562-b3fc-2c963f66afa6/resume"
```

---

### 5. Get All Recordings

Retrieve all recording sessions with optional status filtering.

**Endpoint**: `GET /api/recordings`

**Query Parameters**:
- `status` (string, optional): Filter by status (Recording, Paused, Stopped, Failed, Generated)

**Response**: `200 OK`
```json
[
  {
    "id": "guid",
    "name": "string",
    "status": "string",
    "startedAt": "datetime",
    "endedAt": "datetime",
    "metrics": {
      "totalInteractions": 0,
      "averageIntentConfidence": 0
    }
  }
]
```

**Examples**:
```bash
# Get all recordings
curl "https://localhost:5001/api/recordings"

# Get only stopped recordings
curl "https://localhost:5001/api/recordings?status=Stopped"
```

---

### 6. Get Recording by ID

Retrieve a specific recording session with all details.

**Endpoint**: `GET /api/recordings/{id}`

**Path Parameters**:
- `id` (guid): Recording session ID

**Response**: `200 OK`
```json
{
  "id": "guid",
  "name": "Login Flow Test",
  "description": "Test user login",
  "status": "Stopped",
  "startedAt": "2024-12-20T10:30:00Z",
  "endedAt": "2024-12-20T10:35:00Z",
  "interactions": [
    {
      "id": "guid",
      "sequenceNumber": 1,
      "actionType": "Navigation",
      "intent": "Navigation",
      "intentConfidence": 0.95,
      "description": "Navigated to login page",
      "timestamp": "2024-12-20T10:30:01Z",
      "context": {
        "url": "https://example.com/login",
        "pageTitle": "Login",
        "targetSelector": "body",
        "elementTag": "body"
      }
    }
  ],
  "configuration": {},
  "metrics": {
    "totalInteractions": 15,
    "durationMs": 300000,
    "averageIntentConfidence": 0.89
  }
}
```

**Example**:
```bash
curl "https://localhost:5001/api/recordings/3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

### 7. Get Interactions

Retrieve all interactions for a recording session.

**Endpoint**: `GET /api/recordings/{id}/interactions`

**Path Parameters**:
- `id` (guid): Recording session ID

**Response**: `200 OK`
```json
[
  {
    "id": "guid",
    "sessionId": "guid",
    "sequenceNumber": 1,
    "actionType": "Click",
    "intent": "Authentication",
    "intentConfidence": 0.92,
    "description": "Clicked login button",
    "timestamp": "2024-12-20T10:30:05Z",
    "inputValue": null,
    "context": {
      "url": "https://example.com/login",
      "elementText": "Login",
      "elementTag": "button",
      "targetSelector": "#login-btn"
    },
    "includeInTest": true,
    "generatedCode": "await _page!.Locator(\"#login-btn\").ClickAsync();"
  }
]
```

**Example**:
```bash
curl "https://localhost:5001/api/recordings/3fa85f64-5717-4562-b3fc-2c963f66afa6/interactions"
```

---

### 8. Analyze Recording

Analyze a recording session using AI to detect intent and generate descriptions.

**Endpoint**: `POST /api/recordings/{id}/analyze`

**Path Parameters**:
- `id` (guid): Recording session ID

**Response**: `200 OK`
```json
{
  "id": "guid",
  "interactions": [
    {
      "id": "guid",
      "intent": "Authentication",
      "intentConfidence": 0.95,
      "description": "User entered username in login form"
    }
  ],
  "metrics": {
    "averageIntentConfidence": 0.89,
    "actionRecognitionAccuracy": 92.5
  }
}
```

**Example**:
```bash
curl -X POST "https://localhost:5001/api/recordings/3fa85f64-5717-4562-b3fc-2c963f66afa6/analyze"
```

---

### 9. Generate Test

Generate test code from a recording session.

**Endpoint**: `POST /api/recordings/{id}/generate`

**Path Parameters**:
- `id` (guid): Recording session ID

**Request Body**:
```json
{
  "testFramework": "xUnit",          // xUnit, NUnit, or MSTest
  "language": "C#",                   // Currently only C#
  "includeComments": true,            // Include descriptive comments
  "generatePageObjects": false,       // Generate Page Object Model classes
  "autoGenerateAssertions": true,     // Auto-generate assertions
  "namespace": "MyTests.Generated",   // Namespace for generated code
  "className": null                   // Optional: Custom class name
}
```

**Response**: `200 OK`
```json
{
  "sessionId": "guid",
  "code": "using System;\nusing Xunit;\n...",
  "className": "LoginFlowTests",
  "namespace": "MyTests.Generated",
  "methods": [
    {
      "name": "LoginFlowTest",
      "code": "public async Task LoginFlowTest() { ... }",
      "description": "Test user login functionality"
    }
  ],
  "pageObjects": [],
  "imports": [
    "System",
    "System.Threading.Tasks",
    "Microsoft.Playwright",
    "Xunit"
  ],
  "metrics": {
    "testMethodCount": 1,
    "assertionCount": 5,
    "linesOfCode": 120,
    "estimatedCoverage": 85.5,
    "maintainabilityScore": 87.3
  }
}
```

**Example**:
```bash
curl -X POST "https://localhost:5001/api/recordings/3fa85f64-5717-4562-b3fc-2c963f66afa6/generate" \
  -H "Content-Type: application/json" \
  -d '{
    "testFramework": "xUnit",
    "includeComments": true,
    "autoGenerateAssertions": true
  }'
```

---

### 10. Validate Accuracy

Validate the accuracy of action recognition in a recording.

**Endpoint**: `POST /api/recordings/{id}/validate`

**Path Parameters**:
- `id` (guid): Recording session ID

**Response**: `200 OK`
```json
{
  "accuracyPercentage": 92.5,
  "correctRecognitions": 37,
  "totalActions": 40,
  "averageConfidence": 0.88,
  "lowConfidenceActions": [
    {
      "id": "guid",
      "actionType": "Click",
      "intentConfidence": 0.65
    }
  ],
  "accuracyByActionType": {
    "Click": 95.0,
    "Input": 90.0,
    "Navigation": 100.0,
    "Select": 85.0
  }
}
```

**Example**:
```bash
curl -X POST "https://localhost:5001/api/recordings/3fa85f64-5717-4562-b3fc-2c963f66afa6/validate"
```

---

### 11. Delete Recording

Delete a recording session and all its interactions.

**Endpoint**: `DELETE /api/recordings/{id}`

**Path Parameters**:
- `id` (guid): Recording session ID

**Response**: `204 No Content`

**Example**:
```bash
curl -X DELETE "https://localhost:5001/api/recordings/3fa85f64-5717-4562-b3fc-2c963f66afa6"
```

---

### 12. Get Recent Recordings

Retrieve the most recent recording sessions.

**Endpoint**: `GET /api/recordings/recent`

**Query Parameters**:
- `count` (integer, optional): Number of sessions to retrieve (default: 10)

**Response**: `200 OK`
```json
[
  {
    "id": "guid",
    "name": "string",
    "status": "string",
    "startedAt": "datetime",
    "metrics": {
      "totalInteractions": 0
    }
  }
]
```

**Example**:
```bash
# Get 10 most recent recordings
curl "https://localhost:5001/api/recordings/recent"

# Get 5 most recent recordings
curl "https://localhost:5001/api/recordings/recent?count=5"
```

---

## Data Models

### RecordingSession

```json
{
  "id": "guid",
  "name": "string",
  "description": "string",
  "status": "Recording|Paused|Stopped|Failed|Generated",
  "startedAt": "datetime",
  "endedAt": "datetime",
  "startUrl": "string",
  "browser": "string",
  "viewportSize": {
    "width": 1280,
    "height": 720
  },
  "interactions": [],
  "configuration": {},
  "metrics": {},
  "tags": [],
  "createdBy": "string"
}
```

### UserInteraction

```json
{
  "id": "guid",
  "sessionId": "guid",
  "sequenceNumber": 1,
  "actionType": "Click|Input|Navigation|Select|...",
  "intent": "Authentication|Navigation|DataEntry|...",
  "intentConfidence": 0.95,
  "description": "string",
  "timestamp": "datetime",
  "durationMs": 1000,
  "inputValue": "string",
  "context": {
    "url": "string",
    "pageTitle": "string",
    "targetSelector": "string",
    "elementTag": "string",
    "elementText": "string"
  },
  "includeInTest": true,
  "generatedCode": "string"
}
```

### GeneratedTest

```json
{
  "sessionId": "guid",
  "code": "string",
  "className": "string",
  "namespace": "string",
  "methods": [],
  "pageObjects": [],
  "imports": [],
  "metrics": {
    "testMethodCount": 1,
    "assertionCount": 5,
    "linesOfCode": 120,
    "estimatedCoverage": 85.5,
    "maintainabilityScore": 87.3
  }
}
```

---

## Error Responses

All error responses follow the Problem Details format (RFC 7807):

```json
{
  "type": "string",
  "title": "string",
  "status": 400,
  "detail": "string",
  "instance": "string"
}
```

### Common Errors

**400 Bad Request**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "name": ["The name field is required."],
    "startUrl": ["The startUrl field must be a valid URL."]
  }
}
```

**404 Not Found**:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Recording not found",
  "status": 404,
  "detail": "Recording session with ID 3fa85f64-5717-4562-b3fc-2c963f66afa6 was not found."
}
```

---

## Rate Limiting

Currently, no rate limiting is implemented. For production deployments, consider implementing rate limiting based on your requirements.

---

## OpenAPI/Swagger

Interactive API documentation is available at:
```
https://localhost:5001/openapi
```

Download OpenAPI specification:
```
https://localhost:5001/openapi/v1.json
```

---

## Examples

### Complete Workflow Example

```bash
# 1. Start recording
SESSION_ID=$(curl -X POST "https://localhost:5001/api/recordings/start" \
  -H "Content-Type: application/json" \
  -d '{"name":"Login Test","startUrl":"https://example.com/login"}' \
  | jq -r '.id')

# 2. Wait for user interactions...
# (User performs actions in browser)

# 3. Stop recording
curl -X POST "https://localhost:5001/api/recordings/$SESSION_ID/stop"

# 4. Analyze with AI
curl -X POST "https://localhost:5001/api/recordings/$SESSION_ID/analyze"

# 5. Generate test code
curl -X POST "https://localhost:5001/api/recordings/$SESSION_ID/generate" \
  -H "Content-Type: application/json" \
  -d '{"testFramework":"xUnit","autoGenerateAssertions":true}' \
  | jq -r '.code' > LoginTest.cs

# 6. Validate accuracy
curl -X POST "https://localhost:5001/api/recordings/$SESSION_ID/validate"
```

---

**Version**: 1.0.0  
**Last Updated**: December 2024
