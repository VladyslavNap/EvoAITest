# Week 2: Analytics Dashboard + Historical Tracking - Gap Analysis

**Date:** January 2026  
**Branch:** AnalyticsDashboard  
**Status:** Step 1 Complete - Assessment Phase

---

## Executive Summary

The EvoAITest project already has a **solid analytics foundation** with comprehensive services, models, and UI components. However, several key features required for "Week 2: Analytics Dashboard + Historical Tracking" are missing or incomplete.

### Overall Status: 60% Complete ✅

- ✅ **Data Models**: Complete (FlakyTestAnalysis, TestTrend, DashboardStatistics)
- ✅ **Database Schema**: Migration exists (20250102000001_AddAnalyticsTables)
- ✅ **Core Services**: TestAnalyticsService, FlakyTestDetectorService implemented
- ✅ **API Layer**: 11 endpoints in AnalyticsController
- ✅ **SignalR Hub**: Real-time updates infrastructure ready
- ⚠️ **Background Processing**: Missing scheduled jobs for trend persistence
- ⚠️ **Cache Invalidation**: No auto-invalidation after executions
- ⚠️ **Historical Persistence**: Trends computed on-demand, not persisted
- ⚠️ **Advanced UI**: Filters, drill-down, comparison views incomplete
- ❌ **Alerts/Notifications**: No health alert system
- ❌ **Data Retention**: No cleanup/archival policy

---

## Current Implementation Analysis

### 1. Database & Models ✅ **COMPLETE**

**Tables Implemented:**
- `FlakyTestAnalyses` - Stores flaky test detection results
- `TestTrends` - Designed for historical trend storage
- `TestExecutionResults` - Test execution data
- `TestExecutionSessions` - Session-level execution data
- `RecordingSessions` - Recording metadata

**Indexes:**
```
FlakyTestAnalyses: 5 indexes (RecordingSessionId, Severity, Score, AnalyzedAt, composite)
TestTrends: 6 indexes (Timestamp, RecordingSessionId, Interval, composites)
TestExecutionResults: 5 indexes (RecordingSessionId, Status, StartedAt, composites)
```

**✅ Strengths:**
- Well-indexed for analytics queries
- Comprehensive flaky test tracking
- Trend table supports multiple intervals (hourly/daily/weekly/monthly)
- JSON columns for extensibility (Patterns, Metrics, Metadata)

**❌ Gaps:**
- No retention/archival fields (e.g., `ArchivedAt`, `RetentionDays`)
- Missing audit trail for manual baseline updates
- No table for alert configurations/history

---

### 2. Core Services ✅ **85% COMPLETE**

#### **TestAnalyticsService** (EvoAITest.Agents)
**Implemented Methods:**
- `GetDashboardStatisticsAsync()` - Comprehensive dashboard data
- `CalculateTrendsAsync()` - On-demand trend calculation
- `GetRecordingTrendsAsync()` - Recording-specific trends
- `GetRecordingInsightsAsync()` - Detailed recording analysis
- `GetTopFailingTestsAsync()` - Top 10-50 failing tests
- `GetSlowestTestsAsync()` - Performance bottlenecks
- `GetMostExecutedTestsAsync()` - Usage patterns
- `DetermineHealth()` - Overall suite health scoring

**✅ Strengths:**
- Rich analytics with 8 major methods
- Integrates FlakyTestDetector for flakiness analysis
- Health scoring logic (Excellent/Good/Fair/Poor)
- Time-window filtering (24h, 7d, 30d)

**❌ Gaps:**
- `SaveTrendsAsync()` declared in interface but **NOT IMPLEMENTED**
- `SaveFlakyTestAnalysisAsync()` declared but **NOT IMPLEMENTED**
- `GetHistoricalTrendsAsync()` declared but **NOT IMPLEMENTED**
- No background job trigger for scheduled recalculation
- No cache invalidation hooks

---

#### **FlakyTestDetectorService** (EvoAITest.Agents)
**Implemented:**
- Pattern detection (time-of-day, day-of-week, sequence-dependent)
- Severity classification (Critical/High/Medium/Low)
- Root cause analysis
- Stability metrics calculation

**✅ Strengths:**
- Advanced ML-like pattern detection
- Configurable flakiness criteria
- Comprehensive recommendations

**❌ Gaps:**
- No integration with alerting system
- Results not automatically persisted after detection

---

#### **AnalyticsCacheService** (EvoAITest.Agents)
**Implemented:**
- In-memory caching (IMemoryCache)
- Separate TTLs for different data types:
  - Dashboard: 30 seconds
  - Flaky Tests: 5 minutes
  - Trends: 10 minutes
- Manual invalidation methods

**✅ Strengths:**
- Tiered caching strategy
- Priority-based eviction (High/Normal/Low)

