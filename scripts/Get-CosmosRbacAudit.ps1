<#
.SYNOPSIS
Audits Cosmos DB RBAC readiness for each ADLC enrollment resource group.

.DESCRIPTION
For each enrollment (default 1000..1100) it checks the per-enrollment
user-assigned managed identity against its own resource group and Cosmos DB
account:

  - Reader at the resource group level
  - DocumentDB Account Contributor (control plane) at the Cosmos account scope
  - Cosmos DB Built-in Data Contributor (data plane) at the Cosmos account scope

Writes the result to a CSV file.

.PARAMETER Subscription
Subscription id containing the enrollment resource groups.

.PARAMETER StartEnrollment
First enrollment number (default 1000).

.PARAMETER EndEnrollment
Last enrollment number (default 1100).

.PARAMETER OutputPath
Path of the CSV file to write.

.EXAMPLE
.\scripts\Get-CosmosRbacAudit.ps1
.EXAMPLE
.\scripts\Get-CosmosRbacAudit.ps1 -OutputPath .\cosmos-rbac-audit.csv
#>
param(
    [string]$Subscription = '4969651e-74b0-4e8a-a81d-7fbb61c3fee5',
    [int]$StartEnrollment = 1000,
    [int]$EndEnrollment = 1100,
    [string]$OutputPath = 'cosmos-rbac-audit.csv'
)

$ErrorActionPreference = 'Stop'
$rows = @()

Write-Host "Auditing enrollments $StartEnrollment..$EndEnrollment in subscription $Subscription"

$identities = az identity list --subscription $Subscription --query "[?starts_with(name,'id-ca-adlc-exp')].{name:name, rg:resourceGroup, principalId:principalId}" -o json | ConvertFrom-Json
$accts = az cosmosdb list --subscription $Subscription --query "[?starts_with(name,'cosmos-adlc-exp')].{name:name, rg:resourceGroup, id:id}" -o json | ConvertFrom-Json
$allRbac = az role assignment list --subscription $Subscription --all --query "[].{principalId:principalId, scope:scope, roleName:roleDefinitionName, roleId:roleDefinitionId}" -o json | ConvertFrom-Json

$identByEnr = @{}
foreach ($i in $identities) {
    if ($i.name -match 'id-ca-adlc-exp-(\d{4})$') { $identByEnr[$matches[1]] = $i }
}
$acctByEnr = @{}
foreach ($a in $accts) {
    if ($a.name -match 'cosmos-adlc-exp-(\d{4})$') { $acctByEnr[$matches[1]] = $a }
}

foreach ($enr in $StartEnrollment..$EndEnrollment) {
    $key = "$enr"
    $rg = "rg-adlc-exp-2608-$key"
    $acctName = "cosmos-adlc-exp-$key"
    $rgScope = "/subscriptions/$Subscription/resourceGroups/$rg"

    $acct = $acctByEnr[$key]
    $ident = $identByEnr[$key]

    if (-not $acct -and -not $ident) {
        $rows += [pscustomobject]@{ Enrollment=$key; RG=$rg; IdentityName='-'; PrincipalId='-'; CosmosAccount=$acctName; ReaderAtRG='RG-NOT-FOUND'; AccountContributor='ACCT-NOT-FOUND'; DataContributor='ACCT-NOT-FOUND'; Status='MISSING-RESOURCES' }
        continue
    }
    if (-not $ident) {
        $rows += [pscustomobject]@{ Enrollment=$key; RG=$rg; IdentityName='IDENTITY-NOT-FOUND'; PrincipalId='-'; CosmosAccount=$acctName; ReaderAtRG='-'; AccountContributor='-'; DataContributor='-'; Status='MISSING-IDENTITY' }
        continue
    }

    $principalId = $ident.principalId
    $acctScope = $acct.id

    $readerAtRg = $false
    foreach ($ra in $allRbac) {
        if ($ra.principalId -eq $principalId -and $ra.scope -eq $rgScope -and $ra.roleName -eq 'Reader') { $readerAtRg = $true; break }
    }

    $acctContrib = $false
    foreach ($ra in $allRbac) {
        if ($ra.principalId -eq $principalId -and $ra.scope -eq $acctScope -and $ra.roleName -eq 'DocumentDB Account Contributor') { $acctContrib = $true; break }
    }

    $dataContrib = $false
    $sqlAssigns = @(az cosmosdb sql role assignment list --account-name $acctName --resource-group $rg --query "[].{principalId:principalId, roleId:roleDefinitionId}" -o json 2>$null | ConvertFrom-Json)
    foreach ($sa in $sqlAssigns) {
        if ($sa.principalId -eq $principalId -and $sa.roleId -like '*00000000-0000-0000-0000-000000000002') { $dataContrib = $true; break }
    }
    if (-not $dataContrib) {
        foreach ($ra in $allRbac) {
            if ($ra.principalId -eq $principalId -and $ra.scope -eq $acctScope -and $ra.roleId -like '*00000000-0000-0000-0000-000000000002') { $dataContrib = $true; break }
        }
    }

    $rows += [pscustomobject]@{
        Enrollment=$key; RG=$rg; IdentityName=$ident.name; PrincipalId=$principalId; CosmosAccount=$acctName
        ReaderAtRG=$(if($readerAtRg){'OK'}else{'MISSING'})
        AccountContributor=$(if($acctContrib){'OK'}else{'MISSING'})
        DataContributor=$(if($dataContrib){'OK'}else{'MISSING'})
        Status=$(if($readerAtRg -and $acctContrib -and $dataContrib){'COMPLETE'}else{'INCOMPLETE'})
    }
    Write-Host "checked $rg"
}

$rows | Export-Csv -Path $OutputPath -NoTypeInformation -Encoding UTF8
Write-Host "CSV written to $OutputPath with $($rows.Count) rows"
