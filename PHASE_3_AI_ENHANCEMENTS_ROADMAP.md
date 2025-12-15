# Phase 3: AI Enhancements - Implementation Roadmap

## Status: ?? **PLANNING** - Ready for Implementation

### Date: 2025-12-09
### Estimated Duration: 80-100 hours
### Priority: High

---

## Executive Summary

Phase 3 focuses on advanced AI-powered capabilities that leverage the existing framework to provide intelligent, self-healing, and adaptive browser automation. This phase builds upon the solid foundation of Phase 2 and introduces cutting-edge AI features that significantly reduce maintenance overhead and improve test reliability.

---

## Phase 3 Goals

### Primary Objectives
1. **Reduce Test Maintenance** - Auto-fix selector changes without manual intervention
2. **Improve Test Reliability** - Smart waiting and adaptive strategies
3. **Accelerate Test Creation** - Generate tests from user recordings
4. **Enhance Visual Intelligence** - Screenshot analysis and element detection
5. **Intelligent Error Recovery** - Context-aware retry and healing logic

### Success Metrics
- ? 80% reduction in test maintenance time
- ? 95%+ test reliability rate
- ? 50% faster test creation from recordings
- ? 90%+ selector healing success rate
- ? Zero manual intervention for common failures

---

## Feature Breakdown

### Feature 1: Self-Healing Tests (Auto-Fix Selector Changes)

**Priority:** ?? Critical  
**Estimated Time:** 25-30 hours  
**Complexity:** High  

#### Overview
Automatically detect and fix broken selectors when page structure changes, using AI to identify the target element through multiple strategies.

#### Components

##### 1.1 Selector Healing Service
**Location:** `EvoAITest.Core/Services/SelectorHealingService.cs`

**Capabilities:**
- Detect selector failures with context
- Multiple healing strategies:
  - Visual similarity matching
  - Text content matching
  - ARIA label matching
  - Position-based matching
  - Fuzzy attribute matching
- Confidence scoring for healed selectors
- Automatic selector update in task definitions

**Key Methods:**
```csharp
public interface ISelectorHealingService
{
    Task<HealedSelector?> HealSelectorAsync(
        string failedSelector,
        PageState pageState,
        string? expectedText,
        byte[]? screenshot,
        CancellationToken cancellationToken = default);
    
    Task<List<SelectorCandidate>> FindSelectorCandidatesAsync(
        PageState pageState,
        HealingContext context,
        CancellationToken cancellationToken = default);
    
    Task<double> CalculateConfidenceScoreAsync(
        SelectorCandidate candidate,
        HealingContext context,
        CancellationToken cancellationToken = default);
}
```

##### 1.2 Visual Element Matcher
**Location:** `EvoAITest.Core/Services/VisualElementMatcher.cs`

**Capabilities:**
- Compare element screenshots for similarity
- Use SSIM/perceptual hashing for matching
- Handle minor visual changes
- Position and size tolerance

##### 1.3 LLM-Powered Selector Generator
**Location:** `EvoAITest.Agents/Agents/SelectorAgent.cs`

**Capabilities:**
- Analyze page structure with LLM
- Generate robust selectors
- Suggest multiple alternatives
- Explain reasoning for selections

##### 1.4 Healing History & Learning
**Location:** `EvoAITest.Core/Data/Models/SelectorHealingHistory.cs`

**Database Schema:**
```sql
CREATE TABLE SelectorHealingHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TaskId UNIQUEIDENTIFIER NOT NULL,
    OriginalSelector NVARCHAR(500) NOT NULL,
    HealedSelector NVARCHAR(500) NOT NULL,
    HealingStrategy VARCHAR(50) NOT NULL,
    ConfidenceScore FLOAT NOT NULL,
    Success BIT NOT NULL,
    HealedAt DATETIMEOFFSET NOT NULL,
    PageUrl NVARCHAR(2000),
    Context NVARCHAR(MAX) -- JSON
);
```

#### Implementation Steps

**Step 1:** Create Models (3 hours)
- HealedSelector record
- SelectorCandidate record
- HealingContext class
- HealingStrategy enum

**Step 2:** Implement Core Service (8 hours)
- SelectorHealingService implementation
- Multiple healing strategies
- Confidence scoring algorithm
- Integration with PageState

**Step 3:** Visual Matching (6 hours)
- VisualElementMatcher implementation
- Screenshot comparison utilities
- Perceptual hashing
- Position-based matching

**Step 4:** LLM Integration (5 hours)
- SelectorAgent implementation
- Prompt templates for selector generation
- Multi-candidate generation
- Reasoning extraction

**Step 5:** Database & Persistence (3 hours)
- Migration for SelectorHealingHistory
- Repository methods
- Healing history tracking
- Learning from past healings

