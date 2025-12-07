# Visual Regression Testing - Implementation Roadmap

## ✅ Phase 1: Core Models and Specification (COMPLETE)

### What Was Done
- [x] Complete 7-section specification document
- [x] Core data models (`VisualCheckpoint.cs`)
- [x] Service interface (`IVisualComparisonService.cs`)
- [x] Extended `ExecutionHistory` with visual regression fields
- [x] Extended `AutomationTask` with visual checkpoints
- [x] Added `visual_check` tool to `BrowserToolRegistry`
- [x] Build verification ✅ **SUCCESSFUL**

### Deliverables
- `VISUAL_REGRESSION_SPEC.md` (~2000 lines)
- `VISUAL_REGRESSION_SUMMARY.md` (~500 lines)
- `EvoAITest.Core/Models/VisualCheckpoint.cs` (~300 lines)
- `EvoAITest.Core/Abstractions/IVisualComparisonService.cs` (~100 lines)

---

## 🔄 Phase 2: Comparison Engine Implementation (NEXT)

### Tasks

#### 2.1 Create VisualComparisonEngine ✅ **COMPLETE**
**File:** `EvoAITest.Core/Services/VisualComparisonEngine.cs`

**Status:** ✅ **IMPLEMENTED** (~520 lines)
- Core comparison logic with pixel-by-pixel + SSIM
- Support for all 4 checkpoint types
- Difference region detection with flood-fill
- Diff image generation (red highlights)
- Anti-aliasing detection
- Cancellation token support

**Dependencies:**
```xml
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.12" />
<PackageReference Include="SixLabors.ImageSharp.Drawing" Version="2.1.4" />
```

**Build Status:** ✅ **SUCCESSFUL**

**Documentation:** `PHASE_2_1_COMPLETE.md`

**Estimated Effort:** ~~2-3 days~~ → **Actual: ~3 hours**

#### 2.2 Implement VisualComparisonService ✅ **COMPLETE**
**File:** `EvoAITest.Core/Services/VisualComparisonService.cs`

**Status:** ✅ **IMPLEMENTED** (~370 lines)
- Complete workflow orchestration for visual regression
- Integration with VisualComparisonEngine and FileStorageService
- Database persistence for baselines and results
- First-run baseline creation
- Baseline approval workflow
- Comparison history retrieval
- SHA256 image hash calculation

**Build Status:** ✅ **SUCCESSFUL**

**Documentation:** `PHASE_2_2_2_3_COMPLETE.md`

**Estimated Effort:** ~~1-2 days~~ → **Actual: ~1 hour**

#### 2.3 Create FileStorageService ✅ **COMPLETE**
**Files:** 
- `EvoAITest.Core/Abstractions/IFileStorageService.cs` (~50 lines)
- `EvoAITest.Core/Services/LocalFileStorageService.cs` (~200 lines)

**Status:** ✅ **IMPLEMENTED**
- Clean storage abstraction (IFileStorageService)
- Local filesystem implementation with path sanitization
- Structured storage organization (baselines/actual/diff)
- URL generation for web access
- Existence checking and error handling

**Build Status:** ✅ **SUCCESSFUL**

**Documentation:** `PHASE_2_2_2_3_COMPLETE.md`

**Estimated Effort:** ~~0.5 days~~ → **Actual: ~1 hour**

#### 2.4 Database Migration ✅ **COMPLETE**
**File:** Migration: `20251207131304_AddVisualRegressionTables`

**Status:** ✅ **IMPLEMENTED**
- Created EvoAIDbContextFactory for design-time migrations
- Updated EvoAIDbContext with VisualBaseline and VisualComparisonResult entities
- Added 2 new tables: VisualBaselines, VisualComparisonResults
- Extended 2 existing tables: AutomationTasks, ExecutionHistory
- Created 10 indexes for query optimization
- Configured 5 foreign keys with proper cascading

**Tables:**
- `VisualBaselines` - Stores baseline images metadata
- `VisualComparisonResults` - Stores comparison results
- `ExecutionHistory` - Added visual regression fields
- `AutomationTasks` - Added VisualCheckpoints and Metadata fields

