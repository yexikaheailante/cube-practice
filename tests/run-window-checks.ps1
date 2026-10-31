$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $PSScriptRoot
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $compiler /nologo /target:exe /main:WindowChecks /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /out:"$PSScriptRoot\WindowChecks.exe" "$projectDir\src\CubeCore.cs" "$projectDir\src\CubeCanvas.cs" "$projectDir\src\MainWindow.cs" "$PSScriptRoot\WindowChecks.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& "$PSScriptRoot\WindowChecks.exe"
exit $LASTEXITCODE
