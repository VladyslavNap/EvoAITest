# Changelog

All notable changes to EvoAITest will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-01-20

### Added - Dashboard Analytics Feature

#### Real-Time Execution Monitoring
- **Live Dashboard** - Real-time execution monitoring at `/execution-dashboard`
- **SignalR Integration** - Automatic updates when executions start/complete
- **Active Execution Tracking** - Watch tasks execute in real-time with progress bars
- **Execution Metrics** - Comprehensive KPIs (active count, success rates, durations)
- **System Health Monitoring** - Real-time health status with error rates and uptime

#### Analytics API Endpoints (11 new endpoints)
- `GET /api/execution-analytics/dashboard` - Comprehensive dashboard analytics
- `GET /api/execution-analytics/active` - Currently active executions
- `GET /api/execution-analytics/time-series` - Historical trend data
- `GET /api/execution-analytics/tasks/{id}/metrics` - Task-specific metrics
- `GET /api/execution-analytics/health` - System health metrics
- `GET /api/execution-analytics/top-executed` - Most executed tasks
- `GET /api/execution-analytics/top-failing` - Top failing tasks
- `GET /api/execution-analytics/slowest` - Slowest tasks
- `GET /api/execution-analytics/trends` - Execution trends analysis
- `POST /api/execution-analytics/calculate-time-series` - Trigger aggregation

#### Data Models
- **ExecutionMetrics** - Real-time execution state tracking
- **DashboardAnalytics** - Comprehensive analytics aggregation
- **TimeSeriesDataPoint** - Historical metric data with intervals
- **SystemHealthMetrics** - Health monitoring and status
- **ExecutionTrends** - Trend analysis with direction indicators

#### Services
- **AnalyticsService** - Core analytics logic and metric calculation
- **ExecutionTrackingService** - Coordinates analytics and SignalR broadcasts
- **AnalyticsBroadcastService** - Background service for periodic updates

#### UI Components
- **ExecutionDashboard.razor** - Main dashboard page with SignalR integration
- **MetricCard.razor** - KPI display with trend indicators
- **ActiveExecutionsList.razor** - Real-time task progress list
- **ExecutionChart.razor** - Time series chart visualization
- **TaskSummaryTable.razor** - Task metrics table

#### SignalR Hub Extensions
- Extended AnalyticsHub with execution tracking methods
- Real-time event broadcasting (ExecutionStarted, ExecutionProgress, ExecutionCompleted)
- Dashboard analytics updates
- Active executions updates
- System health updates

#### Database
- New tables: `ExecutionMetrics`, `TimeSeriesData`
- Indexes for performance optimization
- Entity configurations and relationships
- Migration: `AddExecutionAnalytics`