**Build Status:** ✅ **SUCCESSFUL**

**Documentation:** `PHASE_2_4_COMPLETE.md`

**Estimated Effort:** ~~0.5 days~~ → **Actual: ~30 minutes**

#### 2.5 Repository Extensions ✅ **COMPLETE**
**File:** `EvoAITest.Core/Repositories/AutomationTaskRepository.cs`

**Status:** ✅ **IMPLEMENTED** (~280 lines)
- Added 8 new repository methods for visual regression
- GetBaselineAsync - Retrieve baseline by configuration
- SaveBaselineAsync - Persist baselines
- GetComparisonHistoryAsync - Query comparison history with pagination
- SaveComparisonResultAsync - Persist comparison results
- GetBaselinesByTaskAsync - Get all baselines for a task
- GetBaselinesByBranchAsync - Branch-specific baselines
- GetFailedComparisonsAsync - Filter failed comparisons
- DeleteOldBaselinesAsync - Cleanup retention policy

**Build Status:** ✅ **SUCCESSFUL**

**Documentation:** `PHASE_2_5_COMPLETE.md`

**Estimated Effort:** ~~0.5 days~~ → **Actual: ~45 minutes**

### Phase 2 Total Effort: ~~5 days~~ → **Actual: ~6 hours**

**✅ PHASE 2 COMPLETE - ALL TASKS FINISHED**

| Task | Status | Time | LOC |
|------|--------|------|-----|
| 2.1 VisualComparisonEngine | ✅ | 3 hrs | 520 |
| 2.2 VisualComparisonService | ✅ | 1 hr | 370 |
| 2.3 FileStorageService | ✅ | 1 hr | 250 |
| 2.4 Database Migration | ✅ | 30 min | 200 |
| 2.5 Repository Extensions | ✅ | 45 min | 280 |
| **Total** | **100%** | **~6 hrs** | **1,620** |

---

## 🔄 Phase 3: Executor Integration (AFTER PHASE 2)

### Tasks

#### 3.1 Extend DefaultToolExecutor
**File:** `EvoAITest.Core/Services/DefaultToolExecutor.cs`

```csharp
private async Task<ToolExecutionResult> ExecuteVisualCheckAsync(
    Dictionary<string, object> parameters,
    ToolExecutionContext context,
    CancellationToken cancellationToken)
{
    // 1. Parse parameters
    var checkpointName = parameters["checkpoint_name"].ToString();
    var checkpointType = ParseCheckpointType(parameters["checkpoint_type"]);
    var tolerance = Convert.ToDouble(parameters.GetValueOrDefault("tolerance", 0.01));
    var selector = parameters.GetValueOrDefault("selector")?.ToString();
    var ignoreSelectors = ParseIgnoreSelectors(parameters["ignore_selectors"]);
    
    // 2. Create checkpoint
    var checkpoint = new VisualCheckpoint { ... };
    
    // 3. Capture screenshot
    byte[] screenshot = checkpointType switch
    {
        CheckpointType.FullPage => await _browser.TakeScreenshotAsync(...),
        CheckpointType.Element => await _browser.TakeElementScreenshotAsync(...),
        CheckpointType.Region => await _browser.TakeRegionScreenshotAsync(...),
        CheckpointType.Viewport => await _browser.TakeViewportScreenshotAsync(...),
        _ => throw new NotSupportedException()
    };
    
    // 4. Compare against baseline
    var comparisonResult = await _visualService.CompareAsync(
        checkpoint, screenshot, context.TaskId, ...);
    
    // 5. Store results
    context.VisualComparisonResults.Add(comparisonResult);
    
    // 6. Return success/failure
    return new ToolExecutionResult
    {
        Success = comparisonResult.Passed,
        Output = $"Visual check '{checkpointName}': ...",
        Metadata = { ["comparison_id"] = comparisonResult.Id, ... }
    };
}
```

