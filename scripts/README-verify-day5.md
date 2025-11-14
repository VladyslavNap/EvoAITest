# Day 5 Verification Script - Usage Guide

## Overview
The `verify-day5.ps1` script validates the complete EvoAITest Day 5 configuration, including Azure OpenAI (GPT-5), Azure Key Vault, and local Ollama support.

## Prerequisites

### Required
- **.NET 10 SDK** - https://dotnet.microsoft.com/
- **PowerShell 7+** - https://aka.ms/powershell
- **Azure CLI** - https://aka.ms/azure-cli
- **Azure Account** - Logged in with `az login`

### Optional
- **Docker** - For local Aspire dashboard
- **Ollama** - For local development with open-source models

## Quick Start

### Windows (PowerShell)
```powershell
cd C:\Users\vxn20a\source\repos\EvoAITest
.\scripts\verify-day5.ps1
```

### Linux/macOS (PowerShell)
```bash
cd ~/source/repos/EvoAITest
pwsh ./scripts/verify-day5.ps1
```

### Command Line Options
```powershell
# Skip Ollama check (for CI/CD or production-only validation)
.\scripts\verify-day5.ps1 -SkipOllama

# Skip Azure validation (for local development without Azure access)
.\scripts\verify-day5.ps1 -SkipAzure

# Skip both (minimal validation)
.\scripts\verify-day5.ps1 -SkipOllama -SkipAzure
```

## What the Script Checks

### ? Prerequisites
- .NET 10 SDK installed and version
- Docker installed (optional)
- PowerShell version (7+ recommended)

### ? Azure Configuration
- Azure CLI installed and authenticated
- Current subscription details
- Azure Key Vault access (evoai-keyvault)
- LLMAPIKEY secret existence
- User permissions

### ? Azure OpenAI Setup
- AZURE_OPENAI_ENDPOINT environment variable
- Endpoint URL format validation
- Expected endpoint: `https://youropenai.cognitiveservices.azure.com`
- Warning if API key in environment (should use Key Vault)

### ? Ollama (Local Development)
- Ollama server running at http://localhost:11434
- Available models list
- qwen2.5-7b availability check

### ? Build & Tests
- Solution builds successfully (Release configuration)
- All unit tests pass
- Build warnings reported

### ? Code Configuration
- EvoAITestCoreOptions has Azure properties
- ServiceCollectionExtensions loads configuration
- No hardcoded API keys in source code

### ? Environment Variables
- EVOAITEST__CORE__LLMPROVIDER configuration
- Aspire-style variable format

## Expected Output

### Success (All Checks Pass)
```
?????????????????????????????????????????????????????????????????
?                    ? ALL CHECKS PASSED ?                    ?
?????????????????????????????????????????????????????????????????

? Solution builds successfully
? All unit tests pass
? Azure Key Vault accessible (LLMAPIKEY secret found)
? Azure OpenAI endpoint configured
? No hardcoded secrets in source code
? Ready for Azure Container Apps deployment

Next Steps:
  1. Run locally: cd EvoAITest.AppHost && dotnet run
  2. Deploy to Azure: az containerapp up --source .
  3. Monitor in Aspire Dashboard: http://localhost:15888
```

### Warnings (Some Issues)
```
?????????????????????????????????????????????????????????????????
?            ??  VERIFICATION COMPLETED WITH WARNINGS ??         ?
?????????????????????????????????????????????????????????????????

Warnings: 3

Review warnings above and address any issues.
The solution may still work, but warnings should be resolved.
```

### Failure (Critical Issues)
```
?????????????????????????????????????????????????????????????????
?               ? VERIFICATION FAILED ?                        ?
?????????????????????????????????????????????????????????????????

Failures: 2
Warnings: 1

Review errors above and fix issues before proceeding.

Common fixes:
  1. Install .NET 10 SDK: https://dotnet.microsoft.com/
  2. Login to Azure: az login
  3. Set environment variables (see above)
  4. Create Key Vault secret: az keyvault secret set ...
```

## Common Issues & Fixes

### Issue: .NET SDK Not Found
```
? .NET SDK not found. Install from https://dotnet.microsoft.com/
```
**Fix:**
```bash
# Download and install .NET 10 SDK
# Windows: https://dotnet.microsoft.com/download/dotnet/10.0
# Linux: sudo apt install dotnet-sdk-10.0
# macOS: brew install dotnet@10
```

### Issue: Not Logged Into Azure
```
? Not logged into Azure. Run: az login
```
**Fix:**
```bash
az login
az account show
```

### Issue: Key Vault Access Denied
```
? Cannot access Key Vault: evoai-keyvault
```
**Fix:**
```bash
# Get your principal ID
PRINCIPAL_ID=$(az ad signed-in-user show --query id -o tsv)

# Grant Key Vault Secrets User role
az role assignment create \
  --assignee $PRINCIPAL_ID \
  --role "Key Vault Secrets User" \
  --scope /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.KeyVault/vaults/evoai-keyvault
```

