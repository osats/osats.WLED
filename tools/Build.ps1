param([string]$SimHubDir = 'C:\Program Files (x86)\SimHub')
$ErrorActionPreference = 'Stop'
$repo = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
if (!(Test-Path -LiteralPath $vswhere)) { throw 'Install Visual Studio Build Tools and the .NET Framework 4.8 targeting pack.' }
$msbuild = & $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (!$msbuild) { throw 'MSBuild not found.' }
& $msbuild (Join-Path $repo 'osats.WLED.csproj') /t:Rebuild /p:Configuration=Release "/p:SimHubDir=$SimHubDir" /verbosity:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Plugin build failed.' }
& $msbuild (Join-Path $repo 'tests\Tests.csproj') /t:Rebuild /verbosity:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
& (Join-Path $repo 'tests\bin\WLED.Tests.exe')
if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
$package = Join-Path $repo 'artifacts\osats.WLED-2.0.0'
[IO.Directory]::CreateDirectory($package) | Out-Null
Copy-Item -LiteralPath (Join-Path $repo 'bin\Release\osats.WLED.dll') -Destination $package
Copy-Item -LiteralPath (Join-Path $repo 'README.md'), (Join-Path $repo 'LICENSE'), (Join-Path $repo 'CHANGELOG.md') -Destination $package
Compress-Archive -Path (Join-Path $package '*') -DestinationPath (Join-Path $repo 'artifacts\osats.WLED-2.0.0.zip') -Force
Write-Output "Package: $repo\artifacts\osats.WLED-2.0.0.zip"
