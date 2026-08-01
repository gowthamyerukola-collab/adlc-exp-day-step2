# =============================================================================
# USAGE
# =============================================================================
# Prerequisites:
#   - az CLI installed:          https://aka.ms/install-azure-cli
#   - You must have User Access Administrator or Owner on the target
#     subscription/scope so you can create role assignments.
#
# The script will:
#   1. Prompt for 'az login' if you are not already signed in.
#   2. Automatically switch to subscription 'sub-adlc-exp-blr-2608'.
#   3. For each enrollment (default 1000..1100), assign the Cosmos DB
#      Built-in Data Contributor role (00000000-0000-0000-0000-000000000002)
#      to the enrollment's managed identity on its own Cosmos DB account,
#      skipping enrollments that already have the role.
#   4. Write a CSV log (default cosmos-rbac-fix-log.csv) and print a summary.
#
# Common commands:
#   # Dry run - shows what would be assigned, makes NO changes (recommended first)
#   powershell -ExecutionPolicy Bypass -File scripts\Set-CosmosRbacDataContributor.ps1 -WhatIf
#
#   # Real run - assigns the role where missing (1000..1100)
#   powershell -ExecutionPolicy Bypass -File scripts\Set-CosmosRbacDataContributor.ps1
#
#   # Fix a single enrollment (e.g. 1100)
#   powershell -ExecutionPolicy Bypass -File scripts\Set-CosmosRbacDataContributor.ps1 -StartEnrollment 1100 -EndEnrollment 1100
#
#   # Custom subscription or log path
#   powershell -ExecutionPolicy Bypass -File scripts\Set-CosmosRbacDataContributor.ps1 `
#     -SubscriptionName "my-subscription" -LogPath ".\fix-log.csv"
# =============================================================================
<#
.SYNOPSIS
Grants the Cosmos DB Built-in Data Contributor role to each enrollment's
managed identity on its own Cosmos DB account.

.DESCRIPTION
For each enrollment (default 1000..1100) it ensures the per-enrollment
user-assigned managed identity has the Cosmos DB data-plane role
(Cosmos DB Built-in Data Contributor, role id 00000000-0000-0000-0000-000000000002)
on the enrollment's Cosmos DB account.

Requirements for the person running this script:
  - User Access Administrator or Owner on the target subscription/scope
  - az CLI installed (https://aka.ms/install-azure-cli)

.PARAMETER SubscriptionName
Name or id of the subscription to operate on.

.PARAMETER StartEnrollment
First enrollment number (default 1000).

.PARAMETER EndEnrollment
Last enrollment number (default 1100).

.PARAMETER WhatIf
Dry run: shows what would be assigned without making any change.

.PARAMETER LogPath
Path of the CSV log file to write.

.EXAMPLE
.\scripts\Set-CosmosRbacDataContributor.ps1 -WhatIf
.\scripts\Set-CosmosRbacDataContributor.ps1
.\scripts\Set-CosmosRbacDataContributor.ps1 -StartEnrollment 1100 -EndEnrollment 1100
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [string]$SubscriptionName = 'sub-adlc-exp-blr-2608',
    [int]$StartEnrollment = 1000,
    [int]$EndEnrollment = 1100,
    [string]$LogPath = 'cosmos-rbac-fix-log.csv'
)

$ErrorActionPreference = 'Continue'
$DataContributorRoleId = '00000000-0000-0000-0000-000000000002'
$rows = @()

function Test-AzCli {
    $cmd = Get-Command az -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "az CLI is not installed. Install it from https://aka.ms/install-azure-cli and retry."
    }
}

function Test-AzLogin {
    $account = az account show -o json 2>$null | ConvertFrom-Json
    if (-not $account) {
        Write-Host "You are not signed in to Azure. Launching interactive login..." -ForegroundColor Yellow
        az login
        $account = az account show -o json 2>$null | ConvertFrom-Json
        if (-not $account) {
            throw "Azure login failed. Run 'az login' manually and retry."
        }
    }
    return $account
}

function Select-TargetSubscription {
    az account set --subscription $SubscriptionName
    $account = az account show -o json | ConvertFrom-Json
    if ($account.id -ne $SubscriptionName -and $account.name -ne $SubscriptionName) {
        throw "Could not switch to subscription '$SubscriptionName'. Current: '$($account.name)' ($($account.id))."
    }
    Write-Host "Using subscription: $($account.name) ($($account.id))" -ForegroundColor Cyan
    return $account
}

