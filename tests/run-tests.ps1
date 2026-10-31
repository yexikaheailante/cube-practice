$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $PSScriptRoot
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe /out:"$PSScriptRoot\CoreTests.exe" "$projectDir\src\CubeCore.cs" "$PSScriptRoot\CoreTests.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$PSScriptRoot\CoreTests.exe"
exit $LASTEXITCODE
