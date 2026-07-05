#Requires -Version 5.1
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SelfContained = $true,
    [string]$OutputDirectory = "",
    [switch]$Zip = $false,
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $RepoRoot "publish"
}

$ProjectPath = Join-Path $RepoRoot "src" | Join-Path -ChildPath "PitStopVR.App" | Join-Path -ChildPath "PitStopVR.App.csproj"
$VersionFile = Join-Path $RepoRoot "VERSION.txt"

if (-not (Test-Path $ProjectPath)) {
    Write-Error "No se encontró el proyecto en $ProjectPath"
    exit 1
}

$DotNetVersion = dotnet --version 2>$null
if (-not $?) {
    Write-Error "No se encontró dotnet. Instalá el .NET 9 SDK desde https://dotnet.microsoft.com/en-us/download/dotnet/9.0"
    exit 1
}

Write-Host "SDK .NET detectado: $DotNetVersion" -ForegroundColor Cyan

# Resuelve la versión: parámetro > VERSION.txt > 1.0.0
$ResolvedVersion = "1.0.0"
if (-not [string]::IsNullOrWhiteSpace($Version)) {
    $ResolvedVersion = $Version
}
elseif (Test-Path $VersionFile) {
    $ResolvedVersion = (Get-Content $VersionFile -Raw).Trim()
}

Write-Host "Publicando PitStopVR v$ResolvedVersion..." -ForegroundColor Cyan
Write-Host "  Configuración: $Configuration" -ForegroundColor Gray
Write-Host "  Runtime: $Runtime" -ForegroundColor Gray
Write-Host "  Self-contained: $SelfContained" -ForegroundColor Gray
Write-Host "  Salida: $OutputDirectory" -ForegroundColor Gray

if (Test-Path $OutputDirectory) {
    Write-Host "Limpiando salida anterior..." -ForegroundColor Gray
    Remove-Item -Path $OutputDirectory -Recurse -Force
}

$SelfContainedFlag = if ($SelfContained) { "true" } else { "false" }

& dotnet publish $ProjectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained $SelfContainedFlag `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=embedded `
    -o $OutputDirectory

if (-not $?) {
    Write-Error "La publicación falló. Revisá los errores de arriba."
    exit 1
}

# Copiar scripts de instalación junto al ejecutable
$InstallerSource = Join-Path $RepoRoot "installer"
if (Test-Path $InstallerSource) {
    Copy-Item -Path (Join-Path $InstallerSource "*.bat") -Destination $OutputDirectory -Force -ErrorAction SilentlyContinue
}

# Crear ZIP si se solicitó
if ($Zip) {
    $ZipPath = Join-Path $RepoRoot "PitStopVR-v$ResolvedVersion-$Runtime.zip"
    if (Test-Path $ZipPath) {
        Remove-Item $ZipPath -Force
    }
    Compress-Archive -Path "$OutputDirectory\*" -DestinationPath $ZipPath -Force
    Write-Host "Paquete ZIP creado: $ZipPath" -ForegroundColor Green
}

Write-Host "Publicación completada en: $OutputDirectory" -ForegroundColor Green
Write-Host "Ejecutable: $(Join-Path $OutputDirectory "PitStopVR.App.exe")" -ForegroundColor Green
