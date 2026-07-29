<#
  Simple installer for CustomDifficultyModForDeltarune (Windows)
  Usage (recommended): Right‑click → Run with PowerShell
  Optional switches:
    -Uninstall          Restore from most recent backup
    -NoBackup           Skip creating backups before patching
    -GameDir <path>     Path to DELTARUNE folder (skips auto‑detect)
    -UtmtCli <path>     Path to UndertaleModCli.exe (skips download/search)
    -NoModMenu          Skip installing Mod Menu
    -NoCustDiff         Skip installing Custom Difficulty
    -SkipFullGame       Skip searching for the full game and look for the demo
#>
param(
  [switch]$Uninstall,
  [switch]$NoBackup,
  [string]$GameDir,
  [string]$UtmtCli,
  [string]$NoModMenu,
  [string]$NoCustDiff
)

$ErrorActionPreference = 'Stop'
function Log($m){ Write-Host "`n[+] $m" }
function Warn($m){ Write-Host "`n[!] $m" -ForegroundColor Yellow }
function Die($m){ Write-Host "`n[ERROR] $m" -ForegroundColor Red; exit 1 }

# Resolve script & mod script paths
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ScriptsDir = Join-Path $ScriptDir 'src'
$ModMenuScript = (Join-Path $ScriptsDir 'modmenu.csx')
$CustDiffScript = (Join-Path $ScriptsDir 'customdifficulty_ch1to5.csx')

# Verify scripts exist
if (-not $NoModMenu) {
  if (-not (Test-Path $ModMenuScript)) {
    Die "Required script not found: $ModMenuScript"
  }
} else {
  Log 'Skipping Mod Menu (-NoModMenu flag set)'
}
if (-not $NoCustDiff) {
  if (-not (Test-Path $CustDiffScript)) {
    Die "Required script not found: $CustDiffScript"
  }
} else {
  Log 'Skipping Custom Difficulty (-NoCustDiff flag set)'
}

# --- find UndertaleModCli ---
function Get-UtmtCli {
  param([string]$Prefer)
  if ($Prefer -and (Test-Path $Prefer)) { return (Resolve-Path $Prefer).Path }
  
  # Check PATH
  $cmd = Get-Command UndertaleModCli.exe -ErrorAction SilentlyContinue
  if ($cmd) { return $cmd.Source }
  
  # Check cached copy and verify it works
  $cache = Join-Path $env:LOCALAPPDATA 'diffmod-utmt'
  $cachedExe = Join-Path $cache 'UndertaleModCli.exe'
  if (Test-Path $cachedExe) {
    try {
      $null = & $cachedExe --version 2>&1
      return $cachedExe
    } catch {
      # Cached copy doesn't work, will redownload
    }
  }

  # Download specific version (v0.9.1.2) to match macOS
  Log 'Downloading UndertaleModTool CLI (v0.9.1.2)...'
  $api = 'https://api.github.com/repos/UnderminersTeam/UndertaleModTool/releases/tags/0.9.1.2'
  try {
    $headers = @{ 'User-Agent' = 'CustomDifficultyModForDeltarune-Installer' }
    $json = Invoke-WebRequest -UseBasicParsing $api -Headers $headers | Select-Object -Expand Content | ConvertFrom-Json
  } catch {
    Die 'Failed to contact GitHub API'
  }
  
  # Find Windows CLI asset
  $asset = $json.assets | Where-Object { 
    $_.browser_download_url -match 'cli' -and 
    $_.browser_download_url -match '(win|windows)' 
  } | Select-Object -First 1
  
  if (-not $asset) { 
    Die 'Could not find a Windows CLI asset in v0.9.1.2 release. Supply -UtmtCli <path>.' 
  }
  
  $tmp = New-Item -ItemType Directory -Path ([IO.Path]::GetTempPath()) -Name ("utmt_" + [guid]::NewGuid())
  $archive = Join-Path $tmp 'utmt-cli.zip'
  Log "Fetching $($asset.browser_download_url)"
  
  # Use Invoke-WebRequest with proper redirect handling
  try {
    $headers = @{ 'User-Agent' = 'CustomDifficultyModForDeltarune-Installer' }
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $archive -Headers $headers -UseBasicParsing -MaximumRedirection 5
  } catch {
    Die "Download failed: $_"
  }
  
  # Verify the file was downloaded and has content
  if (-not (Test-Path $archive) -or (Get-Item $archive).Length -eq 0) {
    Die 'Download failed - file is empty or missing'
  }
  
  # Clear cache and extract ALL files
  if (Test-Path $cache) {
    Remove-Item -Recurse -Force $cache
  }
  New-Item -ItemType Directory -Force -Path $cache | Out-Null
  
  Log "Extracting archive..."
  Expand-Archive -Path $archive -DestinationPath $cache -Force
  
  # Find the exe in extracted files
  $exe = Get-ChildItem -Path $cache -Filter UndertaleModCli.exe -Recurse | Select-Object -First 1
  if (-not $exe) { 
    Die 'UndertaleModCli.exe not found in archive; supply -UtmtCli <path>.' 
  }
  
  # Test that it works
  try {
    $null = & $exe.FullName --version 2>&1
  } catch {
    Die 'Failed to get working UndertaleModCli'
  }
  
  # Clean up temp
  Remove-Item -Recurse -Force $tmp
  
  return $exe.FullName
}

