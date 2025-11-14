#!/usr/bin/env pwsh
#Requires -Version 7.0

<#
.SYNOPSIS
    Verification script for EvoAITest Day 5 - Azure AI and Key Vault Configuration
.DESCRIPTION
    Validates Azure OpenAI (GPT-5) configuration, Key Vault access, local Ollama setup,
    and ensures no hardcoded secrets in the codebase.
.PARAMETER SkipOllama
    Skip Ollama availability check (useful for CI/CD pipelines)
.PARAMETER SkipAzure
    Skip Azure validation (useful for local development without Azure access)
.EXAMPLE
    .\verify-day5.ps1
    .\verify-day5.ps1 -SkipOllama
    .\verify-day5.ps1 -SkipAzure
#>

param(
    [switch]$SkipOllama,
    [switch]$SkipAzure
)

$ErrorActionPreference = "Continue"
$script:FailureCount = 0
$script:WarningCount = 0

# Color output functions
function Write-Success {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Green
}

function Write-Failure {
    param([string]$Message)
    Write-Host "? $Message" -ForegroundColor Red
    $script:FailureCount++
}

function Write-Warning {
    param([string]$Message)
    Write-Host "??  $Message" -ForegroundColor Yellow
    $script:WarningCount++
}

function Write-Info {
    param([string]$Message)
    Write-Host "??  $Message" -ForegroundColor Cyan
}

function Write-SectionHeader {
    param([string]$Title)
    Write-Host "`n=== $Title ===" -ForegroundColor Magenta
}

# Check if a command exists
function Test-CommandExists {
    param([string]$Command)
    $null -ne (Get-Command $Command -ErrorAction SilentlyContinue)
}

# Main verification script
Write-Host @"

?????????????????????????????????????????????????????????????????
?  EvoAITest Day 5 Verification Script                         ?
?  Azure OpenAI (GPT-5) + Key Vault + Ollama Support          ?
?????????????????????????????????????????????????????????????????

"@ -ForegroundColor Cyan

Write-SectionHeader "Checking Prerequisites"

# Check .NET SDK
if (Test-CommandExists "dotnet") {
    $dotnetVersion = dotnet --version
    if ($dotnetVersion -match "^10\.") {
        Write-Success ".NET SDK: $dotnetVersion (.NET 10)"
    } else {
        Write-Failure ".NET SDK: $dotnetVersion (Expected .NET 10.x)"
    }
} else {
    Write-Failure ".NET SDK not found. Install from https://dotnet.microsoft.com/"
}

# Check Docker
if (Test-CommandExists "docker") {
    try {
        $dockerVersion = docker --version
        Write-Success "Docker: $dockerVersion"
    } catch {
        Write-Warning "Docker installed but not running"
    }
} else {
    Write-Info "Docker not found (Optional: Required for local Aspire dashboard)"
}

# Check PowerShell version
$psVersion = $PSVersionTable.PSVersion
if ($psVersion.Major -ge 7) {
    Write-Success "PowerShell: $psVersion"
} else {
    Write-Warning "PowerShell: $psVersion (PowerShell 7+ recommended)"
}