#### Documentation
- [Dashboard Analytics Guide](docs/DASHBOARD_ANALYTICS.md) - Complete feature documentation
- API documentation updates
- Integration examples (C#, JavaScript, HTTP)
- Architecture diagrams

#### Tests
- 18 comprehensive integration tests
- API endpoint coverage
- SignalR real-time updates testing
- Performance and load testing
- Metric calculation accuracy tests

### Changed
- Updated README.md with Dashboard Analytics feature
- Updated architecture diagram to include analytics components
- Updated API endpoint count (13 → 24+)
- Enhanced CORS configuration for SignalR
- Improved documentation structure

### Technical Details
- **.NET 10** compatibility
- **C# 14.0** features (primary constructors, collection expressions)
- **SignalR** for real-time communication
- **Chart.js** for visualization
- **Entity Framework Core** for persistence
- **FluentAssertions** for testing

## [1.0.0] - 2025-01-15

### Added - Test Recording Feature

#### Browser Interaction Recording
- **Real-time Recording** - Capture user interactions as they happen
- **15 Action Types** - Click, type, navigate, select, hover, and more
- **15 Intent Types** - Login, search, form fill, data entry, etc.
- **AI-Powered Intent Detection** - 90%+ accuracy using Azure OpenAI

#### Test Code Generation
- **Multiple Frameworks** - xUnit, NUnit, MSTest support
- **Multiple Languages** - C#, JavaScript, TypeScript, Python
- **16 Assertion Types** - Smart assertions for common scenarios
- **Page Object Model** - Optional POM generation
- **Production-Ready Code** - Clean, maintainable test code

#### Recording Management
- **Session Persistence** - Save and resume recording sessions
- **Interaction Editing** - Modify recorded interactions
- **Test Execution** - Run generated tests directly
- **Metrics Tracking** - Execution results and statistics

#### Visual Regression Testing
- **Baseline Management** - Create and manage visual baselines
- **Pixel-Perfect Comparison** - Detect visual differences
- **Diff Visualization** - Highlight changes with overlays
- **Multi-Environment Support** - Different baselines per environment
- **Git Integration** - Track baseline changes

#### AI-Powered Features
- **Azure OpenAI Integration** - GPT-4 for intelligent analysis
- **Ollama Support** - Local open-source models
- **Intelligent Routing** - Automatic model selection
- **Circuit Breaker** - Automatic failover
- **Cost Optimization** - Smart routing reduces costs 40-60%

#### Browser Automation
- **25 Built-in Tools** - Comprehensive browser operations
- **Self-Healing** - Automatic recovery from failures
- **Mobile Emulation** - 19 device presets
- **Network Interception** - Mock APIs and monitor traffic
- **Geolocation Testing** - Simulate GPS coordinates

#### Enterprise Features
- **Azure Key Vault** - Secure secret management
- **OpenTelemetry** - Built-in observability
- **.NET Aspire** - Cloud-native orchestration
- **SQL Server** - Persistent data storage

#### API Endpoints (13 endpoints)
- Task Management (CRUD operations)
- Recording Sessions (Create, Start, Stop, Resume)
- Test Generation (Generate code, execute tests)
- Visual Regression (Baseline management, comparison)

#### Documentation
- [Test Recording Quick Start](docs/RECORDING_QUICK_START.md)
- [Visual Regression Quick Start](docs/VisualRegressionQuickStart.md)
- [LLM Integration Guide](docs/LLM_INTEGRATION_GUIDE.md)
- [API Reference](docs/API_REFERENCE.md)
- [Architecture](docs/ARCHITECTURE.md)

#### Tests
- Comprehensive unit tests
- Integration tests with WebApplicationFactory
- End-to-end test scenarios
- 90%+ code coverage

### Initial Release Features
- **.NET 10** runtime and framework
- **Blazor** interactive web UI
- **Playwright** browser automation
- **Entity Framework Core** database access
- **Azure Aspire** orchestration
- **SignalR** for LLM streaming

---

## Version History

- **1.1.0** (2025-01-20) - Dashboard Analytics Feature
- **1.0.0** (2025-01-15) - Test Recording Feature (Initial Release)

---

## Upgrade Guide

### From 1.0.0 to 1.1.0

1. **Update NuGet Packages**
   ```bash
   dotnet restore
   ```

2. **Run Database Migration**
   ```bash
   cd EvoAITest.Core
   dotnet ef migrations add AddExecutionAnalytics
   dotnet ef database update
   ```

3. **Add Chart.js to Your Layout**
   
   Add to `_Host.cshtml` or `App.razor` (before closing `</body>` tag):
   ```html
   <script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js" 
           integrity="sha384-5VlZQ8m3XdRh9Zx0nLOBPRxXjqQDV8BhqQ7KhFmPDtYVNJMPNjHjEA8F8nQJVJ0P" 
           crossorigin="anonymous"></script>
   <script>
     const chartInstances = {};
     
     function renderChart(canvasId, type, data, options) {
       const ctx = document.getElementById(canvasId);
       if (!ctx) return;
       
       // Destroy existing chart if it exists
       if (chartInstances[canvasId]) {
         chartInstances[canvasId].destroy();
       }
       
       chartInstances[canvasId] = new Chart(ctx, { type, data, options });
     }
     
     function destroyChart(canvasId) {
       if (chartInstances[canvasId]) {
         chartInstances[canvasId].destroy();
         delete chartInstances[canvasId];
       }
     }
   </script>
   ```

4. **Update Configuration** (Optional)
   
   No configuration changes required. Analytics are enabled by default.

5. **Access New Features**
   - Navigate to `/execution-dashboard` for real-time monitoring
   - Use new API endpoints at `/api/execution-analytics/*`
   - Connect to SignalR hub at `/hubs/analytics`

### Breaking Changes

None. This release is fully backward compatible.

### Deprecations

None.

---

## Roadmap

### Planned for 1.2.0 (Q1 2025)
- Custom dashboard configuration
- Alert rules and notifications
- Slack/Teams integration
- Mobile app
- Export functionality (CSV, Excel, PDF)

### Planned for 1.3.0 (Q2 2025)
- Predictive analytics with ML
- Comparative analysis tools
- Historical execution replay
- Advanced filtering and search
- Custom metric definitions

### Planned for 2.0.0 (Q3 2025)
- Multi-tenant support
- Role-based access control (RBAC)
- API rate limiting
- Webhook integrations
- GraphQL API

---

## Contributing

See our [Documentation Index](DOCUMENTATION_INDEX.md) for contribution guidelines.

## Support

- **Issues**: [GitHub Issues](https://github.com/VladyslavNap/EvoAITest/issues)
- **Discussions**: [GitHub Discussions](https://github.com/VladyslavNap/EvoAITest/discussions)
- **Documentation**: [Complete Documentation](DOCUMENTATION_INDEX.md)

---

**Keep track of all changes and updates to EvoAITest!**
