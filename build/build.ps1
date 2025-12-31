#!/usr/bin/env pwsh
#Requires -Version 5.1
$ErrorActionPreference = "Stop"

# === Configuration ===
$script:BuildConfig = "Release"
$script:Verbosity = "minimal"  # quiet | minimal | normal | detailed
$script:FailedSteps = @()

# === Helper Functions ===
function Write-Step {
    param([string]$Message, [string]$Color = "Cyan")
    Write-Host "`n$Message" -ForegroundColor $Color
}

function Write-Success {
    param([string]$Message)
    Write-Host "  ✅ $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "  ⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "  ❌ $Message" -ForegroundColor Red
}

function Invoke-BuildStep {
    param(
        [string]$Name,
        [scriptblock]$Action,
        [bool]$CriticalStep = $true
    )
    
    Write-Step "[$Name]" "Magenta"
    try {
        $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
        & $Action
        $stopwatch.Stop()
        Write-Success "$Name completed in $($stopwatch.Elapsed.TotalSeconds)s"
        return $true
    }
    catch {
        $stopwatch.Stop()
        Write-Error "$Name failed: $($_.Exception.Message)"
        $script:FailedSteps += $Name
        
        if ($CriticalStep) {
            Write-Host "`n💥 Build failed at critical step: $Name" -ForegroundColor Red
            exit 1
        }
        return $false
    }
}

# === STEP 0: Setup ===
$projectRoot = Resolve-Path "$PSScriptRoot/.."
$distPath = "$projectRoot/dist"

Write-Host @"
╔════════════════════════════════════════════════╗
║     PhantomExe VM Protector Build System      ║
║            Multi-TFM Architecture              ║
╚════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

Write-Host "📂 Project Root: $projectRoot" -ForegroundColor Gray
Write-Host "🎯 Build Config: $script:BuildConfig" -ForegroundColor Gray
Write-Host ""

# === STEP 1: Clean ===
Invoke-BuildStep "CLEAN" {
    Write-Host "  → Removing bin/obj folders..." -ForegroundColor DarkGray
    Get-ChildItem $projectRoot -Include bin,obj -Recurse -Directory | ForEach-Object {
        Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    }
    
    Write-Host "  → Clearing NuGet cache..." -ForegroundColor DarkGray
    dotnet nuget locals all --clear | Out-Null
    
    if (Test-Path $distPath) {
        Write-Host "  → Removing old dist folder..." -ForegroundColor DarkGray
        Remove-Item $distPath -Recurse -Force
    }
} -CriticalStep $false

# === STEP 2: Create Output Directories ===
Invoke-BuildStep "SETUP" {
    $cliPath = "$distPath/cli"
    $guiPath = "$distPath/gui"
    $corePath = "$distPath/core"
    
    @($distPath, $cliPath, $guiPath, $corePath) | ForEach-Object {
        if (-not (Test-Path $_)) { 
            New-Item -ItemType Directory -Path $_ | Out-Null 
            Write-Host "  → Created: $_" -ForegroundColor DarkGray
        }
    }
    
    # Store paths in script scope
    $script:cliPath = $cliPath
    $script:guiPath = $guiPath
    $script:corePath = $corePath
}

Set-Location $projectRoot

# === STEP 3: Build Shared Layer ===
Invoke-BuildStep "SHARED" {
    dotnet build src/Shared `
        -c $script:BuildConfig `
        -v $script:Verbosity `
        --nologo
}

# === STEP 4: Build Core Layers (TFM-Specific) ===
Invoke-BuildStep "CORE-NETSTANDARD2.0" {
    dotnet build src/Core/netstandard2.0 `
        -c $script:BuildConfig `
        -v $script:Verbosity `
        -p:DisableParallelBuild=true `
        --nologo
}

Invoke-BuildStep "CORE-NET6.0" {
    dotnet build src/Core/net6.0 `
        -c $script:BuildConfig `
        -v $script:Verbosity `
        -p:DisableParallelBuild=true `
        --nologo
}

# === STEP 5: Build Stub ===
Invoke-BuildStep "STUB" {
    dotnet build src/Stub `
        -c $script:BuildConfig `
        -v $script:Verbosity `
        --nologo
}

# === STEP 6: Build Protector (Multi-TFM) ===
Invoke-BuildStep "PROTECTOR" {
    dotnet build src/Protector `
        -c $script:BuildConfig `
        -v $script:Verbosity `
        --nologo
}

# === STEP 7: Build CLI ===
Invoke-BuildStep "CLI" {
    # Restore packages first
    dotnet restore src/CLI --nologo | Out-Null
    dotnet build src/CLI `
        -c $script:BuildConfig `
        -v $script:Verbosity `
        --nologo
}

