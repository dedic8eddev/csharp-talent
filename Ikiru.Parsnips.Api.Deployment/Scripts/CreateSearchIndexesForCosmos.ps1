#Requires -Module Az.CosmosDB

<#
 .SYNOPSIS
	Creates all of the required items in the previously created Azure search service.
#>

param (	
	[Parameter(Mandatory = $true)]
	[string]$SearchServiceName,

	[Parameter(Mandatory = $true)]
	[string]$CosmosServiceName,

	[Parameter(Mandatory = $true)]
	[string]$ResourceGroupName,

	[switch]$DropIfExists
)

Write-Host "Dropping search index flag: [DropIfExists: $DropIfExists]"

# Get CosmosDb Key
$cosmosConnectionStrings = Get-AzCosmosDBAccountKey -ResourceGroupName $ResourceGroupName -Name $CosmosServiceName -Type "ConnectionStrings"
$connString = $cosmosConnectionStrings["Primary Read-Only SQL Connection String"]
$connString = "$connString;Database=Parsnips"

If ($DropIfExists)
{
	. $PSScriptRoot\DeleteSearchItem.ps1 -SearchServiceName $SearchServiceName -ResourceGroupName $ResourceGroupName -ItemType "indexers" -CollectionName "persons"
	. $PSScriptRoot\DeleteSearchItem.ps1 -SearchServiceName $SearchServiceName -ResourceGroupName $ResourceGroupName -ItemType "datasources" -CollectionName "persons"
	. $PSScriptRoot\DeleteSearchItem.ps1 -SearchServiceName $SearchServiceName -ResourceGroupName $ResourceGroupName -ItemType "indexes" -CollectionName "persons"
}


. $PSScriptRoot\CreateSearchItem.ps1 -SearchServiceName $SearchServiceName -ResourceGroupName $ResourceGroupName -ItemType "datasources" -CollectionName "persons" -SourceConnectionString $connString
. $PSScriptRoot\CreateSearchItem.ps1 -SearchServiceName $SearchServiceName -ResourceGroupName $ResourceGroupName -ItemType "indexes" -CollectionName "persons"
. $PSScriptRoot\CreateSearchItem.ps1 -SearchServiceName $SearchServiceName -ResourceGroupName $ResourceGroupName -ItemType "indexers" -CollectionName "persons"
