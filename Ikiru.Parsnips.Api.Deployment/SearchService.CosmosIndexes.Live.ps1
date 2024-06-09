param (	
	[Parameter(Mandatory = $true)]
	#The name of the Resource Group
	[string]$ResourceGroupName,

    [switch]$DropIfExists
)

. $PSScriptRoot\Scripts\CreateSearchIndexesForCosmos.ps1 -SearchServiceName "live-parsnipssearchservice" -CosmosServiceName "liveparsnipscosmosdb" -ResourceGroupName $ResourceGroupName -DropIfExists:$DropIfExists
