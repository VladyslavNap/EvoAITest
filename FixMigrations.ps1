# Script to fix migration issues
# Run this from the solution root directory

Write-Host "EvoAITest Migration Fix Script" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Green
Write-Host ""

# Option 1: Clean slate - remove and recreate all migrations
function Reset-Migrations {
    Write-Host "Option 1: Clean Slate (Recommended for fresh DB)" -ForegroundColor Yellow
    Write-Host "This will:" -ForegroundColor Yellow
    Write-Host "  1. Delete all migration files" -ForegroundColor Yellow
    Write-Host "  2. Create a single consolidated migration" -ForegroundColor Yellow
    Write-Host "  3. Apply it to your database" -ForegroundColor Yellow
    Write-Host ""
    
    $confirm = Read-Host "Do you want to proceed? (yes/no)"
    if ($confirm -eq "yes") {
        Write-Host "Removing old migrations..." -ForegroundColor Cyan
        Remove-Item -Path "EvoAITest.Core/Migrations/*.cs" -Force
        
        Write-Host "Creating new consolidated migration..." -ForegroundColor Cyan
        dotnet ef migrations add InitialCreate -p EvoAITest.Core -s EvoAITest.ApiService
        
        Write-Host "Applying migration to database..." -ForegroundColor Cyan
        dotnet ef database update -p EvoAITest.Core -s EvoAITest.ApiService
        
        Write-Host "? Complete!" -ForegroundColor Green
    }
}

# Option 2: Drop and recreate database with existing migrations
function Reset-Database {
    Write-Host "Option 2: Drop and Recreate Database" -ForegroundColor Yellow
    Write-Host "This will:" -ForegroundColor Yellow
    Write-Host "  1. Drop the existing database" -ForegroundColor Yellow
    Write-Host "  2. Recreate it with all existing migrations" -ForegroundColor Yellow
    Write-Host ""
    
    $confirm = Read-Host "Do you want to proceed? (yes/no)"
    if ($confirm -eq "yes") {
        Write-Host "Dropping database..." -ForegroundColor Cyan
        dotnet ef database drop -p EvoAITest.Core -s EvoAITest.ApiService --force
        
        Write-Host "Applying all migrations..." -ForegroundColor Cyan
        dotnet ef database update -p EvoAITest.Core -s EvoAITest.ApiService
        
        Write-Host "? Complete!" -ForegroundColor Green
    }
}

# Option 3: Manual fix - just apply pending migrations
function Apply-Migrations {
    Write-Host "Option 3: Apply Pending Migrations" -ForegroundColor Yellow
    Write-Host "This will attempt to apply any pending migrations" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "Applying migrations..." -ForegroundColor Cyan
    dotnet ef database update -p EvoAITest.Core -s EvoAITest.ApiService
    
    Write-Host "? Complete!" -ForegroundColor Green
}

# Menu
Write-Host "Choose an option:" -ForegroundColor Cyan
Write-Host "1. Clean Slate - Remove all migrations and create new (Recommended)" -ForegroundColor White
Write-Host "2. Drop and Recreate Database with existing migrations" -ForegroundColor White
Write-Host "3. Just apply pending migrations" -ForegroundColor White
Write-Host "4. Exit" -ForegroundColor White
Write-Host ""

$choice = Read-Host "Enter your choice (1-4)"

switch ($choice) {
    "1" { Reset-Migrations }
    "2" { Reset-Database }
    "3" { Apply-Migrations }
    "4" { Write-Host "Exiting..." -ForegroundColor Yellow }
    default { Write-Host "Invalid choice" -ForegroundColor Red }
}