**Step 6:** Executor Integration (4 hours)
- Automatic healing on selector failures
- Fallback chain implementation
- Success/failure tracking
- Update task definitions

---

### Feature 2: Visual Element Detection (Screenshot Analysis)

**Priority:** ?? High  
**Estimated Time:** 20-25 hours  
**Complexity:** Medium-High  

#### Overview
Use AI-powered vision models to detect and locate UI elements in screenshots without selectors, enabling visual-first automation.

#### Components

##### 2.1 Vision Analysis Service
**Location:** `EvoAITest.Core/Services/VisionAnalysisService.cs`

**Capabilities:**
- Detect UI elements in screenshots
- Classify element types (button, input, link, etc.)
- Extract text from images (OCR)
- Identify element positions and boundaries
- Generate selectors from visual analysis

**Key Methods:**
```csharp
public interface IVisionAnalysisService
{
    Task<List<DetectedElement>> DetectElementsAsync(
        byte[] screenshot,
        ElementFilter? filter = null,
        CancellationToken cancellationToken = default);
    
    Task<DetectedElement?> FindElementByDescriptionAsync(
        byte[] screenshot,
        string description,
        CancellationToken cancellationToken = default);
    
    Task<string?> ExtractTextAsync(
        byte[] screenshot,
        Rectangle? region = null,
        CancellationToken cancellationToken = default);
    
    Task<ElementBoundingBox> LocateElementAsync(
        byte[] screenshot,
        string elementDescription,
        CancellationToken cancellationToken = default);
}
```

##### 2.2 Azure Computer Vision Integration
**Location:** `EvoAITest.LLM/Vision/AzureComputerVisionProvider.cs`

**Capabilities:**
- OCR for text extraction
- Object detection for UI elements
- Image classification
- Custom model training

##### 2.3 GPT-4 Vision Integration
**Location:** `EvoAITest.LLM/Vision/GPT4VisionProvider.cs`

**Capabilities:**
- Describe screenshot content
- Locate elements by description
- Understand UI layouts
- Generate interaction strategies

##### 2.4 Visual Automation Tools
**New Tools:**
- `find_element_by_image` - Locate element by visual appearance
- `click_by_description` - Click element described in natural language
- `extract_text_from_image` - OCR text extraction
- `verify_element_by_image` - Visual verification

#### Implementation Steps

**Step 1:** Vision Models (5 hours)
- DetectedElement class
- ElementBoundingBox class
- ElementFilter class
- Vision result models

**Step 2:** Azure CV Integration (6 hours)
- AzureComputerVisionProvider
- OCR implementation
- Object detection
- Configuration and authentication

**Step 3:** GPT-4 Vision Integration (5 hours)
- GPT4VisionProvider
- Screenshot encoding
- Prompt engineering for vision
- Response parsing

**Step 4:** Vision Analysis Service (6 hours)
- Service implementation
- Multi-provider support
- Element detection logic
- Coordinate mapping

**Step 5:** New Tools (4 hours)
- 4 new vision-based tools
- Tool executors
- BrowserToolRegistry updates
- Parameter validation

**Step 6:** Testing & Integration (4 hours)
- Unit tests for vision service
- Integration tests with real images
- Tool execution tests
- Documentation

---

### Feature 3: Smart Waiting Strategies

**Priority:** ?? Medium-High  
**Estimated Time:** 15-20 hours  
**Complexity:** Medium  

#### Overview
Intelligent, context-aware waiting strategies that adapt based on page behavior, reducing flakiness and improving test speed.

#### Components

##### 3.1 Smart Wait Service
**Location:** `EvoAITest.Core/Services/SmartWaitService.cs`

**Capabilities:**
- Adaptive timeout calculation
- Multi-condition waiting
- Network activity monitoring
- Animation detection
- JavaScript execution completion
- Custom wait predicates

**Key Methods:**
```csharp
public interface ISmartWaitService
{
    Task WaitForStableStateAsync(
        WaitConditions conditions,
        int maxWaitMs = 10000,
        CancellationToken cancellationToken = default);
    
    Task<bool> WaitForConditionAsync(
        Func<Task<bool>> predicate,
        TimeSpan? timeout = null,
        TimeSpan? pollingInterval = null,
        CancellationToken cancellationToken = default);
    
    Task WaitForNetworkIdleAsync(
        int maxActiveRequests = 0,
        int idleDurationMs = 500,
        CancellationToken cancellationToken = default);
    
    Task WaitForAnimationsAsync(
        string? selector = null,
        CancellationToken cancellationToken = default);
    
    Task<TimeSpan> CalculateOptimalTimeoutAsync(
        string action,
        HistoricalData history,
        CancellationToken cancellationToken = default);
}
```