# Azure CLI and Credentials Check
if (-not $SkipAzure) {
    Write-SectionHeader "Validating Azure Configuration"

    if (Test-CommandExists "az") {
        $azVersion = az version --output json | ConvertFrom-Json
        Write-Success "Azure CLI: $($azVersion.'azure-cli')"

        # Check if logged in
        try {
            $account = az account show --output json 2>$null | ConvertFrom-Json
            if ($account) {
                Write-Success "Azure Login: Authenticated as $($account.user.name)"
                Write-Info "Subscription: $($account.name) (ID: $($account.id))"
                Write-Info "Tenant: $($account.tenantId)"
            } else {
                Write-Failure "Not logged into Azure. Run: az login"
            }
        } catch {
            Write-Failure "Not logged into Azure. Run: az login"
        }
    } else {
        Write-Failure "Azure CLI not found. Install from https://aka.ms/azure-cli"
    }

    # Validate Key Vault Access
    Write-SectionHeader "Validating Azure Key Vault Access"

    $keyVaultName = "evoai-keyvault"
    $keyVaultUrl = "https://$keyVaultName.vault.azure.net"
    $secretName = "LLMAPIKEY"

    Write-Info "Key Vault: $keyVaultUrl"

    if (Test-CommandExists "az") {
        try {
            # Check if Key Vault exists and user has access
            $secrets = az keyvault secret list --vault-name $keyVaultName --output json 2>$null | ConvertFrom-Json
            
            if ($secrets) {
                Write-Success "Key Vault access: Can list secrets in $keyVaultName"
                
                # Check for LLMAPIKEY secret
                $llmApiKeySecret = $secrets | Where-Object { $_.name -eq $secretName }
                
                if ($llmApiKeySecret) {
                    Write-Success "Key Vault secret: $secretName found"
                    Write-Info "Secret ID: $($llmApiKeySecret.id)"
                    Write-Info "Enabled: $($llmApiKeySecret.attributes.enabled)"
                } else {
                    Write-Failure "Key Vault secret: $secretName NOT FOUND"
                    Write-Info "Create secret: az keyvault secret set --vault-name $keyVaultName --name $secretName --value '<YOUR_API_KEY>'"
                }
            } else {
                Write-Failure "Cannot access Key Vault: $keyVaultName"
                Write-Info "Ensure you have 'Key Vault Secrets User' role"
                Write-Info "Grant access: az role assignment create --assignee <your-principal-id> --role 'Key Vault Secrets User' --scope /subscriptions/<sub-id>/resourceGroups/<rg>/providers/Microsoft.KeyVault/vaults/$keyVaultName"
            }
        } catch {
            Write-Failure "Error accessing Key Vault: $_"
        }
    }

    # Validate Azure OpenAI Endpoint
    Write-SectionHeader "Validating Azure OpenAI Configuration"

    $azureOpenAIEndpoint = $env:AZURE_OPENAI_ENDPOINT
    $expectedEndpoint = "https://twazncopenai2.cognitiveservices.azure.com"

    if ($azureOpenAIEndpoint) {
        Write-Success "Environment Variable: AZURE_OPENAI_ENDPOINT is set"
        Write-Info "Endpoint: $azureOpenAIEndpoint"
        
        if ($azureOpenAIEndpoint -eq $expectedEndpoint) {
            Write-Success "Endpoint matches expected: $expectedEndpoint"
        } else {
            Write-Warning "Endpoint differs from expected: $expectedEndpoint"
        }

        # Validate URL format
        if ($azureOpenAIEndpoint -match "^https://.*\.cognitiveservices\.azure\.com$") {
            Write-Success "Endpoint format: Valid Azure OpenAI URL"
        } else {
            Write-Warning "Endpoint format: May not be a valid Azure OpenAI URL"
        }
    } else {
        Write-Failure "AZURE_OPENAI_ENDPOINT environment variable not set"
        Write-Info "Set with: `$env:AZURE_OPENAI_ENDPOINT='$expectedEndpoint'"
        Write-Info "Or add to appsettings.json or Azure Container Apps configuration"
    }

    # Check for API Key (should NOT be in environment for production)
    if ($env:AZURE_OPENAI_API_KEY) {
        Write-Warning "AZURE_OPENAI_API_KEY found in environment variables"
        Write-Info "For production, use Key Vault instead of environment variables"
    } else {
        Write-Success "AZURE_OPENAI_API_KEY not in environment (Good: Use Key Vault in production)"
    }
} else {
    Write-Info "Skipping Azure validation (--SkipAzure flag set)"
}

