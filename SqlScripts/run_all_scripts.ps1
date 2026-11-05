# ==========================================================================================
# PowerShell Runner: run_all_scripts.ps1
# DESCRIPTION: Executes all SQL scripts in the SqlScripts folder in exact sequential order
#              using sqlcmd.exe or direct database connection.
#
# USAGE EXAMPLES:
#   .\run_all_scripts.ps1 -Server "localhost" -Database "PropertyAssessmentDb"
#   .\run_all_scripts.ps1 -Server "tcp:your-cloud-sql.database.windows.net" -Database "AssessorDb" -Username "cloudadmin" -Password "SecretPass!"
# ==========================================================================================
param(
    [string]$Server = "localhost",
    [string]$Database = "PropertyAssessmentDb",
    [string]$Username = "",
    [string]$Password = ""
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path

$scripts = @(
    "01_CreateTables.sql",
    "04_SeedFileLocationDefaultSetup.sql",
    "05_CreateInspectionAndAssessmentRequestTables.sql",
    "17_AddTransactionTypeToRequestForInspection.sql",
    "18_AddCancellationAndSupersedingColumnsToTaxDeclaration.sql",
    "02_SeedDefaultAdmin.sql",
    "12_SeedDefaultModules.sql",
    "13_SeedDefaultUserRoles.sql",
    "06_SyncUserRolesWithWorkflowSteps.sql",
    "08_SeedDefaultRolePermissions.sql",
    "19_SeedTransactionStatusesAndWorkflowSequences.sql",
    "07_ImportUsersFromCast.sql",
    "09_SeedBuildingClassifications.sql",
    "10_SeedBuildingAdditionalItems.sql",
    "11_SeedBuildingAssessmentDepreciationAndRates.sql",
    "14_SeedLandPrerequisitesAndTables.sql",
    "15_SeedDefaultProvincesCitiesBarangays.sql",
    "16_SeedMachineryPrerequisitesAndTables.sql",
    "20_CreateAndSeedBuildingDepreciationSchedule.sql",
    "21_UpdateBuildingStructuralMaterials.sql",
    "22_SeedAllSampleData.sql"
)

Write-Host "==================================================================" -ForegroundColor Cyan
Write-Host "City Assessor Database Initialization - Script Runner" -ForegroundColor Cyan
Write-Host "Target Server:   $Server" -ForegroundColor Yellow
Write-Host "Target Database: $Database" -ForegroundColor Yellow
Write-Host "==================================================================" -ForegroundColor Cyan

$i = 1
$total = $scripts.Count

foreach ($scriptName in $scripts) {
    $scriptPath = Join-Path $scriptDir $scriptName
    if (-not (Test-Path $scriptPath)) {
        Write-Warning "File not found: $scriptPath (skipping)"
        continue
    }

    Write-Host "[$i/$total] Running $scriptName..." -ForegroundColor Green -NoNewline

    $cmdArgs = @("-S", $Server, "-d", $Database, "-b", "-i", $scriptPath)
    if ($Username -and $Password) {
        $cmdArgs += @("-U", $Username, "-P", $Password)
    } else {
        $cmdArgs += @("-E") # Windows Authentication
    }

    $output = & sqlcmd.exe @cmdArgs 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host " [FAILED]" -ForegroundColor Red
        Write-Host $output -ForegroundColor Red
        Write-Error "Execution stopped due to error in $scriptName."
        exit $LASTEXITCODE
    } else {
        Write-Host " [DONE]" -ForegroundColor Gray
    }
    $i++
}

Write-Host "`nAll $total scripts executed successfully in sequence!" -ForegroundColor Green