##### 3.2 Page Stability Detector
**Location:** `EvoAITest.Core/Services/PageStabilityDetector.cs`

**Capabilities:**
- Detect page load completion
- Monitor DOM mutations
- Track animation states
- Identify spinners/loaders
- Network request monitoring

##### 3.3 Wait History & Learning
**Location:** `EvoAITest.Core/Data/Models/WaitHistory.cs`

**Database Schema:**
```sql
CREATE TABLE WaitHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TaskId UNIQUEIDENTIFIER NOT NULL,
    Action VARCHAR(100) NOT NULL,
    Selector NVARCHAR(500),
    WaitCondition VARCHAR(50) NOT NULL,
    TimeoutMs INT NOT NULL,
    ActualWaitMs INT NOT NULL,
    Success BIT NOT NULL,
    PageUrl NVARCHAR(2000),
    RecordedAt DATETIMEOFFSET NOT NULL
);
```

##### 3.4 New Wait Tools
**New Tools:**
- `smart_wait` - Adaptive waiting with multiple conditions
- `wait_for_stable` - Wait for page stability
- `wait_for_animations` - Wait for animations to complete
- `wait_for_network_idle` - Wait for network requests

#### Implementation Steps

**Step 1:** Models & Abstractions (3 hours)
- WaitConditions class
- StabilityMetrics class
- HistoricalData class
- Wait strategy enums

**Step 2:** Smart Wait Service (6 hours)
- Core service implementation
- Multi-condition logic
- Timeout calculation
- Polling strategies

**Step 3:** Stability Detection (4 hours)
- PageStabilityDetector
- DOM mutation observation
- Animation detection
- Network monitoring

**Step 4:** History & Learning (3 hours)
- WaitHistory entity
- Repository methods
- Analytics queries
- Adaptive timeout logic

**Step 5:** New Tools (3 hours)
- 4 new wait tools
- Tool executors
- BrowserToolRegistry updates
- Documentation

**Step 6:** Testing (3 hours)
- Unit tests
- Integration tests
- Performance tests
- Edge case handling

---

### Feature 4: Error Recovery and Retry Logic

**Priority:** ?? Medium  
**Estimated Time:** 12-15 hours  
**Complexity:** Medium  

#### Overview
Context-aware error recovery with intelligent retry strategies based on error type, historical data, and page state.

#### Components

##### 4.1 Error Recovery Service
**Location:** `EvoAITest.Core/Services/ErrorRecoveryService.cs`

**Capabilities:**
- Classify error types
- Determine retry strategies
- Apply recovery actions
- Learn from past recoveries
- Exponential backoff with jitter

**Key Methods:**
```csharp
public interface IErrorRecoveryService
{
    Task<RecoveryResult> RecoverFromErrorAsync(
        Exception error,
        ExecutionContext context,
        CancellationToken cancellationToken = default);
    
    Task<RetryStrategy> DetermineRetryStrategyAsync(
        ErrorClassification errorClass,
        int attemptNumber,
        HistoricalRecoveryData history,
        CancellationToken cancellationToken = default);
    
    Task<List<RecoveryAction>> SuggestRecoveryActionsAsync(
        Exception error,
        PageState pageState,
        CancellationToken cancellationToken = default);
}
```

##### 4.2 Error Classifier
**Location:** `EvoAITest.Core/Services/ErrorClassifier.cs`

**Error Categories:**
- Transient (network timeouts, rate limits)
- Element Not Found (stale selectors)
- Navigation Errors (redirects, 404s)
- JavaScript Errors (execution failures)
- Permission Errors (blocked by browser)
- Assertion Failures (test logic errors)

##### 4.3 Recovery Actions
**Predefined Actions:**
- Page refresh
- Navigation retry
- Clear cache/cookies
- Wait and retry
- Alternative selector
- Fallback flow

##### 4.4 Recovery History
**Location:** `EvoAITest.Core/Data/Models/RecoveryHistory.cs`

**Database Schema:**
```sql
CREATE TABLE RecoveryHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    TaskId UNIQUEIDENTIFIER NOT NULL,
    ErrorType VARCHAR(100) NOT NULL,
    ErrorMessage NVARCHAR(MAX),
    RecoveryStrategy VARCHAR(50) NOT NULL,
    RecoveryAction VARCHAR(100) NOT NULL,
    Success BIT NOT NULL,
    AttemptNumber INT NOT NULL,
    RecoveredAt DATETIMEOFFSET NOT NULL,
    Context NVARCHAR(MAX) -- JSON
);
```

#### Implementation Steps

**Step 1:** Models (2 hours)
- RecoveryResult class
- RetryStrategy class
- ErrorClassification enum
- RecoveryAction class

**Step 2:** Error Classifier (3 hours)
- Classification logic
- Pattern matching
- Exception analysis
- Confidence scoring

