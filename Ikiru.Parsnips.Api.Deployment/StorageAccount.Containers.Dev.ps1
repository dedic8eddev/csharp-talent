param (	
	[Parameter(Mandatory = $true)]
	#The name of the Resource Group
	[string]$ResourceGroupName	
)

$scriptPath = Split-Path $MyInvocation.MyCommand.Path
. $scriptPath\Scripts\CreateStorageContainers.ps1 -StorageAccountName "devparsnipsapistorage" -ResourceGroupName $ResourceGroupName