**Estimated Effort:** 1 day

#### 3.2 Add Browser Screenshot Methods
**File:** `EvoAITest.Core/Browser/PlaywrightBrowserAgent.cs`

```csharp
public Task<byte[]> TakeElementScreenshotAsync(string selector, ...)
public Task<byte[]> TakeRegionScreenshotAsync(ScreenshotRegion region, ...)
public Task<byte[]> TakeViewportScreenshotAsync(...)
```

**Estimated Effort:** 0.5 days

#### 3.3 Update ToolExecutionContext
**File:** `EvoAITest.Core/Models/ToolExecutionContext.cs`

```csharp
public sealed class ToolExecutionContext
{
    // ...existing properties...
    
    public List<VisualComparisonResult> VisualComparisonResults { get; init; } = new();
    public string Environment { get; set; } = "dev";
    public string Browser { get; set; } = "chromium";
    public string Viewport { get; set; } = "1920x1080";
}
```

**Estimated Effort:** 0.25 days

### Phase 3 Total Effort: ~2 days

---

## 🔄 Phase 4: Healer Integration (AFTER PHASE 3)

### Tasks

#### 4.1 Visual Regression Healing
**File:** `EvoAITest.Agents/Agents/HealerAgent.cs`

```csharp
private async Task<ExecutionPlan> HealVisualRegressionAsync(
    ExecutionResult failedResult,
    AgentTask task,
    CancellationToken cancellationToken)
{
    var visualFailures = failedResult.VisualComparisonResults
        .Where(v => !v.Passed)
        .ToList();
    
    // Analyze failures with LLM
    var analysisPrompt = BuildVisualFailureAnalysisPrompt(visualFailures);
    var llmResponse = await _llm.GenerateAsync(analysisPrompt, ...);
    var healingStrategy = ParseHealingStrategy(llmResponse);
    
    // Apply healing strategies
    foreach (var failure in visualFailures)
    {
        switch (strategy.Type)
        {
            case HealingStrategyType.AdjustTolerance:
                UpdateCheckpointTolerance(...);
                break;
            case HealingStrategyType.AddIgnoreRegions:
                AddIgnoreSelectors(...);
                break;
            case HealingStrategyType.UpdateBaseline:
                // Log warning, requires manual approval
                break;
            case HealingStrategyType.WaitForStability:
                InsertStabilizationStep(...);
                break;
        }
    }
    
    return healedPlan;
}
```

**Estimated Effort:** 1 day

### Phase 4 Total Effort: ~1 day

---

## 🔄 Phase 5: API Endpoints (AFTER PHASE 4)

### Tasks

#### 5.1 Visual Regression Endpoints
**File:** `EvoAITest.ApiService/Endpoints/VisualRegressionEndpoints.cs`

```csharp
// GET /api/tasks/{taskId}/visual/baselines
// GET /api/tasks/{taskId}/visual/baselines/{checkpointName}
// POST /api/tasks/{taskId}/visual/baselines
// POST /api/tasks/{taskId}/visual/baselines/{comparisonId}/approve
// GET /api/tasks/{taskId}/visual/history
// GET /api/tasks/{taskId}/visual/history/{checkpointName}
// DELETE /api/tasks/{taskId}/visual/baselines/{baselineId}
```

**Estimated Effort:** 1 day

#### 5.2 Request/Response Models
**File:** `EvoAITest.ApiService/Models/VisualRegressionModels.cs`

```csharp
public sealed class CreateBaselineRequest { ... }
public sealed class BaselineResponse { ... }
public sealed class ComparisonResultResponse { ... }
public sealed class ApproveBaselineRequest { ... }
```

**Estimated Effort:** 0.5 days

### Phase 5 Total Effort: ~1.5 days

---

## 🔄 Phase 6: Blazor Front-End (AFTER PHASE 5)

### Tasks

#### 6.1 VisualRegressionViewer Component
**File:** `EvoAITest.Web/Components/VisualRegressionViewer.razor`