**Step 3:** Recovery Service (5 hours)
- Core service implementation
- Strategy determination
- Action execution
- History tracking

**Step 4:** Recovery Actions (3 hours)
- Implement all recovery actions
- Action chaining
- Validation logic
- Rollback support

**Step 5:** Integration (3 hours)
- Executor integration
- HealerAgent enhancement
- Automatic recovery triggers
- Configuration options

**Step 6:** Testing (2 hours)
- Unit tests
- Integration tests
- Scenario-based tests
- Documentation

---

### Feature 5: Test Generation from Recordings

**Priority:** ?? Medium  
**Estimated Time:** 25-30 hours  
**Complexity:** High  

#### Overview
Record user interactions in the browser and automatically generate natural language test descriptions and executable automation tasks.

#### Components

##### 5.1 Browser Recorder
**Location:** `EvoAITest.Core/Services/BrowserRecorderService.cs`

**Capabilities:**
- Record browser interactions
- Capture clicks, typing, navigation
- Record network requests
- Take contextual screenshots
- Generate timeline of actions

**Key Methods:**
```csharp
public interface IBrowserRecorderService
{
    Task StartRecordingAsync(
        string sessionId,
        RecordingOptions options,
        CancellationToken cancellationToken = default);
    
    Task<RecordedSession> StopRecordingAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
    
    Task<RecordedAction> CaptureActionAsync(
        ActionType actionType,
        ActionContext context,
        CancellationToken cancellationToken = default);
    
    Task<RecordedSession> GetRecordingAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
```

##### 5.2 Action Analyzer
**Location:** `EvoAITest.Agents/Agents/ActionAnalyzerAgent.cs`

**Capabilities:**
- Analyze recorded actions
- Identify user intent
- Group related actions
- Detect patterns and flows
- Remove noise/redundant actions

##### 5.3 Test Generator
**Location:** `EvoAITest.Agents/Agents/TestGeneratorAgent.cs`

**Capabilities:**
- Convert recordings to tests
- Generate natural language descriptions
- Create assertions
- Optimize selectors
- Add waits and verifications

**Key Methods:**
```csharp
public interface ITestGeneratorAgent
{
    Task<GeneratedTest> GenerateTestAsync(
        RecordedSession recording,
        TestGenerationOptions options,
        CancellationToken cancellationToken = default);
    
    Task<string> GenerateNaturalLanguageDescriptionAsync(
        RecordedSession recording,
        CancellationToken cancellationToken = default);
    
    Task<List<ExecutionStep>> GenerateExecutionStepsAsync(
        RecordedSession recording,
        CancellationToken cancellationToken = default);
    
    Task<List<Assertion>> GenerateAssertionsAsync(
        RecordedSession recording,
        CancellationToken cancellationToken = default);
}
```

##### 5.4 Recording Storage
**Location:** `EvoAITest.Core/Data/Models/RecordedSession.cs`

**Database Schema:**
```sql
CREATE TABLE RecordedSessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    SessionId VARCHAR(100) NOT NULL UNIQUE,
    Name NVARCHAR(200),
    StartedAt DATETIMEOFFSET NOT NULL,
    EndedAt DATETIMEOFFSET,
    Duration INT, -- milliseconds
    UserId NVARCHAR(200),
    InitialUrl NVARCHAR(2000),
    Status VARCHAR(50) NOT NULL
);

CREATE TABLE RecordedActions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    ActionType VARCHAR(50) NOT NULL,
    Selector NVARCHAR(500),
    Value NVARCHAR(MAX),
    Timestamp DATETIMEOFFSET NOT NULL,
    ScreenshotUrl NVARCHAR(500),
    Context NVARCHAR(MAX), -- JSON
    FOREIGN KEY (SessionId) REFERENCES RecordedSessions(Id)
);
```

##### 5.5 Recording API
**New Endpoints:**
- `POST /api/recordings/start` - Start recording
- `POST /api/recordings/{id}/stop` - Stop recording
- `GET /api/recordings/{id}` - Get recording
- `POST /api/recordings/{id}/generate-test` - Generate test
- `DELETE /api/recordings/{id}` - Delete recording

##### 5.6 Recorder UI Component
**Location:** `EvoAITest.Web/Components/Recorder.razor`

**Features:**
- Start/Stop recording controls
- Live action preview
- Recording playback
- Test generation UI
- Export options

#### Implementation Steps

**Step 1:** Models & Schema (4 hours)
- RecordedSession entity
- RecordedAction entity
- RecordingOptions class
- Migration creation

**Step 2:** Browser Recorder Service (8 hours)
- Core recording logic
- Event capture
- Screenshot integration
- Timeline management
- Storage implementation

