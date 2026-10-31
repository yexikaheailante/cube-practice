$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$iconArgs = @()
if (Test-Path -LiteralPath "$PSScriptRoot\方体.ico") { $iconArgs = @('/win32icon:' + "$PSScriptRoot\方体.ico") }
& $compiler /nologo /optimize+ /target:winexe /utf8output /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /out:"$PSScriptRoot\方体练习.exe" @iconArgs "$PSScriptRoot\src\CubeCore.cs" "$PSScriptRoot\src\CubeCanvas.cs" "$PSScriptRoot\src\MainWindow.cs"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Get-Item -LiteralPath "$PSScriptRoot\方体练习.exe" | Select-Object Name, Length, LastWriteTime
