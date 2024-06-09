param (	
	[Parameter(Mandatory = $true)]
	#The name of the Resource Group
	[string]$ResourceGroupName,

    [switch]$DropIfExists
)

. $PSScriptRoot\Scripts\CreateSearchIndexesForCosmos.ps1 -SearchServiceName "test-parsnipssearchservice" -CosmosServiceName "testparsnipscosmosdb" -ResourceGroupName $ResourceGroupName -DropIfExists:$DropIfExists
