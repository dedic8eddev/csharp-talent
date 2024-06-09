
------------------------------------------------------------------------------------------------------------

TESTING ARM LOCALLY

1. Login to your Azure account with

Connect-AzureRmAccount

2. Choose the correct target Subscription with

Select-AzureRmSubscription <subscriptionId>

(you can list them with Get-AzureRmSubscription)

3. Deploy the specific Template/Params with

New-AzureRmResourceGroupDeployment -ResourceGroupName Dev-ParsnipsResGrp -TemplateFile .\Templates\WebSite.Template.json -TemplateParameterFile .\Api.WebSite.Dev.json

------------------------------------------------------------------------------------------------------------