function Test-DataContributorGranted {
    param([string]$AccountName, [string]$ResourceGroup, [string]$PrincipalId, [string]$AccountId)

    $sqlAssigns = @(az cosmosdb sql role assignment list --account-name $AccountName --resource-group $ResourceGroup --query "[].{principalId:principalId, roleId:roleDefinitionId}" -o json 2>$null | ConvertFrom-Json)
    foreach ($sa in $sqlAssigns) {
        if ($sa.principalId -eq $PrincipalId -and $sa.roleId -like "*$DataContributorRoleId") { return $true }
    }
    $stdAssigns = @(az role assignment list --scope $AccountId --query "[?principalId=='$PrincipalId'].roleDefinitionId" -o json 2>$null | ConvertFrom-Json)
    foreach ($roleId in $stdAssigns) {
        if ($roleId -like "*$DataContributorRoleId") { return $true }
    }
    return $false
}

Test-AzCli
$account = Test-AzLogin
$account = Select-TargetSubscription
$sub = $account.id

Write-Host "Auditing enrollments $StartEnrollment..$EndEnrollment for missing data-plane role..." -ForegroundColor Cyan

foreach ($enr in $StartEnrollment..$EndEnrollment) {
    $key = "$enr"
    $rg = "rg-adlc-exp-2608-$key"
    $identityName = "id-ca-adlc-exp-$key"
    $acctName = "cosmos-adlc-exp-$key"

    $identity = az identity show --name $identityName --resource-group $rg --query "{principalId:principalId, clientId:clientId}" -o json 2>$null | ConvertFrom-Json
    $acct = az cosmosdb show --name $acctName --resource-group $rg --query "{id:id, name:name}" -o json 2>$null | ConvertFrom-Json

    if (-not $identity -or -not $acct) {
        $rows += [pscustomobject]@{ Enrollment=$key; RG=$rg; IdentityName=$identityName; PrincipalId='-'; CosmosAccount=$acctName; Result='SKIPPED-NOT-FOUND' }
        Write-Host "$key : identity or account not found" -ForegroundColor DarkGray
        continue
    }

    $principalId = $identity.principalId
    $acctId = $acct.id

    if (Test-DataContributorGranted -AccountName $acctName -ResourceGroup $rg -PrincipalId $principalId -AccountId $acctId) {
        $rows += [pscustomobject]@{ Enrollment=$key; RG=$rg; IdentityName=$identityName; PrincipalId=$principalId; CosmosAccount=$acctName; Result='ALREADY-GRANTED' }
        Write-Host "$key : already granted" -ForegroundColor Green
        continue
    }

    if ($PSCmdlet.ShouldProcess("$rg / $acctName / $identityName", 'assign Cosmos DB Built-in Data Contributor')) {
        Write-Host "$key : assigning Cosmos DB Built-in Data Contributor..." -ForegroundColor Yellow
        try {
            az cosmosdb sql role assignment create `
                --account-name $acctName `
                --resource-group $rg `
                --role-definition-id $DataContributorRoleId `
                --scope $acctId `
                --principal-id $principalId `
                --output none 2>$null
            if ($LASTEXITCODE -ne 0) { throw "az cosmosdb sql role assignment create failed (exit code $LASTEXITCODE)" }
            $rows += [pscustomobject]@{ Enrollment=$key; RG=$rg; IdentityName=$identityName; PrincipalId=$principalId; CosmosAccount=$acctName; Result='ASSIGNED' }
            Write-Host "$key : assigned" -ForegroundColor Green
        }
        catch {
            $rows += [pscustomobject]@{ Enrollment=$key; RG=$rg; IdentityName=$identityName; PrincipalId=$principalId; CosmosAccount=$acctName; Result="FAILED: $($_.Exception.Message)" }
            Write-Host "$key : FAILED - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    else {
        $rows += [pscustomobject]@{ Enrollment=$key; RG=$rg; IdentityName=$identityName; PrincipalId=$principalId; CosmosAccount=$acctName; Result='WOULD-ASSIGN (what-if)' }
        Write-Host "$key : would assign (what-if)" -ForegroundColor Magenta
    }
}

$rows | Export-Csv -Path $LogPath -NoTypeInformation -Encoding UTF8
Write-Host "`nSummary (log written to $LogPath):" -ForegroundColor Cyan
$rows | Format-Table -AutoSize