**❌ Gaps:**
- **No automatic invalidation after test execution**
- No distributed cache support (Redis) for multi-instance deployments
- Cache statistics not tracked/exposed
- No warming strategy for cold starts

---

### 3. API Layer ✅ **90% COMPLETE**

#### **AnalyticsController** (EvoAITest.ApiService)
**Implemented Endpoints:**

| Method | Endpoint | Response Cache | Status |
|--------|----------|----------------|--------|
| GET | `/api/analytics/dashboard` | 30s | ✅ |
| GET | `/api/analytics/flaky-tests` | 300s | ✅ |
| GET | `/api/analytics/trends` | 300s | ✅ |
| GET | `/api/analytics/recordings/{id}/insights` | None | ✅ |
| GET | `/api/analytics/recordings/{id}/trends` | None | ✅ |
| GET | `/api/analytics/top-failing` | None | ✅ |
| GET | `/api/analytics/slowest` | None | ✅ |
| GET | `/api/analytics/most-executed` | None | ✅ |
| POST | `/api/analytics/export/csv` | None | ✅ |
| POST | `/api/analytics/export/json` | None | ✅ |
| POST | `/api/analytics/export/pdf` | None | ✅ |

**✅ Strengths:**
- 11 comprehensive endpoints
- Response caching with VaryByQueryKeys
- Validation (count: 1-50, days: 1-365)
- Export to CSV/JSON/PDF

**❌ Gaps:**
- No pagination for large result sets (trends, flaky tests)
- Missing `/api/analytics/health-score` endpoint
- No `/api/analytics/compare` for period-over-period comparison
- Missing `/api/analytics/alerts` endpoints (create/list/acknowledge)
- No ETag support for conditional requests
- No rate limiting on export endpoints

---

### 4. SignalR Hub ✅ **75% COMPLETE**

#### **AnalyticsHub** (EvoAITest.ApiService)
**Implemented:**
- Group subscriptions (Dashboard, Recording_{id})
- Extension methods for broadcasting:
  - `SendDashboardUpdate()`
  - `SendFlakyTestDetected()`
  - `SendTestExecutionCompleted()`
  - `SendTrendCalculated()`

**✅ Strengths:**
- Clean group-based subscriptions
- Strongly-typed extension methods
- Connection lifecycle logging

**❌ Gaps:**
- **No AnalyticsBroadcastService to trigger broadcasts**
- No integration with test execution pipeline (no auto-broadcast after test completion)
- Missing `SendHealthScoreChanged()` notification
- No reconnection strategy/documentation for clients
- No authentication/authorization checks

---

### 5. Blazor UI ✅ **70% COMPLETE**

#### **AnalyticsDashboard.razor** (EvoAITest.Web)
**Implemented:**
- Real-time connection status indicator
- Health banner with color-coded status
- 6 stat cards (Executions, Pass Rate, Tests, Flaky, Avg Duration, Recent)
- Simple trend chart (last 30 days)
- 3 top lists (Failing, Slowest, Most Executed)
- Navigation to recording details
- Refresh button

**✅ Strengths:**
- Clean, responsive design
- SignalR integration via `AnalyticsHubClient`
- Real-time updates working
- Color-coded health indicators

**❌ Gaps:**
- **No date range filter** (hardcoded to 30 days)
- **No recording filter dropdown**
- **No drill-down to individual test details**
- No comparison views (week-over-week, month-over-month)
- No export buttons (CSV/PDF)
- Chart is basic (no interactive tooltips, zoom, pan)
- No loading skeleton (only spinner)
- Missing:
  - Test stability timeline view
  - Flaky test details modal
  - Performance degradation alerts UI
  - Custom dashboard widgets/layout

---

### 6. Background Processing ❌ **0% COMPLETE**

**What's Missing:**
- No `AnalyticsPersistenceHostedService` for scheduled trend persistence
- No scheduled job to:
  - Calculate and persist hourly/daily/weekly trends
  - Refresh flaky test analysis
  - Rebuild caches
  - Send health alerts
  - Cleanup old data (retention policy)
- No integration with Aspire orchestration for background tasks

**Required Implementation:**
```csharp
public sealed class AnalyticsPersistenceHostedService : BackgroundService
{
    // Every hour: Calculate and persist trends
    // Every 6 hours: Rebuild flaky test analysis
    // Every 24 hours: Cleanup old data
    // On health change: Send alerts
}
```

---

### 7. Cache Invalidation ❌ **NOT IMPLEMENTED**

**What's Missing:**
- No hook in test execution pipeline to call `AnalyticsCacheService.InvalidateDashboard()`
- No event-driven invalidation when:
  - New test execution completes
  - Flaky test detected
  - Baseline updated
  - Recording modified

