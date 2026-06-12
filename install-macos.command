#!/usr/bin/env bash
set -euo pipefail

# === Simple installer for CustomDifficultyModForDeltarune (macOS) ===
# Usage:
#   Double‑click in Finder (or: chmod +x install-macos.command && ./install-macos.command)
#   Optional flags: --uninstall    Restore from most recent backup
#                   --no-backup    Skip creating backups before patching
#                   --app <path>   Path to DELTARUNE.app (skips picker)
#                   --utmt <path>  Path to UndertaleModCli (skips download/search)

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SCRIPTS_DIR="$SCRIPT_DIR/src"
MOD_SCRIPTS=("$SCRIPTS_DIR/modmenu_ch1to5.csx" "$SCRIPTS_DIR/customdifficulty_ch1to5.csx")
CACHE_DIR="$HOME/.cache/diffmod-utmt"
mkdir -p "$CACHE_DIR"

UNINSTALL=0
NO_BACKUP=0
APP_PATH=""
UTMT_CLI=""

# --- parse args ---
while [[ $# -gt 0 ]]; do
  case "$1" in
    --uninstall) UNINSTALL=1; shift;;
    --no-backup) NO_BACKUP=1; shift;;
    --app) APP_PATH="${2:-}"; shift 2;;
    --utmt) UTMT_CLI="${2:-}"; shift 2;;
    *) echo "Unknown argument: $1"; exit 1;;
  esac
done

# --- helper: pretty echo ---
log() { printf "\n[+] %s\n" "$*" >&2; }
warn() { printf "\n[!] %s\n" "$*" >&2; }
err() { printf "\n[ERROR] %s\n" "$*" >&2; exit 1; }

# --- helper: find or download UndertaleModCli ---
find_utmt_cli() {
  # 1) If provided explicitly
  if [[ -n "$UTMT_CLI" && -x "$UTMT_CLI" ]]; then echo "$UTMT_CLI"; return; fi

  # 2) PATH
  if command -v UndertaleModCli >/dev/null 2>&1; then command -v UndertaleModCli; return; fi

  # 3) Cached copy - check if the tool works, not just if binary exists
  if [[ -x "$CACHE_DIR/UndertaleModCli" ]]; then 
    if "$CACHE_DIR/UndertaleModCli" --version >/dev/null 2>&1; then
      echo "$CACHE_DIR/UndertaleModCli"
      return
    fi
  fi

  # 4) Download specific release (v0.8.3.0) asset that looks like macOS CLI
  log "Downloading UndertaleModTool CLI (v0.8.3.0) …"
  API_URL="https://api.github.com/repos/UnderminersTeam/UndertaleModTool/releases/tags/0.8.3.0"
  JSON="$(curl -fsSL "$API_URL")" || err "Failed to contact GitHub API"
  # Try to pick a macOS CLI asset (zip or tar.gz). We look for 'CLI' and 'mac' in the URL.
  DL_URL=$(printf "%s" "$JSON" | grep -oE '"browser_download_url"\s*:\s*"[^"]+"' | cut -d '"' -f 4 | \
           grep -iE 'cli' | grep -iE 'mac|osx|darwin' | head -n1)
  [[ -n "$DL_URL" ]] || err "Could not find a macOS CLI asset in v0.8.3.0 release. Supply --utmt <path> instead."

  TMPD="$(mktemp -d)"
  ARCHIVE="$TMPD/utmt-cli.zip"
  log "Fetching $DL_URL"
  curl -fL "$DL_URL" -o "$ARCHIVE" || err "Download failed"
  
  log "Extracting archive..."
  # Clear cache dir and extract all files there
  rm -rf "$CACHE_DIR"
  mkdir -p "$CACHE_DIR"
  unzip -q "$ARCHIVE" -d "$CACHE_DIR"
  
  # Make the main binary executable
  chmod +x "$CACHE_DIR/UndertaleModCli"
  
  # Remove macOS quarantine attribute from all files
  xattr -dr com.apple.quarantine "$CACHE_DIR" 2>/dev/null || true
  
  # Test that it works
  if ! "$CACHE_DIR/UndertaleModCli" --version >/dev/null 2>&1; then
    err "Failed to get working UndertaleModCli"
  fi
  
  # Clean up temp dir
  rm -rf "$TMPD"
  
  echo "$CACHE_DIR/UndertaleModCli"
}

