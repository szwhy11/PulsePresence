# PulsePresence Build Script
# Automatically locates MSBuild.exe and compiles PulsePresence.csproj (.NET Framework 4.8 / WPF)

$ErrorActionPreference = 'Stop'
$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $projectDir

# Close any running instances of PulsePresence before building to prevent file locking
Get-Process -Name 'PulsePresence' -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

Write-Host "Searching for MSBuild.exe..." -ForegroundColor Cyan

$msbuild = $null

# 1. Search via vswhere.exe
$vswhereCandidates = @(
    "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe",
    "$env:ProgramFiles\Microsoft Visual Studio\Installer\vswhere.exe"
)
foreach ($vp in $vswhereCandidates) {
    if (Test-Path $vp) {
        $found = & $vp -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if ($found -and (Test-Path $found)) {
            $msbuild = $found
            Write-Host "Found MSBuild via vswhere: $msbuild" -ForegroundColor Green
            break
        }
    }
}

# 2. Search in .NET Framework64 / Framework v4.0.30319
if (-not $msbuild) {
    $fw64 = "$env:windir\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
    $fw32 = "$env:windir\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
    if (Test-Path $fw64) {
        $msbuild = $fw64
        Write-Host "Found MSBuild in Framework64: $msbuild" -ForegroundColor Green
    } elseif (Test-Path $fw32) {
        $msbuild = $fw32
        Write-Host "Found MSBuild in Framework: $msbuild" -ForegroundColor Green
    }
}

# 3. Search in Program Files\Microsoft Visual Studio and BuildTools
if (-not $msbuild) {
    $vsRoots = @(
        "$env:ProgramFiles\Microsoft Visual Studio",
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio"
    )
    foreach ($vr in $vsRoots) {
        if (Test-Path $vr) {
            $found = Get-ChildItem -Path $vr -Filter "MSBuild.exe" -Recurse -ErrorAction SilentlyContinue | 
                Where-Object { $_.FullName -notmatch '\\arm64\\' } | 
                Select-Object -First 1 -ExpandProperty FullName
            if ($found) {
                $msbuild = $found
                Write-Host "Found MSBuild in Visual Studio directory: $msbuild" -ForegroundColor Green
                break
            }
        }
    }
}

# If still not found, show user-friendly instructions
if (-not $msbuild) {
    Write-Host ""
    Write-Host "==========================================================================" -ForegroundColor Red
    Write-Host "[ERROR] MSBuild.exe not found on this system!" -ForegroundColor Red
    Write-Host "==========================================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "To build PulsePresence, MSBuild is required. You can install it via:"
    Write-Host "  1. Download 'Visual Studio 2022 Build Tools':"
    Write-Host "     https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022"
    Write-Host "  2. In the installer, select the workload:"
    Write-Host "     - '.NET Desktop Build Tools' (includes .NET Framework 4.8 SDK & MSBuild)"
    Write-Host ""
    Write-Host "Or install Visual Studio 2022 Community with the .NET Desktop Development workload."
    Write-Host "==========================================================================" -ForegroundColor Red
    exit 1
}

Write-Host "Compiling PulsePresence.csproj..." -ForegroundColor Cyan
$projectFile = Join-Path $projectDir "PulsePresence.csproj"

& $msbuild $projectFile /p:Configuration=Release /v:m /nologo
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] Build failed with exit code $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}

$outputExe = Join-Path $projectDir "PulsePresence.exe"
if (Test-Path $outputExe) {
    Write-Host "Build Succeeded: $outputExe" -ForegroundColor Green
} else {
    Write-Host "[WARNING] Output PulsePresence.exe not found." -ForegroundColor Yellow
}