**Step 3:** Action Analyzer Agent (5 hours)
- LLM integration for analysis
- Intent detection
- Action grouping
- Pattern recognition
- Noise filtering

**Step 4:** Test Generator Agent (6 hours)
- Recording to test conversion
- Natural language generation
- Assertion creation
- Selector optimization
- Step generation

**Step 5:** API Endpoints (3 hours)
- 5 new endpoints
- Request/response models
- Authorization
- Validation

**Step 6:** UI Component (4 hours)
- Blazor recorder component
- Control interface
- Live preview
- Generation workflow
- Export functionality

**Step 7:** Testing (3 hours)
- Unit tests
- Integration tests
- E2E recording tests
- Documentation

---

## Implementation Timeline

### Phase 3.1: Foundation (Weeks 1-2)
**Duration:** 2 weeks  
**Effort:** 40 hours  

**Features:**
- ? Feature 1: Self-Healing Tests (Step 1-3)
- ? Feature 3: Smart Waiting Strategies (Step 1-3)

**Deliverables:**
- Selector healing models and core service
- Visual element matcher
- Smart wait service and stability detector
- Database migrations
- Unit tests

### Phase 3.2: Intelligence (Weeks 3-4)
**Duration:** 2 weeks  
**Effort:** 40 hours  

**Features:**
- ? Feature 1: Self-Healing Tests (Step 4-6)
- ? Feature 2: Visual Element Detection (Step 1-3)
- ? Feature 4: Error Recovery (Step 1-3)

**Deliverables:**
- LLM-powered selector generation
- Healing history and learning
- Azure CV and GPT-4 Vision integration
- Vision analysis service
- Error classifier and recovery service
- Integration tests

### Phase 3.3: Advanced Features (Weeks 5-6)
**Duration:** 2 weeks  
**Effort:** 40 hours  

**Features:**
- ? Feature 2: Visual Element Detection (Step 4-6)
- ? Feature 3: Smart Waiting Strategies (Step 4-6)
- ? Feature 4: Error Recovery (Step 4-6)

**Deliverables:**
- Vision-based automation tools
- Wait history and adaptive timeouts
- Recovery actions and integration
- Tool registry updates
- Comprehensive testing

### Phase 3.4: Recording & Generation (Weeks 7-8)
**Duration:** 2 weeks  
**Effort:** 30 hours  

**Features:**
- ? Feature 5: Test Generation from Recordings (All steps)

**Deliverables:**
- Browser recorder service
- Action analyzer and test generator agents
- Recording storage and API
- Blazor recorder UI
- Complete documentation

---

## Technical Architecture

### New Services Architecture

```
???????????????????????????????????????????????????????????????
?                   Phase 3 AI Services                        ?
???????????????????????????????????????????????????????????????
                            ?
        ?????????????????????????????????????????
        ?                   ?                   ?
??????????????????? ??????????????????? ???????????????????
? Self-Healing    ? ? Vision Analysis ? ? Smart Waiting   ?
? - Selector      ? ? - Azure CV      ? ? - Stability     ?
?   Healing       ? ? - GPT-4 Vision  ? ?   Detection     ?
? - Visual        ? ? - Element       ? ? - Adaptive      ?
?   Matching      ? ?   Detection     ? ?   Timeouts      ?
? - LLM Selector  ? ? - OCR          ? ? - Learning      ?
??????????????????? ??????????????????? ???????????????????
        ?                   ?                   ?
        ?????????????????????????????????????????
                            ?
???????????????????????????????????????????????????????????????
?                  Error Recovery Service                      ?
?  - Classification ? Strategy ? Actions ? History            ?
???????????????????????????????????????????????????????????????
                            ?
                            ?
???????????????????????????????????????????????????????????????
?              Recording & Test Generation                     ?
?  Recorder ? Analyzer ? Generator ? Storage ? API ? UI       ?
???????????????????????????????????????????????????????????????
                            ?
                            ?
???????????????????????????????????????????????????????????????
?              Existing Phase 2 Infrastructure                 ?
?  Browser Agent ? Tool Executor ? Agents ? Database          ?
???????????????????????????????????????????????????????????????
```

### Data Flow

```
User Action / Test Execution
    ?
Browser Agent (with recording)
    ?
Action Fails?
    ?? Yes ? Error Recovery Service
    ?         ?
    ?    Error Classifier
    ?         ?
    ?    Self-Healing Service?
    ?         ?? Selector Failed ? Selector Healing
    ?         ?                      ?
    ?         ?                 Visual Matching
    ?         ?                      ?
    ?         ?                 LLM Selector Gen
    ?         ?                      ?
    ?         ?                 Try Healed Selector
    ?         ?
    ?         ?? Vision Detection ? Vision Analysis
    ?         ?                      ?
    ?         ?                 Element Detection
    ?         ?                      ?
    ?         ?                 Generate Selector
    ?         ?
    ?         ?? Timing Issue ? Smart Wait
    ?                             ?
    ?                        Stability Detection
    ?                             ?
    ?                        Adaptive Wait
    ?                             ?
    ?                        Retry Action
    ?
    ?? No ? Record Action (if recording)
              ?
         Continue Execution
              ?
         Test Complete ? Generate from Recording?
                              ?
                         Test Generator Agent
                              ?
                         Natural Language Test
```

