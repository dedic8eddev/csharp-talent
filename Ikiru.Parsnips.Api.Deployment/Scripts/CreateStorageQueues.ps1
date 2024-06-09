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

function CreateStorageQueue {
	Param ($StorageQueueName)

$queue = Get-AzureStorageQueue -Name $StorageQueueName -ErrorAction Ignore
	if (-not $queue)
	{
	    New-AzureStorageQueue -Name $StorageQueueName
		Write-Host "$StorageQueueName created"
	}
}

Write-Host "Creating Queues in $StorageAccountName"

###############################################################
# Set Storage Account 
###############################################################

Set-AzureRmCurrentStorageAccount -ResourceGroupName $ResourceGroupName -Name $StorageAccountName


###############################################################
# Create Queues
###############################################################
CreateStorageQueue "exportcandidatesqueue";
CreateStorageQueue "personimportfileuploadqueue";
CreateStorageQueue "personlocationchangedqueue";
CreateStorageQueue "searchfirmconfirmationemailqueue";
CreateStorageQueue "datapoolcorepersonupdatedequeue";
CreateStorageQueue "searchfirmsubscriptioneventqueue";

