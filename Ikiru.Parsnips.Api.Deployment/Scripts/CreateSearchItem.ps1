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
	[string]$CollectionName, # Collection just means the name of the Index, DataSource and Indexer which is used in the filenames.

	[Parameter(Mandatory = $false)]
	[string]$SourceConnectionString
)

Write-Host "$CollectionName.$ItemType"

#reading the JSON with item definition
$body = Get-Content -Raw -Path "$PSScriptRoot\..\SearchServiceItems\$CollectionName.$ItemType.json"

# Disabled for now as maybe not a good idea to have different definition on different environments. But was added to aid with debugging.
#if ($ResourceGroupName.ToUpper().StartsWith("DEV")) {
#    $body = $body.Replace("""retrievable"": false","""retrievable"": true")
#}

Write-Host "$CollectionName.$ItemType Reading/Updating JSON..."

$jsonRequest = $body | ConvertFrom-Json

if ($PSBoundParameters.ContainsKey('SourceConnectionString')) {
	$jsonRequest.credentials.connectionString = $SourceConnectionString
	$body = $jsonRequest | ConvertTo-Json
}

#read Azure Search Item name from JSON item definition
$ItemName = $jsonRequest.name

Write-Host "$CollectionName.$ItemType Getting Search Admin Key..."

#reading search primary key
$resource = Get-AzResource -ResourceType "Microsoft.Search/searchServices" -ResourceGroupName $ResourceGroupName -ResourceName $SearchServiceName
$azureSearchAdminKey = (Invoke-AzResourceAction -Action listAdminKeys -ResourceId $resource.ResourceId -ApiVersion 2015-08-19 -Force).PrimaryKey

$url = "https://$SearchServiceName.search.windows.net/$($ItemType)?api-version=2019-05-06"
$headers = @{
    "api-key" = "$azureSearchAdminKey"
}

Write-Host "$CollectionName.$ItemType Checking existence..."

#calling Azure GET API to check the item is present
$items = Invoke-WebRequest -Method Get -Uri $url -Headers $headers
$returnValue = ($items.Content | ConvertFrom-Json).value
$itemExists = $returnValue -And $returnValue.name.Contains($ItemName)

Write-Host "$CollectionName.$ItemType Item exists: $itemExists"

If (-Not $itemExists)
{
    Write-Host "$CollectionName.$ItemType Returned value: '$($returnValue | Format-List | Out-String)', item name: '$ItemName'"

    #tell Azure to not return the item definition
    $headers.Add("Prefer", "return=minimal")

	Write-Host "$CollectionName.$ItemType Creating..."

    #calling Azure POST API to create the item
    Invoke-WebRequest -Method Post -Uri $url -Body $body -ContentType "application/json" -Headers $headers
}