---

## Database Schema Updates

### New Tables

```sql
-- Selector Healing History
CREATE TABLE SelectorHealingHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TaskId UNIQUEIDENTIFIER NOT NULL,
    OriginalSelector NVARCHAR(500) NOT NULL,
    HealedSelector NVARCHAR(500) NOT NULL,
    HealingStrategy VARCHAR(50) NOT NULL,
    ConfidenceScore FLOAT NOT NULL,
    Success BIT NOT NULL,
    HealedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    PageUrl NVARCHAR(2000),
    ExpectedText NVARCHAR(500),
    Context NVARCHAR(MAX),
    FOREIGN KEY (TaskId) REFERENCES AutomationTasks(Id) ON DELETE CASCADE
);

-- Wait History
CREATE TABLE WaitHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TaskId UNIQUEIDENTIFIER NOT NULL,
    Action VARCHAR(100) NOT NULL,
    Selector NVARCHAR(500),
    WaitCondition VARCHAR(50) NOT NULL,
    TimeoutMs INT NOT NULL,
    ActualWaitMs INT NOT NULL,
    Success BIT NOT NULL,
    PageUrl NVARCHAR(2000),
    RecordedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    FOREIGN KEY (TaskId) REFERENCES AutomationTasks(Id) ON DELETE CASCADE
);

-- Recovery History
CREATE TABLE RecoveryHistory (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    TaskId UNIQUEIDENTIFIER NOT NULL,
    ErrorType VARCHAR(100) NOT NULL,
    ErrorMessage NVARCHAR(MAX),
    RecoveryStrategy VARCHAR(50) NOT NULL,
    RecoveryAction VARCHAR(100) NOT NULL,
    Success BIT NOT NULL,
    AttemptNumber INT NOT NULL,
    RecoveredAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    Context NVARCHAR(MAX),
    FOREIGN KEY (TaskId) REFERENCES AutomationTasks(Id) ON DELETE CASCADE
);

-- Recorded Sessions
CREATE TABLE RecordedSessions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SessionId VARCHAR(100) NOT NULL UNIQUE,
    Name NVARCHAR(200),
    StartedAt DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    EndedAt DATETIMEOFFSET,
    Duration INT,
    UserId NVARCHAR(200),
    InitialUrl NVARCHAR(2000),
    FinalUrl NVARCHAR(2000),
    Status VARCHAR(50) NOT NULL,
    GeneratedTestId UNIQUEIDENTIFIER,
    FOREIGN KEY (GeneratedTestId) REFERENCES AutomationTasks(Id)
);

-- Recorded Actions
CREATE TABLE RecordedActions (
    Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    SessionId UNIQUEIDENTIFIER NOT NULL,
    SequenceNumber INT NOT NULL,
    ActionType VARCHAR(50) NOT NULL,
    Selector NVARCHAR(500),
    Value NVARCHAR(MAX),
    Timestamp DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    ScreenshotUrl NVARCHAR(500),
    PageUrl NVARCHAR(2000),
    Context NVARCHAR(MAX),
    FOREIGN KEY (SessionId) REFERENCES RecordedSessions(Id) ON DELETE CASCADE
);

-- Indexes for Performance
CREATE INDEX IX_SelectorHealing_TaskId ON SelectorHealingHistory(TaskId);
CREATE INDEX IX_SelectorHealing_Success ON SelectorHealingHistory(Success);
CREATE INDEX IX_WaitHistory_TaskId ON WaitHistory(TaskId);
CREATE INDEX IX_WaitHistory_Action ON WaitHistory(Action);
CREATE INDEX IX_RecoveryHistory_TaskId ON RecoveryHistory(TaskId);
CREATE INDEX IX_RecoveryHistory_ErrorType ON RecoveryHistory(ErrorType);
CREATE INDEX IX_RecordedActions_SessionId ON RecordedActions(SessionId);
CREATE INDEX IX_RecordedActions_Sequence ON RecordedActions(SessionId, SequenceNumber);
```

---

## Configuration Updates

### appsettings.json Extensions

