#Requires -Module Az.Resources

param (
	[Parameter(Mandatory = $true)]
	[string]$ResourceGroupName,

	[Parameter(Mandatory = $false)]
	[int]$Days = 5
) 

Write-Host "Finding deployments older than $Days days for $ResourceGroupName"
$deployments = Get-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName
$deploymentsToDelete = $deployments | where { $_.Timestamp -lt ((get-date).AddDays(-$Days)) }

Write-Host "Found $($deploymentsToDelete.Length) deployments"

foreach($deployment in $deploymentsToDelete) {
	Write-Host "Deleting $($deployment.DeploymentName)"
    Remove-AzResourceGroupDeployment -ResourceGroupName $ResourceGroupName -Name $deployment.DeploymentName
}
