$ErrorActionPreference = 'Stop' # stop on all errors
$toolsDir   = "$(Split-Path -parent $MyInvocation.MyCommand.Definition)"

$url        = 'https://github.com/DuncanMcPherson/gud/releases/download/v1.6.0/gud_1.6.0.msi' # download url, HTTPS preferred

$packageArgs = @{
  packageName   = $env:ChocolateyPackageName
  unzipLocation = $toolsDir
  fileType      = 'msi' 
  url           = $url

  softwareName  = 'gud*' 
  checksum      = '9B60ADDD5E5ED4BA318A72AC8DDB1A19CB10A69F9315A504E87EDB40CCB45031'
  checksumType  = 'sha256' 
  checksumType64= 'sha256' 
  
  silentArgs    = "/quiet /norestart /l*v `"$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`"" 
  validExitCodes= @(0, 3010, 1641)
}

Install-ChocolateyPackage @packageArgs