```json
{
  "EvoAITest": {
    "Core": {
      "// ... existing config ...",
      
      "Phase3": {
        "SelfHealing": {
          "Enabled": true,
          "ConfidenceThreshold": 0.75,
          "MaxHealingAttempts": 3,
          "SaveHealedSelectors": true,
          "Strategies": [
            "VisualSimilarity",
            "TextContent",
            "AriaLabel",
            "Position",
            "FuzzyAttributes"
          ]
        },
        
        "VisionAnalysis": {
          "Enabled": true,
          "Provider": "GPT4Vision", // or "AzureComputerVision"
          "AzureComputerVision": {
            "Endpoint": "https://your-cv.cognitiveservices.azure.com/",
            "SubscriptionKey": "from-keyvault"
          },
          "OCR": {
            "Enabled": true,
            "Language": "en",
            "DetectOrientation": true
          }
        },
        
        "SmartWaiting": {
          "Enabled": true,
          "AdaptiveTimeouts": true,
          "DefaultMaxWaitMs": 10000,
          "NetworkIdleMs": 500,
          "AnimationDetection": true,
          "LearningEnabled": true
        },
        
        "ErrorRecovery": {
          "Enabled": true,
          "AutoRetry": true,
          "MaxRetries": 3,
          "RecoveryActions": [
            "PageRefresh",
            "AlternativeSelector",
            "WaitAndRetry",
            "ClearCookies",
            "NavigationRetry"
          ]
        },
        
        "Recording": {
          "Enabled": true,
          "AutoScreenshot": true,
          "CaptureNetwork": true,
          "MaxSessionDuration": 3600000, // 1 hour
          "StoragePath": "recordings"
        }
      }
    }
  }
}
```

---

## API Endpoints

### New Endpoints Summary

#### Self-Healing
- `POST /api/healing/selectors/heal` - Heal a failed selector
- `GET /api/healing/selectors/history/{taskId}` - Get healing history
- `GET /api/healing/selectors/candidates` - Get selector candidates

#### Vision
- `POST /api/vision/analyze` - Analyze screenshot
- `POST /api/vision/detect-elements` - Detect elements in image
- `POST /api/vision/find-element` - Find element by description
- `POST /api/vision/ocr` - Extract text from image

#### Recording
- `POST /api/recordings/start` - Start recording session
- `POST /api/recordings/{id}/stop` - Stop recording
- `GET /api/recordings` - List all recordings
- `GET /api/recordings/{id}` - Get recording details
- `POST /api/recordings/{id}/generate-test` - Generate test from recording
- `DELETE /api/recordings/{id}` - Delete recording

#### Analytics
- `GET /api/analytics/healing-success-rate` - Healing statistics
- `GET /api/analytics/wait-times` - Wait time analytics
- `GET /api/analytics/recovery-patterns` - Recovery patterns

---

## New Browser Tools

### Phase 3 Tools (10 new tools)

| Tool | Description | Category |
|------|-------------|----------|
| **find_element_by_image** | Locate element by visual appearance | Vision |
| **click_by_description** | Click element by natural language description | Vision |
| **extract_text_from_image** | OCR text extraction from screenshot region | Vision |
| **verify_element_by_image** | Visual verification without selector | Vision |
| **smart_wait** | Adaptive waiting with multiple conditions | Smart Wait |
| **wait_for_stable** | Wait for page stability | Smart Wait |
| **wait_for_animations** | Wait for animations to complete | Smart Wait |
| **wait_for_network_idle** | Wait for network requests | Smart Wait |
| **heal_selector** | Manually trigger selector healing | Self-Healing |
| **record_session** | Control recording (start/stop/save) | Recording |

**Updated Total:** 35 browser tools (14 core + 6 mobile + 5 network + 10 AI)

---

## Dependencies & Prerequisites

### New NuGet Packages

```xml
<!-- Vision & AI -->
<PackageReference Include="Azure.AI.Vision.ImageAnalysis" Version="1.0.0-beta.1" />
<PackageReference Include="Azure.AI.FormRecognizer" Version="4.1.0" />

<!-- Image Processing -->
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.0" />
<PackageReference Include="System.Drawing.Common" Version="8.0.0" />

<!-- Advanced ML -->
<PackageReference Include="Microsoft.ML" Version="3.0.0" />
<PackageReference Include="Microsoft.ML.Vision" Version="3.0.0" />

<!-- Perceptual Hashing -->
<PackageReference Include="Shipwreck.Phash" Version="0.6.0" />
```

### Azure Services Required

1. **Azure Computer Vision** (optional, for OCR)
   - Resource creation required
   - Key stored in Key Vault
   
2. **GPT-4 Vision** (part of Azure OpenAI)
   - GPT-4 with vision capabilities
   - Requires updated deployment

3. **Azure Storage** (for recordings)
   - Blob storage for session recordings
   - Table storage for quick lookups

---

## Testing Strategy