# --- helper: choose DELTARUNE.app ---
pick_app_if_needed() {
  if [[ -n "$APP_PATH" ]]; then return; fi

  # Try common Steam location first
  DEFAULT_APP="$HOME/Library/Application Support/Steam/steamapps/common/DELTARUNE/DELTARUNE.app"
  if [[ -d "$DEFAULT_APP" ]]; then APP_PATH="$DEFAULT_APP"; fi

  # If no game found, try to find demo
  if [[ -z "$APP_PATH" ]]; then
    DEFAULT_DEMO_APP="$HOME/Library/Application Support/Steam/steamapps/common/DELTARUNEdemo/DELTARUNE.app"
    if [[ -d "$DEFAULT_DEMO_APP" ]]; then APP_PATH="$DEFAULT_DEMO_APP"; fi
  fi

  # Ask user to confirm or pick
  if [[ -n "$APP_PATH" ]]; then
    echo "Detected: $APP_PATH"
    read -r -p "Use this path? [Y/n] " resp
    if [[ "${resp:-Y}" =~ ^(n|N)$ ]]; then APP_PATH=""; fi
  fi

  if [[ -z "$APP_PATH" ]]; then
    if command -v osascript >/dev/null 2>&1; then
      APP_PATH=$(osascript <<'APP'
        set p to POSIX path of (choose file with prompt "Select DELTARUNE.app" of type {"app"})
        return p
APP
      ) || true
    fi
  fi

  if [[ -z "$APP_PATH" ]]; then
    read -r -p "Enter full path to DELTARUNE.app: " APP_PATH
  fi

  [[ -d "$APP_PATH" ]] || err "Path not found: $APP_PATH"
}

# --- helper: list chapter files ---
find_chapter_files() {
  local RES="$1"
  local files=()
  for ch in 1 2 3 4 5; do
    local f="$RES/chapter${ch}_mac/game.ios"
    [[ -f "$f" ]] && files+=("$f")
  done
  # If no chapter files found, assume this is the demo
  if (( ${#files[@]} == 0 )); then
    log "No chapter files found, attempting install for demo. "
    local f="$RES/game.ios"
    [[ -f "$f" ]] && files+=("$f")
  fi
  printf '%s\n' "${files[@]:-}"
}

# --- main ---
UTMT_CLI=$(find_utmt_cli)
log "Using UndertaleModCli: $UTMT_CLI"

pick_app_if_needed
RESOURCES="$APP_PATH/Contents/Resources"
[[ -d "$RESOURCES" ]] || err "Resources folder not found: $RESOURCES"

# Gather targets
IFS=$'\n' read -r -d '' -a TARGETS < <(find_chapter_files "$RESOURCES" && printf '\0')
if [[ ${#TARGETS[@]} -eq 0 ]]; then err "No chapter game files found under $RESOURCES"; fi

# Backups dir
TS="$(date +%Y%m%d-%H%M%S)"
BACKUPS="$RESOURCES/ModBackups/$TS"

if [[ $UNINSTALL -eq 1 ]]; then
  # Restore most recent backup
  LAST="$(ls -1dt "$RESOURCES/ModBackups"/* 2>/dev/null | head -n1 || true)"
  [[ -n "$LAST" ]] || err "No backups found to restore."
  log "Restoring from backup: $LAST"
  rsync -a "$LAST/" "$RESOURCES/" || err "Restore failed"
  log "Uninstall complete."
  exit 0
fi

# Only create backup directory when installing (not uninstalling)
if [[ $NO_BACKUP -eq 0 ]]; then
  mkdir -p "$BACKUPS"
  log "Backing up originals to $BACKUPS …"
  for f in "${TARGETS[@]}"; do
    rel="${f#"$RESOURCES/"}"
    mkdir -p "$BACKUPS/$(dirname "$rel")"
    cp -n "$f" "$BACKUPS/$rel"
    echo "  saved: $rel"
  done
else
  log "Skipping backup (--no-backup flag set)"
fi

# Apply patches
log "Patching chapters …"
for f in "${TARGETS[@]}"; do
  echo "  -> $f"
  "$UTMT_CLI" load "$f" --scripts "${MOD_SCRIPTS[0]}" --verbose false --output "$f"
  "$UTMT_CLI" load "$f" --scripts "${MOD_SCRIPTS[1]}" --verbose false --output "$f"
done

log "Done. Launch DELTARUNE and open the Mods menu to configure difficulty."
