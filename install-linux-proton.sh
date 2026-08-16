#!/usr/bin/env bash
set -euo pipefail

# === Simple installer for CustomDifficultyModForDeltarune (Linux w\ Proton) ===
# Usage:
#   Double‑click in your file explorer (or: chmod +x install-linux.sh && ./install-linux.sh)
#   Optional flags: --uninstall         Restore from most recent backup
#                   --no-backup         Skip creating backups before patching
#                   --game-dir <path>   Path to DELTARUNE folder (skips auto‑detect)
#                   --utmt <path>       Path to UndertaleModCli (skips download/search)

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SCRIPTS_DIR="$SCRIPT_DIR/src"
MOD_SCRIPTS=("$SCRIPTS_DIR/modmenu.csx" "$SCRIPTS_DIR/customdifficulty_ch1to5.csx")
CACHE_DIR="$HOME/.cache/diffmod-utmt"
mkdir -p "$CACHE_DIR"

UNINSTALL=0
NO_BACKUP=0
GAME_DIR=""
UTMT_CLI=""

# --- parse args ---
while [[ $# -gt 0 ]]; do
  case "$1" in
    --uninstall) UNINSTALL=1; shift;;
    --no-backup) NO_BACKUP=1; shift;;
    --game-dir) GAME_DIR="${2:-}"; shift 2;;
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

  # 4) Download specific release (v0.9.1.2) asset that looks like Linux/Ubuntu CLI
  log "Downloading UndertaleModTool CLI (v0.9.1.2) …"
  API_URL="https://api.github.com/repos/UnderminersTeam/UndertaleModTool/releases/tags/0.9.1.2"
  JSON="$(curl -fsSL "$API_URL")" || err "Failed to contact GitHub API"
  # Try to pick a Linux/Ubuntu CLI asset (zip or tar.gz). We look for 'CLI' and 'Ubuntu' in the URL.
  DL_URL=$(printf "%s" "$JSON" | grep -oE '"browser_download_url"\s*:\s*"[^"]+"' | cut -d '"' -f 4 | \
           grep -iE 'cli' | grep -iE 'linux|ubuntu' | head -n1)
  [[ -n "$DL_URL" ]] || err "Could not find a Linux/Ubuntu CLI asset in v0.9.1.2 release. Supply --utmt <path> instead."

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
  
  # Test that it works
  if ! "$CACHE_DIR/UndertaleModCli" --version >/dev/null 2>&1; then
    err "Failed to get working UndertaleModCli"
  fi
  
  # Clean up temp dir
  rm -rf "$TMPD"
  
  echo "$CACHE_DIR/UndertaleModCli"
}

# --- helper: pick or detect game dir ---
detect_game_dir_if_needed() {
  if [[ -n "$GAME_DIR" ]]; then return; fi

  # Try common Steam location first
  DEFAULT_GAME_DIR="$HOME/.steam/steam/SteamApps/common/DELTARUNE"
  if [[ -d "$DEFAULT_GAME_DIR" ]]; then GAME_DIR="$DEFAULT_GAME_DIR"; fi

  # If no game found, try to find demo
  if [[ -z "$GAME_DIR" ]]; then
    DEFAULT_DEMO_GAME_DIR="$HOME/.steam/steam/SteamApps/common/DELTARUNEdemo"
    if [[ -d "$DEFAULT_DEMO_GAME_DIR" ]]; then GAME_DIR="$DEFAULT_DEMO_GAME_DIR"; fi
  fi

  # Ask user to confirm or pick
  if [[ -n "$GAME_DIR" ]]; then
    echo "Detected: $GAME_DIR"
    read -r -p "Use this path? [Y/n] " resp
    if [[ "${resp:-Y}" =~ ^(n|N)$ ]]; then GAME_DIR=""; fi
  fi

  if [[ -z "$GAME_DIR" ]]; then
    read -r -p "Enter full path to DELTARUNE folder: " GAME_DIR
  fi

  [[ -d "$GAME_DIR" ]] || err "Path not found: $GAME_DIR"
}

# --- helper: list chapter files ---
find_chapter_files() {
  local RES="$1"
  local files=()
  for ch in 1 2 3 4 5; do
    local f="$RES/chapter${ch}_windows/data.win"
    [[ -f "$f" ]] && files+=("$f")
  done
  # If no chapter files found, assume this is the demo
  if (( ${#files[@]} == 0 )); then
    log "No chapter files found, attempting install for demo. "
    local f="$RES/data.win"
    [[ -f "$f" ]] && files+=("$f")
  fi
  printf '%s\n' "${files[@]:-}"
}

# --- main ---
UTMT_CLI=$(find_utmt_cli)
log "Using UndertaleModCli: $UTMT_CLI"

detect_game_dir_if_needed
RESOURCES="$GAME_DIR"
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
read -r -n 1 -s -p $'Press any key to exit\n'