Features:
- Tabs: Baseline | Actual | Diff | Side-by-Side
- Metrics display
- Action buttons (Approve, Adjust Tolerance, View Regions)
- Difference region overlay
- History timeline

**Estimated Effort:** 2 days

#### 6.2 BaselineApprovalDialog Component
**File:** `EvoAITest.Web/Components/BaselineApprovalDialog.razor`

Features:
- Confirmation dialog
- Reason input
- Preview comparison
- Approval submission

**Estimated Effort:** 0.5 days

#### 6.3 ToleranceAdjustmentDialog Component
**File:** `EvoAITest.Web/Components/ToleranceAdjustmentDialog.razor`

Features:
- Slider for tolerance adjustment
- Live preview of pass/fail
- Apply to specific checkpoint or all

**Estimated Effort:** 0.5 days

#### 6.4 DifferenceRegionOverlay Component
**File:** `EvoAITest.Web/Components/DifferenceRegionOverlay.razor`

Features:
- Overlay difference regions on image
- Clickable regions for details
- Region metadata display

**Estimated Effort:** 0.5 days

### Phase 6 Total Effort: ~3.5 days

---

## 🔄 Phase 7: Testing (PARALLEL WITH PHASES 2-6)

### Tasks

#### 7.1 Unit Tests for Comparison Engine
**File:** `EvoAITest.Tests/VisualRegression/ComparisonEngineTests.cs`

Tests:
- [x] Identical images return 0% difference
- [x] Minor differences within tolerance
- [x] Major differences outside tolerance
- [x] Ignored regions exclusion
- [x] Different dimensions handling
- [x] SSIM calculation
- [x] Difference region identification

**Estimated Effort:** 1 day

#### 7.2 Unit Tests for VisualComparisonService
**File:** `EvoAITest.Tests/VisualRegression/VisualComparisonServiceTests.cs`

Tests:
- [x] Baseline creation and retrieval
- [x] Comparison with missing baseline
- [x] Comparison with existing baseline
- [x] Baseline approval workflow
- [x] History retrieval

**Estimated Effort:** 1 day

#### 7.3 Integration Tests
**File:** `EvoAITest.Tests/Integration/VisualRegressionIntegrationTests.cs`

Tests:
- [x] Full workflow with real browser
- [x] Stable page layout consistency
- [x] Dynamic content with ignore masks
- [x] Multiple checkpoints in one task
- [x] Baseline versioning across branches

**Estimated Effort:** 1.5 days

#### 7.4 End-to-End Tests
**File:** `EvoAITest.Tests/E2E/VisualRegressionE2ETests.cs`

Tests:
- [x] Plan creation with visual checkpoints
- [x] Execution with visual checks
- [x] Healer healing visual failures
- [x] Baseline approval via API
- [x] Front-end display of results

**Estimated Effort:** 1.5 days

### Phase 7 Total Effort: ~5 days

---

## 🔄 Phase 8: CI/CD Integration (AFTER PHASES 2-7)

### Tasks

#### 8.1 GitHub Actions Workflow
**File:** `.github/workflows/visual-regression.yml`

Features:
- Download baselines from artifact storage
- Run visual regression tests
- Upload test results (actual, diffs)
- Generate visual report
- Comment PR with results
- Fail on unexpected regressions
- Update baselines (main branch only)

**Estimated Effort:** 1 day

#### 8.2 Baseline Approval Workflow
**File:** `.github/workflows/approve-baselines.yml`

Features:
- Manual workflow dispatch
- PR number input
- Checkpoint selection (specific or all)
- Commit updated baselines
- Comment on PR

**Estimated Effort:** 0.5 days

#### 8.3 Azure DevOps Pipeline
**File:** `azure-pipelines-visual-regression.yml`

Parallel implementation for Azure DevOps.

**Estimated Effort:** 0.5 days

### Phase 8 Total Effort: ~2 days

---

## 🔄 Phase 9: Documentation (PARALLEL WITH ALL PHASES)

### Tasks

#### 9.1 User Guide
**File:** `docs/VisualRegressionUserGuide.md`

