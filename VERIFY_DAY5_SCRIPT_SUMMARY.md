# Day 5 Verification Script – Summary

The PowerShell script at `scripts/verify-day5.ps1` remains the single command that validates the Day 5 baseline. All procedural guidance, troubleshooting steps, and command examples now live in [scripts/README-verify-day5.md](scripts/README-verify-day5.md); this file only captures the high-level intent to avoid duplicating that content.

## Purpose
- Confirm local prerequisites (.NET 10, PowerShell 7+, optional Docker).
- Validate Azure authentication, Key Vault access, and the `LLMAPIKEY` secret when Azure checks are enabled.
- Ensure Ollama is reachable for local development when requested.
- Build the solution, run unit tests, and scan for common misconfigurations.

## When to Run
- Before handing the repo to a new contributor.
- As part of CI smoke tests (use `-SkipAzure -SkipOllama` in hosted pipelines).
- Prior to provisioning Phase 1 infrastructure.

## Key Flags
- `-SkipAzure` – skip Azure CLI, Key Vault, and endpoint checks.
- `-SkipOllama` – skip local Ollama verification.
- `-SkipAzure -SkipOllama` – reduce to build/test/secret scan only (typical for CI).

## Related Documentation
- [scripts/README-verify-day5.md](scripts/README-verify-day5.md) – full usage guide and troubleshooting matrix.
- [EVOAITEST_CORE_UNIT_TESTS_SUMMARY.md](EVOAITEST_CORE_UNIT_TESTS_SUMMARY.md) – what the test suite covers.
- [EVOAITEST_CORE_CONFIGURATION_GUIDE.md](EVOAITEST_CORE_CONFIGURATION_GUIDE.md) – context for the configuration checks performed by the script.