### Issue: LLMAPIKEY Secret Not Found
```
? Key Vault secret: LLMAPIKEY NOT FOUND
```
**Fix:**
```bash
# Create the secret in Key Vault
az keyvault secret set \
  --vault-name evoai-keyvault \
  --name LLMAPIKEY \
  --value "your-azure-openai-api-key-here"
```

### Issue: AZURE_OPENAI_ENDPOINT Not Set
```
? AZURE_OPENAI_ENDPOINT environment variable not set
```
**Fix:**
```powershell
# PowerShell (Windows)
$env:AZURE_OPENAI_ENDPOINT = "https://youropenai.cognitiveservices.azure.com"

# Bash (Linux/macOS)
export AZURE_OPENAI_ENDPOINT="https://youropenai.cognitiveservices.azure.com"

# Persistent (add to your profile or .env file)
```

### Issue: Ollama Not Running
```
??  Ollama not running at http://localhost:11434
```
**Fix:**
```bash
# Install Ollama
# Windows/macOS: Download from https://ollama.ai
# Linux: curl -fsSL https://ollama.ai/install.sh | sh

# Start Ollama
ollama serve

# Pull recommended model
ollama pull qwen2.5-7b
```

### Issue: Build Failures
```
? Solution build: FAILED
```
**Fix:**
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build --configuration Release
```

### Issue: Test Failures
```
? Unit tests: FAILED
```
**Fix:**
```bash
# Run tests with verbose output to see details
dotnet test --verbosity normal

# Run specific test class
dotnet test --filter "FullyQualifiedName~EvoAITestCoreOptionsTests"
```

## Environment Variable Setup

### Production (Azure OpenAI)
```powershell
# PowerShell
$env:EVOAITEST__CORE__LLMPROVIDER = "AzureOpenAI"
$env:AZURE_OPENAI_ENDPOINT = "https://youropenai.cognitiveservices.azure.com"

# Bash
export EVOAITEST__CORE__LLMPROVIDER="AzureOpenAI"
export AZURE_OPENAI_ENDPOINT="https://youropenai.cognitiveservices.azure.com"
```

### Development (Ollama)
```powershell
# PowerShell
$env:EVOAITEST__CORE__LLMPROVIDER = "Ollama"
$env:EVOAITEST__CORE__OLLAMAENDPOINT = "http://localhost:11434"

# Bash
export EVOAITEST__CORE__LLMPROVIDER="Ollama"
export EVOAITEST__CORE__OLLAMAENDPOINT="http://localhost:11434"
```

## CI/CD Integration

### GitHub Actions
```yaml
name: Day 5 Verification

on: [push, pull_request]

jobs:
  verify:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Install PowerShell
        run: |
          sudo apt-get update
          sudo apt-get install -y powershell
      
      - name: Run Verification Script
        run: pwsh ./scripts/verify-day5.ps1 -SkipAzure -SkipOllama
```

### Azure Pipelines
```yaml
steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'
  
  - task: PowerShell@2
    inputs:
      filePath: 'scripts/verify-day5.ps1'
      arguments: '-SkipAzure -SkipOllama'
    displayName: 'Day 5 Verification'
```

## Script Exit Codes

- **0** - Success (all checks passed or warnings only)
- **1** - Failure (one or more critical checks failed)

```powershell
# Check exit code
.\scripts\verify-day5.ps1
if ($LASTEXITCODE -eq 0) {
    Write-Host "Verification successful"
} else {
    Write-Host "Verification failed"
}
```

## Security Best Practices

### ? DO
- Store API keys in Azure Key Vault
- Use managed identity for authentication
- Use environment variables for non-sensitive config
- Run verification script before deployment
- Review script output for security warnings

### ? DON'T
- Hardcode API keys in source code
- Commit secrets to Git
- Store API keys in environment variables in production
- Skip verification script in CI/CD
- Ignore security warnings

## Troubleshooting

### Script Won't Run
```powershell
# Enable script execution (Windows)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Make script executable (Linux/macOS)
chmod +x scripts/verify-day5.ps1
```

### Script Errors Out Immediately
```powershell
# Check PowerShell version
$PSVersionTable.PSVersion

# Upgrade to PowerShell 7+
# Windows: winget install Microsoft.PowerShell
# Linux: https://aka.ms/powershell-release?tag=stable
```

### Can't Find Az Command
```bash
# Install Azure CLI
# Windows: winget install Microsoft.AzureCLI
# Linux: curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash
# macOS: brew install azure-cli
```

## Support & Resources

- **Azure OpenAI Documentation**: https://learn.microsoft.com/azure/ai-services/openai/
- **Azure Key Vault Documentation**: https://learn.microsoft.com/azure/key-vault/
- **Ollama Documentation**: https://ollama.ai/docs
- **.NET Aspire Documentation**: https://learn.microsoft.com/dotnet/aspire/
- **PowerShell Documentation**: https://learn.microsoft.com/powershell/

## Changelog

### Version 1.0 (Day 5)
- Initial release
- Azure OpenAI (GPT-5) validation
- Azure Key Vault access check
- Ollama support validation
- Build and test verification
- Hardcoded secret detection
- Environment variable validation

## License
This script is part of the EvoAITest project.