### Unit Tests
- ? Selector healing logic
- ? Vision analysis mocking
- ? Wait condition evaluation
- ? Error classification
- ? Recording capture
- ? Test generation

**Target:** 150+ new unit tests

### Integration Tests
- ? End-to-end self-healing
- ? Vision-based automation
- ? Smart wait scenarios
- ? Error recovery flows
- ? Recording to test generation

**Target:** 25+ integration tests

### Performance Tests
- ? Selector healing performance (<500ms)
- ? Vision analysis latency (<2s)
- ? Wait optimization impact
- ? Recording overhead (<5%)

---

## Success Criteria

### Feature 1: Self-Healing
- ? 90%+ selector healing success rate
- ? <500ms average healing time
- ? Confidence score >0.75 for 80%+ healings
- ? Zero false positives in healing
- ? Automatic selector update in tasks

### Feature 2: Vision
- ? 95%+ element detection accuracy
- ? <2s average vision analysis time
- ? OCR accuracy >95% for standard fonts
- ? Successfully locate elements by description

### Feature 3: Smart Wait
- ? 50% reduction in unnecessary wait time
- ? 80% reduction in timing-related failures
- ? Adaptive timeouts within 10% of optimal
- ? Zero false timeout failures

### Feature 4: Error Recovery
- ? 85%+ automatic recovery success rate
- ? Correct error classification in 95%+ cases
- ? Average 2 retries before success
- ? No infinite retry loops

### Feature 5: Recording
- ? Capture 100% of user interactions
- ? Generate executable test in <10s
- ? 90%+ accuracy in action recognition
- ? Generated tests pass without modification

---

## Risk Assessment

### High Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Vision API costs | High | Medium | Implement caching, rate limiting |
| Healing false positives | High | Medium | Conservative confidence thresholds |
| Performance degradation | Medium | Low | Performance testing, lazy loading |
| Complex UI detection | High | High | Multiple detection strategies |

### Medium Risks

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Integration complexity | Medium | Medium | Phased rollout, feature flags |
| Learning curve | Medium | Medium | Comprehensive documentation |
| Backward compatibility | Low | Low | Maintain existing APIs |

---

## Documentation Requirements

### User Documentation
- ? Self-healing guide
- ? Vision automation guide
- ? Smart wait strategies guide
- ? Recording and test generation tutorial
- ? Troubleshooting guide for AI features

### Developer Documentation
- ? Architecture documentation
- ? API reference for new services
- ? Integration guide
- ? Configuration guide
- ? Extension guide

### Code Documentation
- ? XML documentation on all public APIs
- ? Code examples for each feature
- ? Architecture decision records
- ? Performance guidelines

---

## Phase 3 Deliverables Summary

### Code
- 5 new major services
- 10 new browser tools
- 4 new AI agents
- 5 new database tables
- 6 new API endpoint groups
- 1 Blazor recorder component

### Tests
- 150+ unit tests
- 25+ integration tests
- Performance test suite

### Documentation
- 5 user guides
- Complete API reference
- Architecture documentation
- Migration guides

### Statistics
- **Estimated Lines of Code:** 12,000-15,000
- **Estimated Tests:** 175+
- **Estimated Documentation:** 8,000 lines
- **Total Effort:** 80-100 hours
- **Duration:** 8 weeks (part-time)

---

## Next Steps

### Immediate Actions
1. ? Review and approve roadmap
2. ? Set up Azure Computer Vision resource
3. ? Update Azure OpenAI to GPT-4 Vision
4. ? Create feature branch for Phase 3
5. ? Set up project tracking (GitHub Projects/Azure DevOps)

### Week 1 Actions
1. Implement selector healing models
2. Create database migrations
3. Start SelectorHealingService implementation
4. Set up unit test framework for Phase 3

---

## Conclusion

Phase 3: AI Enhancements represents a significant leap in automation intelligence, moving from scripted automation to truly adaptive, self-healing test infrastructure. By leveraging cutting-edge AI capabilities, this phase will dramatically reduce maintenance overhead and improve test reliability.

The phased approach ensures steady progress with incremental value delivery, while the comprehensive testing strategy ensures production quality at every step.

**Estimated ROI:**
- 80% reduction in test maintenance time = **$50K-100K/year saved**
- 95% test reliability = **50% reduction in false failures**
- 50% faster test creation = **2-4 hours saved per test**
- Reduced flakiness = **Improved developer productivity**

**Total Phase 3 Investment:** 80-100 hours (~$10K-15K at $125/hr)  
**Break-even Timeline:** 2-3 months  
**3-Year Value:** $150K-300K+  

---

**Roadmap Version:** 1.0  
**Created:** 2025-12-09  
**Status:** Ready for Review  
**Priority:** High  
**Risk Level:** Medium  

---

*For questions or feedback on this roadmap, please contact the development team.*
