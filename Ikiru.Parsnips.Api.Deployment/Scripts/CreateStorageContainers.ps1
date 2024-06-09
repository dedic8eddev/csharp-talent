<#
 .SYNOPSIS
	Creates Azure Storage Containers
#>

param (	
	[Parameter(Mandatory = $true)]
	#The name of the Storage Account
	[string]$StorageAccountName,

	[Parameter(Mandatory = $true)]
	#The name of the Resource Group
	[string]$ResourceGroupName	
)

function CreateStorageContainer {
	Param ($StorageContainerName)

$container = Get-AzureStorageContainer -Container $StorageContainerName -ErrorAction Ignore
	if (-not $container)
	{
	    New-AzureStorageContainer -Name $StorageContainerName -Permission Off
		Write-Host "$StorageContainerName created"
	}
}

Write-Host "Creating Containers in $StorageAccountName"

###############################################################
# Set Storage Account 
###############################################################

Set-AzureRmCurrentStorageAccount -ResourceGroupName $ResourceGroupName -Name $StorageAccountName


###############################################################
# Create Containers
###############################################################
CreateStorageContainer "exportcandidates";
CreateStorageContainer "imports";
CreateStorageContainer "personsdocuments";
CreateStorageContainer "rawresumes";