# Check for Ollama (Local Development)
if (-not $SkipOllama) {
    Write-SectionHeader "Checking Ollama (Local Development)"

    $ollamaEndpoint = "http://localhost:11434"
    
    try {
        if ($IsWindows -or $env:OS -eq "Windows_NT") {
            $ollamaResponse = Invoke-WebRequest -Uri "$ollamaEndpoint/api/version" -Method GET -TimeoutSec 2 -ErrorAction Stop
        } else {
            $ollamaResponse = curl -s -m 2 "$ollamaEndpoint/api/version"
        }
        
        if ($ollamaResponse) {
            Write-Success "Ollama: Running at $ollamaEndpoint"
            
            # List available models
            try {
                if ($IsWindows -or $env:OS -eq "Windows_NT") {
                    $tagsResponse = Invoke-RestMethod -Uri "$ollamaEndpoint/api/tags" -Method GET -TimeoutSec 2
                    if ($tagsResponse.models) {
                        Write-Info "Available models: $($tagsResponse.models.name -join ', ')"
                        
                        # Check for recommended model
                        $hasQwen = $tagsResponse.models | Where-Object { $_.name -match "qwen" }
                        if ($hasQwen) {
                            Write-Success "Recommended model 'qwen2.5-7b' is available"
                        } else {
                            Write-Info "Install qwen2.5-7b: ollama pull qwen2.5-7b"
                        }
                    }
                } else {
                    Write-Info "List models: ollama list"
                }
            } catch {
                Write-Warning "Could not list Ollama models"
            }
        }
    } catch {
        Write-Info "Ollama not running at $ollamaEndpoint"
        Write-Info "For local development:"
        Write-Info "  1. Install: https://ollama.ai"
        Write-Info "  2. Start: ollama serve"
        Write-Info "  3. Pull model: ollama pull qwen2.5-7b"
    }
} else {
    Write-Info "Skipping Ollama check (--SkipOllama flag set)"
}

# Build Solution
Write-SectionHeader "Building Solution"

$solutionFile = "EvoAITest.sln"

if (Test-Path $solutionFile) {
    Write-Info "Building: $solutionFile"
    
    $buildOutput = dotnet build $solutionFile --configuration Release --nologo 2>&1
    $buildExitCode = $LASTEXITCODE
    
    if ($buildExitCode -eq 0) {
        Write-Success "Solution build: PASSED"
        
        # Check for warnings
        $warnings = $buildOutput | Select-String "warning" 
        if ($warnings) {
            Write-Warning "Build produced warnings:"
            $warnings | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }
        }
    } else {
        Write-Failure "Solution build: FAILED (Exit code: $buildExitCode)"
        Write-Host $buildOutput -ForegroundColor Red
    }
} else {
    Write-Failure "Solution file not found: $solutionFile"
}

# Run Tests
Write-SectionHeader "Running Unit Tests"

$testProject = "EvoAITest.Tests\EvoAITest.Tests.csproj"

if (Test-Path $testProject) {
    Write-Info "Running tests: $testProject"
    
    $testOutput = dotnet test $testProject --configuration Release --nologo --verbosity quiet 2>&1
    $testExitCode = $LASTEXITCODE
    
    if ($testExitCode -eq 0) {
        Write-Success "Unit tests: PASSED"
        
        # Extract test summary
        $summary = $testOutput | Select-String "Passed|Failed|Skipped"
        if ($summary) {
            Write-Info "Test Summary:"
            $summary | ForEach-Object { Write-Host "  $_" -ForegroundColor Cyan }
        }
    } else {
        Write-Failure "Unit tests: FAILED (Exit code: $testExitCode)"
        Write-Host $testOutput -ForegroundColor Red
    }
} else {
    Write-Warning "Test project not found: $testProject"
}

# Verify Azure Configuration in Code
Write-SectionHeader "Verifying Azure Configuration in Code"

$optionsFile = "EvoAITest.Core\Options\EvoAITestCoreOptions.cs"
$extensionsFile = "EvoAITest.Core\Extensions\ServiceCollectionExtensions.cs"

if (Test-Path $optionsFile) {
    $optionsContent = Get-Content $optionsFile -Raw
    
    # Check for Azure properties
    $hasAzureOpenAI = $optionsContent -match "AzureOpenAIEndpoint"
    $hasAzureApiKey = $optionsContent -match "AzureOpenAIApiKey"
    $hasOllama = $optionsContent -match "OllamaEndpoint"
    
    if ($hasAzureOpenAI -and $hasAzureApiKey) {
        Write-Success "EvoAITestCoreOptions: Azure OpenAI properties found"
    } else {
        Write-Warning "EvoAITestCoreOptions: Azure OpenAI properties missing"
    }
    
    if ($hasOllama) {
        Write-Success "EvoAITestCoreOptions: Ollama support found"
    } else {
        Write-Warning "EvoAITestCoreOptions: Ollama support missing"
    }
} else {
    Write-Warning "Options file not found: $optionsFile"
}

if (Test-Path $extensionsFile) {
    $extensionsContent = Get-Content $extensionsFile -Raw
    
    $hasConfigBinding = $extensionsContent -match "AddEvoAITestCore"
    
    if ($hasConfigBinding) {
        Write-Success "ServiceCollectionExtensions: Azure config loading found"
    } else {
        Write-Warning "ServiceCollectionExtensions: Config loading may be missing"
    }
} else {
    Write-Warning "Extensions file not found: $extensionsFile"
}

