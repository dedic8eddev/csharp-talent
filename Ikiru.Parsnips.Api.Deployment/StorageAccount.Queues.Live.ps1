param (	
	[Parameter(Mandatory = $true)]
	#The name of the Resource Group
	[string]$ResourceGroupName	
)

$scriptPath = Split-Path $MyInvocation.MyCommand.Path
. $scriptPath\Scripts\CreateStorageQueues.ps1 -StorageAccountName "liveparsnipsapistorage" -ResourceGroupName $ResourceGroupName
