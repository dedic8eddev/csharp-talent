$scriptPath = split-path -parent $MyInvocation.MyCommand.Definition
$pfxPath = "$scriptPath\..\wildcard_talentis_global.pfx"
$pfxFileBytes = get-content $pfxPath -Encoding Byte

[System.Convert]::ToBase64String($pfxFileBytes) | Out-File "$scriptPath\..\wildcard_talentis_global.pfx.base64"