# Check for Hardcoded Secrets
Write-SectionHeader "Scanning for Hardcoded Secrets"

$secretPatterns = @(
    @{ Pattern = "sk-[a-zA-Z0-9]{32,}"; Name = "OpenAI API Key" },
    @{ Pattern = "(key|apikey|api-key|subscription-key)\s*[:=]\s*['""]?[a-f0-9]{64,}['""]?"; Name = "Azure Key (Hex)" },
    @{ Pattern = "DefaultEndpointsProtocol=.*AccountKey=[^;]+"; Name = "Azure Storage Connection String" },
    @{ Pattern = "Password\s*=\s*['""][^'""]+['""]"; Name = "SQL Password" }
)

$foundSecrets = $false

Get-ChildItem -Path . -Include *.cs,*.json -Recurse -Exclude bin,obj | ForEach-Object {
    $file = $_
    $content = Get-Content $file.FullName -Raw
    
    foreach ($secretPattern in $secretPatterns) {
        if ($content -match $secretPattern.Pattern) {
            # Ignore comments and test data
            $lines = $content -split "`n"
            $matchingLines = $lines | Where-Object { 
                $_ -match $secretPattern.Pattern -and 
                $_ -notmatch "//.*$($secretPattern.Pattern)" -and
                $_ -notmatch "test-key" -and
                $_ -notmatch "example" -and
                $_ -notmatch "TODO"
            }
            
            if ($matchingLines) {
                Write-Warning "Potential $($secretPattern.Name) found in: $($file.FullName)"
                $foundSecrets = $true
            }
        }
    }
}

if (-not $foundSecrets) {
    Write-Success "No hardcoded secrets detected in source code"
} else {
    Write-Warning "Review flagged files for potential secrets"
}

# Validate Environment Variables
Write-SectionHeader "Validating Environment Variables"

$llmProvider = $env:EVOAITEST__CORE__LLMPROVIDER

if ($llmProvider) {
    Write-Success "EVOAITEST__CORE__LLMPROVIDER: $llmProvider"
    
    if ($llmProvider -eq "AzureOpenAI") {
        Write-Info "Using Azure OpenAI (Production configuration)"
    } elseif ($llmProvider -eq "Ollama") {
        Write-Info "Using Ollama (Local development configuration)"
    } elseif ($llmProvider -eq "Local") {
        Write-Info "Using Local LLM endpoint"
    } else {
        Write-Warning "Unknown LLM provider: $llmProvider"
    }
} else {
    Write-Info "EVOAITEST__CORE__LLMPROVIDER not set (Will use default: AzureOpenAI)"
    Write-Info "For production: `$env:EVOAITEST__CORE__LLMPROVIDER='AzureOpenAI'"
    Write-Info "For local dev: `$env:EVOAITEST__CORE__LLMPROVIDER='Ollama'"
}

# Final Summary
Write-SectionHeader "Verification Summary"

if ($script:FailureCount -eq 0 -and $script:WarningCount -eq 0) {
    Write-Host @"

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

"@ -ForegroundColor Green
    exit 0
} elseif ($script:FailureCount -eq 0) {
    Write-Host @"

?????????????????????????????????????????????????????????????????
?            ??  VERIFICATION COMPLETED WITH WARNINGS ??         ?
?????????????????????????????????????????????????????????????????

Warnings: $script:WarningCount

Review warnings above and address any issues.
The solution may still work, but warnings should be resolved.

"@ -ForegroundColor Yellow
    exit 0
} else {
    Write-Host @"

?????????????????????????????????????????????????????????????????
?               ? VERIFICATION FAILED ?                        ?
?????????????????????????????????????????????????????????????????

Failures: $script:FailureCount
Warnings: $script:WarningCount

Review errors above and fix issues before proceeding.

Common fixes:
  1. Install .NET 10 SDK: https://dotnet.microsoft.com/
  2. Login to Azure: az login
  3. Set environment variables (see above)
  4. Create Key Vault secret: az keyvault secret set --vault-name evoai-keyvault --name LLMAPIKEY --value '<your-key>'

"@ -ForegroundColor Red
    exit 1
}