# === STEP 8: Build WinForms GUI ===
Invoke-BuildStep "GUI" {
    dotnet build src/WinForms `
        -c $script:BuildConfig `
        -v $script:Verbosity `
        --nologo
}

# === STEP 9: Publish CLI ===
Invoke-BuildStep "PUBLISH-CLI" {
    dotnet publish src/CLI `
        -c $script:BuildConfig `
        -r win-x64 `
        --self-contained false `
        -o $script:cliPath `
        -p:DisableParallelBuild=true `
        -p:PublishSingleFile=false `
        --nologo `
        -v $script:Verbosity
    
    # ✅ Copy Core DLLs to CLI output (required for embedding)
    Write-Host "  → Copying Core DLLs to CLI..." -ForegroundColor DarkGray
    $coreNs20 = "src/Core/netstandard2.0/bin/$script:BuildConfig/netstandard2.0/PhantomExe.VM.Core.netstandard2.0.dll"
    $coreNet6 = "src/Core/net6.0/bin/$script:BuildConfig/net6.0/PhantomExe.VM.Core.net6.0.dll"
    
    if (Test-Path $coreNs20) {
        Copy-Item $coreNs20 -Destination "$script:cliPath/PhantomExe.Core.dll" -Force
    }
    if (Test-Path $coreNet6) {
        Copy-Item $coreNet6 -Destination "$script:cliPath/PhantomExe.Core.modern.dll" -Force
    }
}

# === STEP 10: Publish GUI ===
Invoke-BuildStep "PUBLISH-GUI" {
    dotnet publish src/WinForms `
        -c $script:BuildConfig `
        -r win-x64 `
        --self-contained true `
        -o $script:guiPath `
        -p:DisableParallelBuild=true `
        -p:PublishSingleFile=false `
        --nologo `
        -v $script:Verbosity
    
    # ✅ Copy Core DLLs to GUI output (required for embedding)
    Write-Host "  → Copying Core DLLs to GUI..." -ForegroundColor DarkGray
    $coreNs20 = "src/Core/netstandard2.0/bin/$script:BuildConfig/netstandard2.0/PhantomExe.VM.Core.netstandard2.0.dll"
    $coreNet6 = "src/Core/net6.0/bin/$script:BuildConfig/net6.0/PhantomExe.VM.Core.net6.0.dll"
    
    if (Test-Path $coreNs20) {
        Copy-Item $coreNs20 -Destination "$script:guiPath/PhantomExe.Core.dll" -Force
    }
    if (Test-Path $coreNet6) {
        Copy-Item $coreNet6 -Destination "$script:guiPath/PhantomExe.Core.modern.dll" -Force
    }
}

# === STEP 11: Copy Core Runtimes ===
Invoke-BuildStep "COPY-RUNTIMES" {
    # ✅ FIXED: Correct paths
    $coreNs20 = "src/Core/netstandard2.0/bin/$script:BuildConfig/netstandard2.0/PhantomExe.VM.Core.netstandard2.0.dll"
    $coreNet6 = "src/Core/net6.0/bin/$script:BuildConfig/net6.0/PhantomExe.VM.Core.net6.0.dll"
    
    Write-Host "  → Checking Core DLLs..." -ForegroundColor DarkGray
    
    if (-not (Test-Path $coreNs20)) {
        throw "netstandard2.0 Core DLL not found at: $coreNs20"
    }
    Write-Host "    ✓ Found: netstandard2.0 Core" -ForegroundColor DarkGray
    
    if (-not (Test-Path $coreNet6)) {
        throw "net6.0 Core DLL not found at: $coreNet6"
    }
    Write-Host "    ✓ Found: net6.0 Core" -ForegroundColor DarkGray
    
    Write-Host "  → Copying Core runtimes..." -ForegroundColor DarkGray
    Copy-Item $coreNs20 -Destination "$script:corePath/PhantomExe.Core.legacy.dll" -Force
    Copy-Item $coreNet6 -Destination "$script:corePath/PhantomExe.Core.modern.dll" -Force
    Copy-Item $coreNs20 -Destination $script:cliPath -Force
    Copy-Item $coreNet6 -Destination $script:guiPath -Force
}

# === STEP 12: Copy Stub Runtime ===
Invoke-BuildStep "COPY-STUB" {
    $stubDll = "src/Stub/bin/$script:BuildConfig/netstandard2.0/PhantomExe.Stub.dll"
    
    if (Test-Path $stubDll) {
        Write-Host "  → Copying Stub runtime..." -ForegroundColor DarkGray
        Copy-Item $stubDll -Destination "$script:corePath/PhantomExe.Stub.dll" -Force
        Copy-Item $stubDll -Destination $script:cliPath -Force
        Copy-Item $stubDll -Destination $script:guiPath -Force
        Write-Host "    ✓ Stub copied to all targets" -ForegroundColor DarkGray
    } else {
        Write-Warning "Stub DLL not found (optional)"
    }
} -CriticalStep $false

# === STEP 13: Generate Runtime Manifest ===
Invoke-BuildStep "MANIFEST" {
    $manifest = @"
{
  "runtime": {
    "legacy": "PhantomExe.Core.legacy.dll",
    "modern": "PhantomExe.Core.modern.dll",
    "stub": "PhantomExe.Stub.dll"
  },
  "build": {
    "date": "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')",
    "config": "$script:BuildConfig",
    "architecture": "win-x64"
  }
}
"@
    $manifest | Out-File -FilePath "$script:corePath/runtime.json" -Encoding utf8 -NoNewline
    Write-Host "  → Created runtime.json" -ForegroundColor DarkGray
}

# === STEP 14: Create ZIP Packages ===
Invoke-BuildStep "PACKAGE" {
    if ($PSVersionTable.PSVersion.Major -ge 5) {
        Write-Host "  → Creating CLI package..." -ForegroundColor DarkGray
        Compress-Archive -Path "$script:cliPath\*" `
            -DestinationPath "$distPath\phantomexe-cli-win-x64.zip" -Force
        
        Write-Host "  → Creating GUI package..." -ForegroundColor DarkGray
        Compress-Archive -Path "$script:guiPath\*" `
            -DestinationPath "$distPath\phantomexe-gui-win-x64.zip" -Force
        
        Write-Host "  → Creating Core runtimes package..." -ForegroundColor DarkGray
        Compress-Archive -Path "$script:corePath\*" `
            -DestinationPath "$distPath\phantomexe-core-runtimes.zip" -Force
        
        Write-Host "    ✓ All packages created" -ForegroundColor DarkGray
    } else {
        Write-Warning "PowerShell 5+ required for ZIP creation"
    }
} -CriticalStep $false

