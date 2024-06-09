#Requires -Module Az.Resources

<#
 .SYNOPSIS
	Creates one of the Items (indexes, indexers, datasources) in the Search Service
#>

param (	
	[Parameter(Mandatory = $true)]
	[string]$SearchServiceName,

	[Parameter(Mandatory = $true)]
	[string]$ResourceGroupName,
	
	[Parameter(Mandatory = $true)]
	[string]$ItemType,

	[Parameter(Mandatory = $true)]
	[string]$CollectionName # Collection just means the name of the Index, DataSource and Indexer which is used in the filenames.
)

Write-Host "Deleting $CollectionName.$ItemType..."

Write-Host "$CollectionName.$ItemType Reading JSON..."

#reading the JSON with item definition
$body = Get-Content -Raw -Path "$PSScriptRoot\..\SearchServiceItems\$CollectionName.$ItemType.json"

$jsonRequest = $body | ConvertFrom-Json

#read Azure Search Item name from JSON item definition
$ItemName = $jsonRequest.name

Write-Host "$CollectionName.$ItemType Getting Search Admin Key..."

#reading search primary key
$resource = Get-AzResource -ResourceType "Microsoft.Search/searchServices" -ResourceGroupName $ResourceGroupName -ResourceName $SearchServiceName
$azureSearchAdminKey = (Invoke-AzResourceAction -Action listAdminKeys -ResourceId $resource.ResourceId -ApiVersion 2015-08-19 -Force).PrimaryKey

$url = "https://$SearchServiceName.search.windows.net/$($ItemType)" + "?api-version=2020-06-30"
$headers = @{
    "api-key" = "$azureSearchAdminKey"
}

Write-Host "$CollectionName.$ItemType Checking existence..."

#calling Azure GET API to check the item is present
$items = Invoke-WebRequest -Method Get -Uri $url -Headers $headers
$returnValue = ($items.Content | ConvertFrom-Json).value
$itemExists = $returnValue -And $returnValue.name.Contains($ItemName)

Write-Host "$CollectionName.$ItemType Item exists: $itemExists."

If ($itemExists)
{
	Write-Host "$CollectionName.$ItemType Deleting..."

	#calling Azure DELETE API to drop the item
	$deleteUrl = "https://$SearchServiceName.search.windows.net/$ItemType/$($ItemName)?api-version=2019-05-06"
    Invoke-WebRequest -Method Delete -Uri $deleteUrl -ContentType "application/json" -Headers $headers
}