**Where to Integrate:**
- `ExecutionOrchestrator.ExecuteAsync()` completion
- `TestExecutionService` after persisting results
- `FlakyTestDetectorService` after analysis

---

### 8. Historical Data Persistence ⚠️ **50% COMPLETE**

**Current State:**
- `TestTrend` table exists with proper indexes
- `CalculateTrendsAsync()` computes trends on-demand
- `SaveTrendsAsync()` interface method exists but **NOT IMPLEMENTED**
- Trends are **NOT persisted to database**

**Impact:**
- Every dashboard load recalculates trends (slow for large datasets)
- No historical comparison possible
- Cache misses result in expensive queries

**Required:**
```csharp
public async Task SaveTrendsAsync(
    IEnumerable<TestTrend> trends,
    CancellationToken cancellationToken = default)
{
    // 1. Check for existing trends (avoid duplicates)
    // 2. Bulk insert new trends
    // 3. Update CalculatedAt timestamp
    // 4. Return count of persisted trends
}
```

---

### 9. Alerts & Notifications ❌ **0% COMPLETE**

**What's Missing:**
- No alert configuration table (`AlertRules`, `AlertHistory`)
- No alert triggers:
  - Pass rate drops below threshold
  - Flaky test count spike
  - Performance degradation
  - Test suite health changes
- No notification channels:
  - Email
  - Slack/Teams webhooks
  - In-app notifications
- No throttling/rate limiting for alerts

**Required Tables:**
```sql
CREATE TABLE AlertRules (
    Id GUID PRIMARY KEY,
    Name NVARCHAR(200),
    Condition NVARCHAR(500), -- JSON: { "metric": "passRate", "operator": "<", "threshold": 80 }
    Severity NVARCHAR(50),
    Enabled BIT,
    ThrottleMinutes INT
);

CREATE TABLE AlertHistory (
    Id GUID PRIMARY KEY,
    RuleId GUID,
    TriggeredAt DATETIMEOFFSET,
    Severity NVARCHAR(50),
    Message NVARCHAR(MAX),
    Acknowledged BIT
);
```

---

### 10. Data Retention ❌ **NOT IMPLEMENTED**

**What's Missing:**
- No retention policy configuration
- No cleanup service for:
  - Old TestExecutionResults (keep last 90 days?)
  - Archived TestTrends (aggregate to weekly/monthly after 90 days?)
  - Old FlakyTestAnalyses (keep last 10 analyses per test?)
- No archival strategy (move to cold storage)

**Required:**
```csharp
public sealed class DataRetentionService : BackgroundService
{
    // Daily: Check retention policies
    // Archive data older than retention period
    // Aggregate daily trends into weekly after 90 days
    // Aggregate weekly trends into monthly after 1 year
}
```

---

## Priority Gap Matrix

| Gap | Priority | Effort | Impact | Notes |
|-----|----------|--------|--------|-------|
| **Background Processing** | 🔴 CRITICAL | High (3 days) | Very High | Enables trend persistence |
| **Historical Persistence** | 🔴 CRITICAL | Medium (1 day) | Very High | Core Week 2 goal |
| **Cache Invalidation Hooks** | 🟡 HIGH | Low (1 day) | High | Performance critical |
| **Dashboard Filters** | 🟡 HIGH | Medium (2 days) | High | UX improvement |
| **Alerts System** | 🟡 HIGH | High (3 days) | High | Proactive monitoring |
| **Drill-down Navigation** | 🟢 MEDIUM | Medium (2 days) | Medium | UX enhancement |
| **Comparison Views** | 🟢 MEDIUM | Medium (2 days) | Medium | Advanced analytics |
| **Data Retention** | 🟢 MEDIUM | Medium (2 days) | Medium | Long-term stability |
| **Export UI Buttons** | 🟢 LOW | Low (0.5 days) | Low | Nice-to-have |
| **Chart Enhancements** | 🟢 LOW | High (3 days) | Low | Polish |

---

## Recommended Implementation Order

### **Phase 1: Core Historical Tracking** (Days 1-2)
1. ✅ Implement `SaveTrendsAsync()` and `SaveFlakyTestAnalysisAsync()`
2. ✅ Create `AnalyticsPersistenceHostedService` for scheduled trend calculation
3. ✅ Add cache invalidation hooks in execution pipeline
4. ✅ Test end-to-end: Execute test → Trend persisted → Cache invalidated → Dashboard updates

### **Phase 2: UI Enhancements** (Days 3-4)
5. ✅ Add date range filter to dashboard
6. ✅ Add recording filter dropdown
7. ✅ Implement drill-down navigation (test details modal)
8. ✅ Add export buttons (CSV/JSON) to dashboard UI

