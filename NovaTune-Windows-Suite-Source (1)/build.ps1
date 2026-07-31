[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [ValidateSet('x64', 'ARM64')]
    [string]$Platform = 'x64'
)

$ErrorActionPreference = 'Stop'
$solution = Join-Path $PSScriptRoot 'NovaTune.sln'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw '.NET 9 SDK was not found. Install it with the Windows App SDK development workload.'
}

dotnet restore $solution -p:Platform=$Platform
dotnet build $solution -c $Configuration -p:Platform=$Platform --no-restore
dotnet test (Join-Path $PSScriptRoot 'tests\NovaTune.Core.Tests\NovaTune.Core.Tests.csproj') -c $Configuration -p:Platform=$Platform --no-build

Write-Host "NovaTune build and tests completed successfully." -ForegroundColor Green
