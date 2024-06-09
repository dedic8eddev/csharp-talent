param (	
	[Parameter(Mandatory = $true)]
	#The name of the Resource Group
	[string]$ResourceGroupName,

    [switch]$DropIfExists
)

Write-Host "Dropping search index: [DropIfExists: $DropIfExists]"

. $PSScriptRoot\Scripts\CreateSearchIndexesForCosmos.ps1 -SearchServiceName "dev-parsnipssearchservice" -CosmosServiceName "devparsnipscosmosdb" -ResourceGroupName $ResourceGroupName -DropIfExists:$DropIfExists