### **Phase 3: Advanced Features** (Days 5-6)
9. ✅ Implement comparison views (week-over-week, month-over-month)
10. ✅ Create alert system (tables + service + API)
11. ✅ Add health score change notifications
12. ✅ Integrate alerts with SignalR

### **Phase 4: Production Readiness** (Day 7)
13. ✅ Implement data retention policy
14. ✅ Add pagination to large result endpoints
15. ✅ Performance testing (load 100k+ test results)
16. ✅ Documentation update

---

## Testing Gaps

**Missing Tests:**
- ❌ No tests for trend persistence logic
- ❌ No tests for background job execution
- ❌ No tests for cache invalidation
- ❌ No bUnit tests for dashboard filters
- ❌ No integration tests for SignalR broadcasting
- ❌ No load tests for analytics queries

**Required Test Coverage:**
```
TestAnalyticsService: 85% → 95%
AnalyticsCacheService: 70% → 90%
AnalyticsController: 80% → 95%
AnalyticsDashboard.razor: 0% → 70%
Background Services: 0% → 80%
```

---

## Migration Needs

**No new migrations required** for Phase 1-2.

**Phase 3 requires migration:**
```csharp
public partial class AddAlertingTables : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create AlertRules table
        // Create AlertHistory table
        // Add indexes
    }
}
```

**Phase 4 requires migration:**
```csharp
public partial class AddRetentionFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add ArchivedAt column to TestExecutionResults
        // Add RetentionPolicy column to RecordingSessions
        // Add indexes for archival queries
    }
}
```

---

## Configuration Gaps

**Missing Configuration:**
```json
{
  "Analytics": {
    "Persistence": {
      "Enabled": true,
      "IntervalMinutes": 60,
      "BatchSize": 1000
    },
    "Cache": {
      "DashboardDurationSeconds": 30,
      "TrendDurationMinutes": 10,
      "DistributedCache": {
        "Enabled": false,
        "Provider": "Redis",
        "ConnectionString": "..."
      }
    },
    "Alerts": {
      "Enabled": true,
      "EmailEnabled": false,
      "SlackWebhookUrl": "",
      "ThrottleMinutes": 60,
      "PassRateThreshold": 80.0,
      "FlakinessThreshold": 50.0
    },
    "Retention": {
      "Enabled": true,
      "ExecutionResultsDays": 90,
      "TrendAggregationDays": 90,
      "ArchiveToBlob": false
    }
  }
}
```

---

## Summary: What Needs to Be Built

### **CRITICAL (Must Have for Week 2)**
1. ✅ **AnalyticsPersistenceHostedService** - Background job for trend persistence
2. ✅ **SaveTrendsAsync implementation** - Persist calculated trends to database
3. ✅ **SaveFlakyTestAnalysisAsync implementation** - Persist flaky test analysis
4. ✅ **Cache invalidation hooks** - Auto-invalidate after test execution
5. ✅ **Dashboard filters** - Date range + recording filter
6. ✅ **GetHistoricalTrendsAsync implementation** - Query persisted trends efficiently

### **HIGH (Should Have)**
7. ✅ **Alert system foundation** - Tables + basic service
8. ✅ **Drill-down navigation** - Test details modal from dashboard cards
9. ✅ **Export UI buttons** - Trigger CSV/JSON export from dashboard

### **MEDIUM (Nice to Have)**
10. ✅ **Comparison views** - Week-over-week, month-over-month charts
11. ✅ **Data retention service** - Cleanup old data
12. ✅ **Pagination** - For large trend/flaky test lists

### **LOW (Future Enhancements)**
13. ⏳ **Chart library upgrade** - Replace simple chart with Chart.js/Blazor Charts
14. ⏳ **Custom dashboard layouts** - User-configurable widgets
15. ⏳ **Distributed caching** - Redis integration for multi-instance scaling

---

## Risks & Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Performance degradation with 100k+ results** | High | Implement pagination, add `LIMIT` clauses, optimize indexes |
| **Background job conflicts in multi-instance** | Medium | Add distributed lock (Redis, SQL), job idempotency |
| **Cache invalidation storms** | Medium | Implement debouncing, rate limiting |
| **SignalR connection drops** | Low | Implement reconnection logic, heartbeat |
| **Large export files timeout** | Low | Stream results, add progress indicator |

---

## Conclusion

The EvoAITest analytics infrastructure is **well-architected** but incomplete for Week 2 goals. The main gaps are:

1. **Historical Persistence** - Trends calculated but not saved
2. **Background Processing** - No scheduled jobs
3. **Cache Management** - No auto-invalidation
4. **Advanced UI** - Missing filters and drill-down

**Estimated Effort to Complete Week 2 Goals:** 5-7 days

**Current Status:** 60% → Target: 100%

---

**Next Steps:** Proceed to Step 2 - Design data model changes and migration plan for alert system + retention policy.