# === STEP 15: Summary ===
Write-Host ""
Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║           BUILD COMPLETED SUCCESSFULLY         ║" -ForegroundColor Green
Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""

Write-Host "📦 Output Files:" -ForegroundColor Cyan
Write-Host "   CLI:  $((Resolve-Path $script:cliPath).Path)\PhantomExe.CLI.exe" -ForegroundColor Gray
Write-Host "   GUI:  $((Resolve-Path $script:guiPath).Path)\PhantomExe.WinForms.exe" -ForegroundColor Gray
Write-Host "   Core: $((Resolve-Path $script:corePath).Path)" -ForegroundColor Gray
Write-Host ""

if (Test-Path "$distPath\phantomexe-cli-win-x64.zip") {
    $cliSize = (Get-Item "$distPath\phantomexe-cli-win-x64.zip").Length / 1MB
    $guiSize = (Get-Item "$distPath\phantomexe-gui-win-x64.zip").Length / 1MB
    $coreSize = (Get-Item "$distPath\phantomexe-core-runtimes.zip").Length / 1MB
    
    Write-Host "📦 Package Sizes:" -ForegroundColor Cyan
    Write-Host "   CLI:  $([math]::Round($cliSize, 2)) MB" -ForegroundColor Gray
    Write-Host "   GUI:  $([math]::Round($guiSize, 2)) MB" -ForegroundColor Gray
    Write-Host "   Core: $([math]::Round($coreSize, 2)) MB" -ForegroundColor Gray
    Write-Host ""
}

Write-Host "📁 Distribution: $(Resolve-Path $distPath)" -ForegroundColor Cyan
Write-Host ""

if ($script:FailedSteps.Count -gt 0) {
    Write-Host "⚠️  Some non-critical steps failed:" -ForegroundColor Yellow
    $script:FailedSteps | ForEach-Object { Write-Host "   - $_" -ForegroundColor Yellow }
    Write-Host ""
}

# Return to original directory
Set-Location $projectRoot

exit 0