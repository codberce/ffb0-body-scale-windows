$ErrorActionPreference = 'Stop'
$localSdk = Join-Path $PSScriptRoot '..\.dotnet8\dotnet.exe'
$systemSdk = 'C:\Program Files\dotnet\dotnet.exe'

if (Test-Path -LiteralPath $localSdk) {
    $dotnet = (Resolve-Path -LiteralPath $localSdk).Path
} elseif (Test-Path -LiteralPath $systemSdk) {
    $dotnet = $systemSdk
} else {
    throw '.NET 8 SDK was not found. Install it or restore the workspace-local SDK in work\.dotnet8.'
}

$publishDir = Join-Path $PSScriptRoot 'publish'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

& $dotnet publish (Join-Path $PSScriptRoot 'CantarClinica.csproj') `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir `
    --nologo

if ($LASTEXITCODE -ne 0) {
    throw "Modern native-print build failed with exit code $LASTEXITCODE"
}

Get-Item -LiteralPath (Join-Path $publishDir 'FI2319 Clinic Scale.exe')
