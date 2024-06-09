<#
 .SYNOPSIS
	Creates the Deployment KeyVault (if not created) and ensures it is setup for use in Deployment.

 .DESCRIPTION
	Runs the KeyVault ARM Template to ensure the Deployment KeyVault is created and setup correcty.
#>

param (	
	[Parameter(Mandatory = $true)]
	#The subscription id where the Deployment KeyVault will be deployed.  Use the LIVE Subscription for this. (Use Get-AzureRmSubscription to find)
	[string]$SubscriptionId,
	
	[Parameter(Mandatory = $true)]
	#Whether this is the Internal (DEV + TEST) or Live KeyVault.
	[bool]$Internal, 

	#The ObjectId of the DEV environment ServicePrincipal (SPN) that will be running the Deployments from VSTS. Use VSTS > Settings > Services > (Pick SP) > Manage Service Principal to get the description then use e.g. Get-AzureRmADServicePrincipal -SearchString "VSTS" or "voy-dev"
	[string]$DevServicePrincipalObjectId,  

	#The ObjectId of the LIVE environment ServicePrincipal (SPN) that will be running the Deployments from VSTS. Use VSTS > Settings > Services > (Pick SP) > Manage Service Principal to get the description then use e.g. Get-AzureRmADServicePrincipal -SearchString "VSTS" or "voy-dev"
	[string]$LiveServicePrincipalObjectId,  
		
	[Parameter(Mandatory = $true)]
	#The resource group where the KeyVault will be created. Should be separate; NOT be one environments are deployed into.
	[string]$ResourceGroupName, 

	#Optional, a resource group location. If specified, will try to create a new resource group in this location. If not specified, assumes resource group is existing.
	[string]$ResourceGroupLocation
)

$TemplateFile = "..\Templates\KeyVault.Template.json"
$ErrorActionPreference = "Stop"

# TODO: Move this to helper function/script
if ($Internal) 
{
	$KeyVaultName = "IntParsnipsDeploymentKV"
}
else
{
	$KeyVaultName = "LiveParsnipsDeploymentKV"
}


#################################################################################
## Login to Azure and select subscription
#################################################################################
Write-Output "Selecting subscription '$SubscriptionId'..."

try
{
	Select-AzureRmSubscription -SubscriptionID $SubscriptionId
}
catch 
{	
	if ($_ -like "*Login-AzureRmAccount to login*") 
	{
		Write-Output "Logging in..."
		Login-AzureRmAccount
		Select-AzureRmSubscription -SubscriptionID $SubscriptionId
	}
	else
	{
		throw # Note, powershell re-throwing doesn't keep correct location of exception source...
	}
}

#################################################################################
## Get Tenant For Subscription
#################################################################################
Write-Output "Getting subscription tenant..."

$subscription = Get-AzureRmSubscription -SubscriptionID $SubscriptionId
$tenantId = $subscription.TenantId

Write-Output "Got tenant '$tenantId' for '$($subscription.Name)'"

#################################################################################
## Ensure Resource Group is created
#################################################################################
$resourceGroup = Get-AzureRmResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if ( -not $ResourceGroup ) {
	Write-Output "Could not find resource group '$ResourceGroupName' - will create it"
	if ( -not $ResourceGroupLocation ) {
		$ResourceGroupLocation = Read-Host -Prompt 'Enter location for resource group'
	}
	Write-Output "Creating resource group '$ResourceGroupName' in location '$ResourceGroupLocation'"
	New-AzureRmResourceGroup -Name $resourceGroupName -Location $resourceGroupLocation
}
else {
	Write-Output "Using existing resource group '$ResourceGroupName'"
}

#################################################################################
## Grant SPNs access to Resource Group
#################################################################################
$roleScope = "/subscriptions/$SubscriptionId/resourceGroups/$ResourceGroupName"

if ($Internal)
{
	Write-Output "Granting DEV SPN Contributor access to resource group..."
	if ((Get-AzureRmRoleAssignment -RoleDefinitionName Contributor -ObjectId $DevServicePrincipalObjectId -Scope $roleScope -ErrorAction SilentlyContinue) -eq $null) {
		New-AzureRmRoleAssignment -RoleDefinitionName Contributor -ObjectId $DevServicePrincipalObjectId -Scope $roleScope
	}
}
else
{
	Write-Output "Granting LIVE SPN Contributor access to resource group..."
	if ((Get-AzureRmRoleAssignment -RoleDefinitionName Contributor -ObjectId $LiveServicePrincipalObjectId -Scope $roleScope -ErrorAction SilentlyContinue) -eq $null) {
		New-AzureRmRoleAssignment -RoleDefinitionName Contributor -ObjectId $LiveServicePrincipalObjectId -Scope $roleScope
	}
}

#################################################################################
## Create KeyVault and Policies
#################################################################################

if ($Internal)
{
	$templateParameters = @{"keyVaultName"=$KeyVaultName;"tenantId"=$subscription.TenantId;"objectId"=$DevServicePrincipalObjectId;secretsPermissions = ("Get", "List");"enabledForTemplateDeployment"=$true;}

	Write-Output "Deploying KeyVault with DEV Service Principal access policy..."
	New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile $TemplateFile -TemplateParameterObject $templateParameters		
}
else
{
	$templateParameters = @{"keyVaultName"=$KeyVaultName;"tenantId"=$subscription.TenantId;"objectId"=$LiveServicePrincipalObjectId;secretsPermissions = ("Get", "List");"enabledForTemplateDeployment"=$true;}

	Write-Output "Deploying KeyVault with LIVE Service Principal access policy..."
	New-AzureRmResourceGroupDeployment -ResourceGroupName $ResourceGroupName -TemplateFile $TemplateFile -TemplateParameterObject $templateParameters
}

#################################################################################
## Grant Permission for KeyVault to Subscription SPN used by Azure
#################################################################################

# Hardcoded ServicePrincipalName refers to:
# Microsoft.Azure.WebSites       ServicePrincipal               fe456c75-f60a-4a20-8d1e-daf7ce650d6e

# Get-AzureRmADServicePrincipal -SearchString "Microsoft Azure App Service"

Write-Output "Adding Azure Subscription Service Principal access policy..."
Set-AzureRmKeyVaultAccessPolicy -VaultName $KeyVaultName -ServicePrincipalName abfa0a7c-a6b6-4736-8310-5855508787cd -PermissionsToSecrets get 

#################################################################################
## Print New KeyVault Resource ID string
#################################################################################
Write-Output "Getting KeyVault resource ID..."

$keyVault = Get-AzureRmKeyVault -VaultName $KeyVaultName
Write-Output "Use the following resource ID in ARM parameters:"
Write-Output ""
Write-Output $keyVault.ResourceId
Write-Output ""