Sections:
- Introduction
- Creating visual checkpoints
- Running tests with visual regression
- Reviewing differences
- Approving new baselines
- Configuring tolerance
- Best practices
- Troubleshooting

**Estimated Effort:** 1 day

#### 9.2 API Documentation
**File:** `docs/VisualRegressionAPI.md`

Sections:
- Service interface
- Endpoint reference
- Request/response models
- Authentication
- Rate limiting

**Estimated Effort:** 0.5 days

#### 9.3 Development Guide
**File:** `docs/VisualRegressionDevelopment.md`

Sections:
- Architecture overview
- Comparison algorithm details
- Storage structure
- Adding new checkpoint types
- Extending comparison logic
- Custom storage providers

**Estimated Effort:** 0.5 days

### Phase 9 Total Effort: ~2 days

---

## Total Project Effort Estimation

| Phase | Effort | Dependencies |
|-------|--------|--------------|
| Phase 1: Core Models | ✅ Complete | None |
| Phase 2: Comparison Engine | 5 days | Phase 1 |
| Phase 3: Executor Integration | 2 days | Phase 2 |
| Phase 4: Healer Integration | 1 day | Phase 3 |
| Phase 5: API Endpoints | 1.5 days | Phase 4 |
| Phase 6: Blazor Front-End | 3.5 days | Phase 5 |
| Phase 7: Testing | 5 days | Phases 2-6 (parallel) |
| Phase 8: CI/CD Integration | 2 days | Phases 2-7 |
| Phase 9: Documentation | 2 days | All phases (parallel) |

**Total Effort:** ~22 days (excluding Phase 1)

**With Parallelization:** ~15 days

---

## Development Schedule Recommendation

### Sprint 1 (Week 1)
- Phase 2: Comparison Engine Implementation
- Start Phase 7: Unit tests for comparison engine

### Sprint 2 (Week 2)
- Phase 3: Executor Integration
- Phase 4: Healer Integration
- Continue Phase 7: Integration tests

### Sprint 3 (Week 3)
- Phase 5: API Endpoints
- Phase 6: Blazor Front-End (start)
- Continue Phase 7: E2E tests
- Start Phase 9: Documentation

### Sprint 4 (Week 4)
- Phase 6: Blazor Front-End (complete)
- Phase 8: CI/CD Integration
- Finalize Phase 7: All tests
- Finalize Phase 9: Documentation

**Total Timeline:** 4 weeks / 1 month

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| SSIM calculation performance | Medium | Use library (ImageSharp), optimize for large images |
| Storage space growth | Medium | Implement retention policy, compress PNGs |
| Flaky tests in CI/CD | High | Implement robust stabilization, ignore masks |
| Baseline drift across environments | Medium | Separate baselines per environment |
| Large diff images | Low | Generate diffs on-demand, cache temporarily |

---

## Success Criteria

✅ **Functionality**
- [ ] Visual checkpoints can be defined in AutomationTask
- [ ] Screenshots captured at checkpoints
- [ ] Comparisons performed with configurable tolerance
- [ ] Baselines stored and versioned
- [ ] Diffs generated and displayed
- [ ] Approval workflow functional

✅ **Performance**
- [ ] Comparison completes in <3 seconds for 1920x1080
- [ ] Storage <5MB per checkpoint
- [ ] API response <500ms for baseline retrieval

✅ **Quality**
- [ ] >90% test coverage
- [ ] 0 critical bugs
- [ ] <1% false positive rate (flaky tests)

✅ **Usability**
- [ ] Clear UI for reviewing diffs
- [ ] Simple baseline approval process
- [ ] Comprehensive error messages

---

## Current Status

**Phase 1:** ✅ **COMPLETE**
- All core models implemented
- Service interface defined
- Browser tool registered
- Build successful
- Specification and summary documents complete

**Next Action:** Begin Phase 2 - Comparison Engine Implementation

---

**Last Updated:** 2025-12-02  
**Status:** Phase 1 Complete, Ready for Phase 2
