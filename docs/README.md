# EvoAITest Documentation

This directory contains comprehensive documentation for the EvoAITest framework.

> ?? **[Main Documentation Index](../DOCUMENTATION_INDEX.md)** - Start here for complete navigation

---

## ?? Directory Structure

```
docs/
??? README.md (this file)
??? archive/                         # Archived historical documentation
?   ??? implementation-steps/        # Step-by-step implementation notes
?   ??? llm-routing-docs/           # LLM routing planning docs
?   ??? recording-docs/             # Recording feature index (superseded)
?   ??? summary-files/              # Historical tracking files
?
??? Test Recording & Generation
?   ??? RECORDING_FEATURE.md        # Complete feature documentation
?   ??? RECORDING_QUICK_START.md    # 5-minute setup guide
?   ??? RECORDING_CHANGELOG.md      # Version history
?   ??? API_REFERENCE.md            # 13 REST endpoints
?   ??? ARCHITECTURE.md             # Technical architecture
?
??? LLM Integration
?   ??? LLM_INTEGRATION_GUIDE.md         # Complete guide (RECOMMENDED)
?   ??? LLM_ROUTING_COMPLETE_GUIDE.md    # Alternative comprehensive guide
?   ??? LLM_ROUTING_CONFIGURATION.md     # Configuration setup
?   ??? LLM_ROUTING_ARCHITECTURE.md      # Architecture details
?   ??? LLM_ROUTING_API_DESIGN.md        # API reference
?   ??? LLM_ROUTING_SPECIFICATION.md     # Feature specification
?
??? Visual Regression Testing
?   ??? VisualRegressionQuickStart.md    # 10-minute setup
?   ??? VisualRegressionUserGuide.md     # Complete user guide
?
??? Configuration & Setup
?   ??? KEY_VAULT_SETUP.md          # Azure Key Vault setup
?   ??? CHANGELOG.md                # Project-wide changelog
```

---

## ?? Quick Navigation

### Getting Started

| Audience | Start Here |
|----------|------------|
| **New Users** | [Documentation Index](../DOCUMENTATION_INDEX.md) ? [Recording Quick Start](RECORDING_QUICK_START.md) |
| **Developers** | [Documentation Index](../DOCUMENTATION_INDEX.md) ? [Architecture](ARCHITECTURE.md) |
| **DevOps** | [Key Vault Setup](KEY_VAULT_SETUP.md) ? [LLM Configuration](LLM_ROUTING_CONFIGURATION.md) |
| **QA Engineers** | [Recording Quick Start](RECORDING_QUICK_START.md) ? [Visual Regression Quick Start](VisualRegressionQuickStart.md) |

### By Feature

| Feature | Documentation |
|---------|---------------|
| **Test Recording** | [Feature Guide](RECORDING_FEATURE.md) \| [Quick Start](RECORDING_QUICK_START.md) \| [API](API_REFERENCE.md) |
| **LLM Integration** | [Integration Guide](LLM_INTEGRATION_GUIDE.md) \| [Configuration](LLM_ROUTING_CONFIGURATION.md) |
| **Visual Regression** | [Quick Start](VisualRegressionQuickStart.md) \| [User Guide](VisualRegressionUserGuide.md) |

---

## ?? Documentation by Type

### Quick Start Guides (5-15 minutes)
- [Recording Quick Start](RECORDING_QUICK_START.md) - Record and generate tests
- [Visual Regression Quick Start](VisualRegressionQuickStart.md) - Set up visual testing
- [LLM Integration Guide](LLM_INTEGRATION_GUIDE.md) - LLM provider setup

### Complete Feature Documentation
- [Recording Feature Guide](RECORDING_FEATURE.md) - Test recording and generation
- [Visual Regression User Guide](VisualRegressionUserGuide.md) - Visual testing
- [LLM Routing Complete Guide](LLM_ROUTING_COMPLETE_GUIDE.md) - Multi-model routing

### Technical Architecture
- [Recording Architecture](ARCHITECTURE.md) - Recording system design
- [LLM Routing Architecture](LLM_ROUTING_ARCHITECTURE.md) - LLM routing internals
- [LLM API Design](LLM_ROUTING_API_DESIGN.md) - Interfaces and models

### Configuration & Setup
- [Key Vault Setup](KEY_VAULT_SETUP.md) - Azure Key Vault configuration
- [LLM Configuration](LLM_ROUTING_CONFIGURATION.md) - LLM routing setup
- [Recording Configuration](RECORDING_FEATURE.md#configuration) - Recording settings

### API References
- [Recording API Reference](API_REFERENCE.md) - 13 REST endpoints
- [LLM API Design](LLM_ROUTING_API_DESIGN.md) - LLM provider interfaces

### Changelogs & History
- [Project Changelog](CHANGELOG.md) - Project-wide version history
- [Recording Changelog](RECORDING_CHANGELOG.md) - Recording feature versions

---

## ??? Archive

Historical documentation is preserved in the `archive/` directory:

- **implementation-steps/** - Step-by-step implementation notes (STEP_*.md files)
- **llm-routing-docs/** - LLM routing planning documentation
- **recording-docs/** - Superseded recording documentation index
- **summary-files/** - Historical tracking and summary files

These files are kept for historical reference but are not actively maintained.

---

## ?? Need Help?

- **Getting Started**: [Main Documentation Index](../DOCUMENTATION_INDEX.md)
- **Issues**: [GitHub Issues](https://github.com/VladyslavNap/EvoAITest/issues)
- **Examples**: [Login Example](../examples/LoginExample/README.md)

---

**Last Updated:** January 2026  
**Documentation Version:** 1.0