# --- pick or detect game dir ---
function Detect-GameDir {
  $candidates = @()
  $defaultSteam = Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\common'
  if (Test-Path $defaultSteam) { $candidates += $defaultSteam }
  $libVdf = Join-Path ${env:ProgramFiles(x86)} 'Steam\steamapps\libraryfolders.vdf'
  if (Test-Path $libVdf) {
    $paths = Select-String -Path $libVdf -Pattern '"path"\s+"([^"]+)' | ForEach-Object { $_.Matches[0].Groups[1].Value }
    foreach ($p in $paths) { 
      try {
        # Check if the drive/path exists before trying to use it
        if (Test-Path $p) {
          $steamCommon = Join-Path $p 'steamapps\common'
          if (Test-Path $steamCommon) {
            $candidates += $steamCommon
          }
        }
      } catch {
        # Skip paths that cause errors (e.g., missing drives)
        continue
      }
    }
  }
  $found = $null
  if (-not $SkipFullGame) {
    foreach ($c in $candidates) {
      $try = Join-Path $c 'DELTARUNE'
      if (Test-Path $try) { $found = $try; break }
    }
  }
  # If no game found, try to find demo
  if (-not ($found)) {
      foreach ($c in $candidates) {
        $try = Join-Path $c 'DELTARUNEdemo'
        if (Test-Path $try) { $found = $try; break }
      }
  }
  if ($found) {
    Write-Host "Detected: $found"
    $ok = Read-Host 'Use this path? [Y/n]'
    if (-not $ok -or $ok -match '^(y|Y)$') { return $found }
  }
  # Picker
  Add-Type -AssemblyName System.Windows.Forms | Out-Null
  $dlg = New-Object System.Windows.Forms.FolderBrowserDialog
  $dlg.Description = 'Select your DELTARUNE folder (contains chapter folders)'
  if ($dlg.ShowDialog() -eq 'OK') { return $dlg.SelectedPath }
  $manual = Read-Host 'Enter full path to DELTARUNE folder'
  return $manual
}

# --- find chapter files ---
function Find-ChapterFiles {
  param([string]$GamePath)
  $files = @()
  for ($ch = 1; $ch -le 5; $ch++) {
    # Windows uses chapterX_windows\data.win pattern
    $f = Join-Path $GamePath "chapter${ch}_windows\data.win"
    if (Test-Path $f) { 
      $files += Get-Item $f 
    }
  }
  # If no chapter files found, assume this is the demo
  if ($files.Count -eq 0) {
    Log ("No chapter files found, attempting install for demo. ")
    $f = Join-Path $GamePath "data.win"
    if (Test-Path $f) { 
      $files += Get-Item $f 
    }
  }
  return $files
}

if (-not $GameDir -or -not (Test-Path $GameDir)) { $GameDir = Detect-GameDir }
if (-not (Test-Path $GameDir)) { Die "Path not found: $GameDir" }

# Find chapter game files
$targets = Find-ChapterFiles -GamePath $GameDir
if ($targets.Count -eq 0) { 
  Die 'No chapter data.win files found. Expected pattern: chapter1_windows\data.win, chapter2_windows\data.win, etc.' 
}

# Backups directory with timestamp
$ts = Get-Date -Format 'yyyyMMdd-HHmmss'
$backups = Join-Path $GameDir "ModBackups\$ts"

if ($Uninstall) {
  $root = Join-Path $GameDir 'ModBackups'
  if (-not (Test-Path $root)) {
    Die 'No backups folder found.'
  }
  $last = Get-ChildItem -Directory $root -ErrorAction SilentlyContinue | 
          Sort-Object Name -Descending | 
          Select-Object -First 1
  if (-not $last) { 
    Die 'No backups found to restore.' 
  }
  Log ("Restoring from backup: " + $last.FullName)
  
  # Properly restore files from backup
  Get-ChildItem -Path $last.FullName -Recurse -File | ForEach-Object {
    $rel = $_.FullName.Substring($last.FullName.Length).TrimStart('\','/')
    $destPath = Join-Path $GameDir $rel
    $destDir = Split-Path $destPath -Parent
    if (-not (Test-Path $destDir)) {
      New-Item -ItemType Directory -Force -Path $destDir | Out-Null
    }
    Copy-Item -LiteralPath $_.FullName -Destination $destPath -Force
    Write-Host "  restored: $rel"
  }
  
  Log 'Uninstall complete.'
  exit 0
}

# Find UTMT CLI
$UtmtExe = Get-UtmtCli -Prefer $UtmtCli
Log ("Using UndertaleModCli: " + $UtmtExe)

# Only create backup directory when installing (not uninstalling)
if (-not $NoBackup) {
  New-Item -ItemType Directory -Force -Path $backups | Out-Null
  
  # Backup originals
  Log ("Backing up originals to " + $backups + ' ...')
  foreach ($f in $targets) {
    $rel = $f.FullName.Substring($GameDir.Length).TrimStart('\','/')
    $destPath = Join-Path $backups $rel
    $destDir = Split-Path $destPath -Parent
    New-Item -ItemType Directory -Force -Path $destDir | Out-Null
    Copy-Item -LiteralPath $f.FullName -Destination $destPath -Force
    Write-Host ("  saved: " + $rel)
  }
} else {
  Log 'Skipping backup (-NoBackup flag set)'
}

# Apply patches to each chapter
Log 'Patching chapters ...'
foreach ($f in $targets) {
  Write-Host ("  -> " + $f.FullName)
  if (-not $NoModMenu) {
    & $UtmtExe load $f.FullName --scripts $ModMenuScript --verbose false --output $f.FullName
    if ($LASTEXITCODE -ne 0) { Die "Failed to apply first patch to $($f.Name)" }
  }
  if (-not $NoCustDiff) {
    & $UtmtExe load $f.FullName --scripts $CustDiffScript --verbose false --output $f.FullName
    if ($LASTEXITCODE -ne 0) { Die "Failed to apply second patch to $($f.Name)" }
  }
}

Log 'Done. Launch DELTARUNE and open the Mods menu to configure difficulty.'
