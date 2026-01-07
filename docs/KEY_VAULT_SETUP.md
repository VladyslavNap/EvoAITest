# Azure Key Vault Setup Guide

This guide explains how to set up and configure Azure Key Vault for secure secret management in the EvoAITest application.

---

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Creating Azure Key Vault](#creating-azure-key-vault)
4. [Configuring Access Permissions](#configuring-access-permissions)
5. [Adding Secrets](#adding-secrets)
6. [Local Development Setup](#local-development-setup)
7. [Production Configuration](#production-configuration)
8. [Troubleshooting](#troubleshooting)

---

## Overview

Azure Key Vault provides secure storage for:
- API keys (OpenAI, Azure OpenAI, etc.)
- Database connection strings
- Service credentials
- Certificates and encryption keys

The application uses:
- **`DefaultAzureCredential`** for authentication (supports managed identity, Azure CLI, Visual Studio, environment variables)
- **In-memory caching** for performance (default: 60 minutes)
- **Automatic retry** with exponential backoff

---

## Prerequisites

### Required Tools
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (v2.40+)
- [Azure subscription](https://azure.microsoft.com/free/)
- Azure account with permissions to create Key Vault

### Required Permissions
- **Resource Group**: Contributor or Owner
- **Key Vault**: Key Vault Administrator or Key Vault Secrets Officer

---

## Creating Azure Key Vault

### Option 1: Azure Portal

1. **Navigate to Azure Portal**
   - Go to https://portal.azure.com
   - Sign in with your Azure account

2. **Create Key Vault**
   ```
   - Click "Create a resource"
   - Search for "Key Vault"
   - Click "Create"
   ```

3. **Configure Basic Settings**
   - **Subscription**: Select your subscription
   - **Resource Group**: Create new or select existing (e.g., `rg-evoaitest`)
   - **Key Vault Name**: Enter unique name (e.g., `kv-evoaitest-dev`)
   - **Region**: Select your region (e.g., `East US`)
   - **Pricing Tier**: Standard

4. **Configure Access Policy**
   - **Permission Model**: Choose "Vault access policy" or "Azure RBAC"
   - If using **Vault access policy**:
     - Add yourself with "Secret Get, List, Set" permissions
   - If using **Azure RBAC**:
     - Assign yourself "Key Vault Secrets Officer" role

5. **Networking** (Optional)
   - **Public access**: Allow (for development)
   - **Private endpoint**: Configure for production

6. **Review + Create**
   - Review settings
   - Click "Create"

### Option 2: Azure CLI

```bash
# Login to Azure
az login

# Set variables
RESOURCE_GROUP="rg-evoaitest"
LOCATION="eastus"
KEY_VAULT_NAME="kv-evoaitest-dev"

# Create resource group
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION

# Create Key Vault
az keyvault create \
  --name $KEY_VAULT_NAME \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION \
  --enable-rbac-authorization true

# Get your user ID
USER_ID=$(az ad signed-in-user show --query id -o tsv)

# Assign yourself Key Vault Secrets Officer role
az role assignment create \
  --role "Key Vault Secrets Officer" \
  --assignee $USER_ID \
  --scope /subscriptions/{subscription-id}/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.KeyVault/vaults/$KEY_VAULT_NAME
```

---

## Configuring Access Permissions

### For Local Development (Azure CLI)

```bash
# Ensure you're logged in
az login

# Verify access
az keyvault secret list --vault-name kv-evoaitest-dev
```

The application will automatically use your Azure CLI credentials via `DefaultAzureCredential`.

### For Production (Managed Identity)

#### 1. Enable Managed Identity on App Service

```bash
# Enable system-assigned managed identity
az webapp identity assign \
  --name your-app-name \
  --resource-group rg-evoaitest

# Get the principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name your-app-name \
  --resource-group rg-evoaitest \
  --query principalId -o tsv)
```

#### 2. Grant Key Vault Access

```bash
# Assign Key Vault Secrets User role to the managed identity
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $PRINCIPAL_ID \
  --scope /subscriptions/{subscription-id}/resourceGroups/rg-evoaitest/providers/Microsoft.KeyVault/vaults/kv-evoaitest-prod
```

### For Azure Container Apps

```bash
# Create user-assigned managed identity
az identity create \
  --name id-evoaitest \
  --resource-group rg-evoaitest

# Get identity details
IDENTITY_ID=$(az identity show \
  --name id-evoaitest \
  --resource-group rg-evoaitest \
  --query id -o tsv)

IDENTITY_PRINCIPAL_ID=$(az identity show \
  --name id-evoaitest \
  --resource-group rg-evoaitest \
  --query principalId -o tsv)

# Grant Key Vault access
az role assignment create \
  --role "Key Vault Secrets User" \
  --assignee $IDENTITY_PRINCIPAL_ID \
  --scope /subscriptions/{subscription-id}/resourceGroups/rg-evoaitest/providers/Microsoft.KeyVault/vaults/kv-evoaitest-prod

# Assign identity to container app
az containerapp update \
  --name your-container-app \
  --resource-group rg-evoaitest \
  --user-assigned $IDENTITY_ID
```

---

## Adding Secrets

### Via Azure Portal

1. Navigate to your Key Vault
2. Click **"Secrets"** in the left menu
3. Click **"+ Generate/Import"**
4. Enter secret details:
   - **Name**: `OpenAI-ApiKey` (use hyphens, not underscores)
   - **Value**: Your API key
   - **Activation date**: (optional)
   - **Expiration date**: (optional)
5. Click **"Create"**

### Via Azure CLI

```bash
KEY_VAULT_NAME="kv-evoaitest-dev"

# Add OpenAI API Key
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "OpenAI-ApiKey" \
  --value "sk-..."

# Add Azure OpenAI API Key
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "AzureOpenAI-ApiKey" \
  --value "your-azure-openai-key"

# Add Database Connection String
az keyvault secret set \
  --vault-name $KEY_VAULT_NAME \
  --name "Database-ConnectionString" \
  --value "Server=...;Database=...;User Id=...;Password=..."

# Verify secrets
az keyvault secret list --vault-name $KEY_VAULT_NAME --query "[].name" -o table
```

### Common Secrets to Add

| Secret Name | Purpose | Example Value |
|-------------|---------|---------------|
| `OpenAI-ApiKey` | OpenAI API access | `sk-...` |
| `AzureOpenAI-ApiKey` | Azure OpenAI access | `abcd1234...` |
| `AzureOpenAI-Endpoint` | Azure OpenAI endpoint | `https://your-resource.openai.azure.com/` |
| `Database-ConnectionString` | SQL Server connection | `Server=...` |
| `Redis-ConnectionString` | Redis cache connection | `your-redis.redis.cache.windows.net:6380,...` |

---

## Local Development Setup

### Option 1: Without Key Vault (Recommended for Local Dev)

Use **User Secrets** or **appsettings.Development.json**:

#### User Secrets (Recommended)

```bash
cd EvoAITest.ApiService

# Initialize user secrets
dotnet user-secrets init

# Add secrets
dotnet user-secrets set "EvoAITest:Core:AzureOpenAIApiKey" "your-key-here"
dotnet user-secrets set "EvoAITest:Core:OllamaEndpoint" "http://localhost:11434"
```

#### appsettings.Development.json

```json
{
  "KeyVault": {
    "Enabled": false
  },
  "EvoAITest": {
    "Core": {
      "AzureOpenAIApiKey": "your-key-here",
      "AzureOpenAIEndpoint": "https://your-resource.openai.azure.com/",
      "AzureOpenAIDeployment": "gpt-4",
      "OllamaEndpoint": "http://localhost:11434",
      "OllamaModel": "qwen2.5-coder:7b"
    }
  }
}
```

### Option 2: With Key Vault (Optional for Local Dev)

#### appsettings.Development.json

```json
{
  "KeyVault": {
    "VaultUri": "https://kv-evoaitest-dev.vault.azure.net/",
    "Enabled": true,
    "EnableCaching": false,
    "MaxRetries": 1,
    "LogSecretNames": true
  }
}
```

**Prerequisites**:
- Logged in via `az login`
- Granted Key Vault Secrets User role (see above)

---

## Production Configuration

### appsettings.Production.json

```json
{
  "KeyVault": {
    "VaultUri": "https://kv-evoaitest-prod.vault.azure.net/",
    "Enabled": true,
    "EnableCaching": true,
    "CacheDuration": "01:00:00",
    "MaxRetries": 3,
    "OperationTimeout": "00:00:30",
    "LogSecretNames": false
  }
}
```

### Environment Variables (Azure App Service / Container Apps)

```bash
# Set via Azure Portal or CLI
KeyVault__VaultUri=https://kv-evoaitest-prod.vault.azure.net/
KeyVault__Enabled=true
KeyVault__EnableCaching=true
```

### Aspire AppHost Configuration

The AppHost automatically passes Key Vault configuration to services:

```csharp
// EvoAITest.AppHost/AppHost.cs
var keyVaultEndpoint = builder.AddParameter("KeyVaultEndpoint", secret: false);

var apiService = builder.AddProject<Projects.EvoAITest_ApiService>("apiservice")
    .WithEnvironment("KeyVault__VaultUri", keyVaultEndpoint);
```

To set the parameter:

```bash
# appsettings.json in AppHost
{
  "Parameters": {
    "KeyVaultEndpoint": "https://kv-evoaitest-prod.vault.azure.net/"
  }
}
```

---

## Troubleshooting

### Issue: "Azure Key Vault is not accessible"

**Symptoms:**
- `ISecretProvider.IsAvailableAsync()` returns `false`
- Errors in logs about authentication

**Solutions:**

1. **Check Azure CLI Login**
   ```bash
   az login
   az account show
   ```

2. **Verify Key Vault Permissions**
   ```bash
   az keyvault secret list --vault-name kv-evoaitest-dev
   ```

3. **Check Managed Identity**
   ```bash
   # Verify identity is assigned
   az webapp identity show --name your-app --resource-group rg-evoaitest
   
   # Verify role assignment
   az role assignment list --assignee {principal-id} --all
   ```

4. **Verify Key Vault URL**
   - Ensure `VaultUri` ends with `/` (e.g., `https://kv-name.vault.azure.net/`)
   - Ensure using `.vault.azure.net` (not `.vault.azure.com`)

### Issue: "Secret not found"

**Solutions:**

1. **List all secrets**
   ```bash
   az keyvault secret list --vault-name kv-evoaitest-dev --query "[].name"
   ```

2. **Check secret naming**
   - Use hyphens: `OpenAI-ApiKey` (not `OpenAI_ApiKey`)
   - Secret names are case-sensitive

3. **Verify secret exists**
   ```bash
   az keyvault secret show --vault-name kv-evoaitest-dev --name "OpenAI-ApiKey"
   ```

### Issue: "Access denied" (403)

**Solutions:**

1. **Grant correct role**
   ```bash
   # For reading secrets
   az role assignment create \
     --role "Key Vault Secrets User" \
     --assignee {user-or-principal-id} \
     --scope {keyvault-resource-id}
   ```

2. **Check firewall rules**
   - In Azure Portal, go to Key Vault ? Networking
   - Ensure your IP or service is allowed

3. **Verify RBAC vs Access Policy**
   - Key Vault uses either RBAC or Access Policies (not both)
   - Check "Permission model" in Key Vault settings

### Issue: High latency on secret retrieval

**Solutions:**

1. **Enable caching**
   ```json
   {
     "KeyVault": {
       "EnableCaching": true,
       "CacheDuration": "01:00:00"
     }
   }
   ```

2. **Reduce timeout**
   ```json
   {
     "KeyVault": {
       "OperationTimeout": "00:00:10"
     }
   }
   ```

3. **Use batch retrieval**
   ```csharp
   var secrets = await secretProvider.GetSecretsAsync(new[]
   {
       "OpenAI-ApiKey",
       "Database-ConnectionString"
   });
   ```

### Issue: "DefaultAzureCredential failed"

**Solutions:**

1. **Check credential chain**
   - Environment Variables ? Managed Identity ? Azure CLI ? Visual Studio
   - Set `AZURE_TENANT_ID` if needed

2. **Use specific credential**
   ```json
   {
     "KeyVault": {
       "TenantId": "00000000-0000-0000-0000-000000000000"
     }
   }
   ```

3. **Enable diagnostic logging**
   ```bash
   # Set environment variable
   AZURE_LOG_LEVEL=verbose
   ```

---

## Best Practices

### Security

1. **Never commit secrets to source control**
   - Use `.gitignore` for `appsettings.Development.json` with secrets
   - Use User Secrets for local development
   - Use Key Vault for all environments (dev, staging, prod)

2. **Limit permissions**
   - Grant "Secrets User" (read-only) in production
   - Grant "Secrets Officer" (read/write) only to admins
   - Use separate Key Vaults for dev/staging/prod

3. **Enable monitoring**
   ```bash
   # Enable diagnostic logs
   az monitor diagnostic-settings create \
     --name diag-keyvault \
     --resource {keyvault-resource-id} \
     --logs '[{"category": "AuditEvent", "enabled": true}]' \
     --workspace {log-analytics-workspace-id}
   ```

4. **Configure network restrictions**
   - Use private endpoints in production
   - Enable firewall rules
   - Restrict to specific IPs or VNets

### Performance

1. **Enable caching**
   - Cache duration: 30-60 minutes for production
   - Cache duration: 5-15 minutes for development

2. **Use batch retrieval**
   - Fetch multiple secrets in one call
   - Reduces API calls and latency

3. **Pre-load secrets at startup**
   ```csharp
   var secrets = await secretProvider.GetSecretsAsync(new[]
   {
       "OpenAI-ApiKey",
       "Database-ConnectionString",
       "Redis-ConnectionString"
   });
   ```

### Operations

1. **Rotate secrets regularly**
   ```bash
   # Update secret
   az keyvault secret set \
     --vault-name kv-evoaitest-prod \
     --name "OpenAI-ApiKey" \
     --value "new-key"
   
   # Invalidate cache (if using app code)
   secretProvider.InvalidateCache("OpenAI-ApiKey");
   ```

2. **Monitor secret expiration**
   - Set expiration dates on secrets
   - Monitor via Azure Monitor alerts

3. **Use secret versions**
   - Key Vault maintains secret history
   - Roll back if needed:
     ```bash
     az keyvault secret show \
       --vault-name kv-evoaitest-prod \
       --name "OpenAI-ApiKey" \
       --version {version-id}
     ```

---

## Reference

### Useful Azure CLI Commands

```bash
# List all Key Vaults
az keyvault list --query "[].name" -o table

# Get Key Vault details
az keyvault show --name kv-evoaitest-dev

# List all secrets
az keyvault secret list --vault-name kv-evoaitest-dev

# Get secret value
az keyvault secret show --vault-name kv-evoaitest-dev --name "OpenAI-ApiKey" --query "value" -o tsv

# Delete secret (soft delete)
az keyvault secret delete --vault-name kv-evoaitest-dev --name "old-secret"

# Purge deleted secret (permanent)
az keyvault secret purge --vault-name kv-evoaitest-dev --name "old-secret"

# List deleted secrets
az keyvault secret list-deleted --vault-name kv-evoaitest-dev

# Recover deleted secret
az keyvault secret recover --vault-name kv-evoaitest-dev --name "recovered-secret"
```

### Configuration Reference

See `KeyVaultOptions.cs` for all available configuration properties:

- `VaultUri` - Key Vault endpoint (required)
- `Enabled` - Enable/disable Key Vault (default: true)
- `EnableCaching` - Cache secrets in memory (default: true)
- `CacheDuration` - Cache TTL (default: 60 minutes)
- `MaxRetries` - Retry attempts (default: 3)
- `OperationTimeout` - API timeout (default: 30 seconds)
- `TenantId` - Azure AD tenant ID (optional)
- `LogSecretNames` - Log secret names (default: true)

---

## Additional Resources

- [Azure Key Vault Documentation](https://docs.microsoft.com/en-us/azure/key-vault/)
- [DefaultAzureCredential](https://docs.microsoft.com/en-us/dotnet/api/azure.identity.defaultazurecredential)
- [Azure RBAC for Key Vault](https://docs.microsoft.com/en-us/azure/key-vault/general/rbac-guide)
- [Key Vault Best Practices](https://docs.microsoft.com/en-us/azure/key-vault/general/best-practices)

---

**Last Updated:** December 2024  
**Version:** 1.0  
**Maintainer:** EvoAITest Team
