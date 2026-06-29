# Custom Difficulty Mod for DELTARUNE
Difficulty options for DELTARUNE. Make the game easy, hard, or nightmare! ( or no-hit! )

<img src="https://github.com/user-attachments/assets/9e9be8b9-0c3c-4b28-9a12-3aa2d328189c" width="480" />

## Download
**[Latest release](https://github.com/Emmehehe/CustomDifficultyModForDeltarune/releases/tag/1.7.0)**

## What you can change
- **Presets** — Select from various difficulty presets
  - Easy `-50% damage, +50% i-frames`
  - Normal `Standard Deltarune experience`
  - Hard `+50% damage, -20% i-frames, -10% enemy cooldowns`
  - Nightmare `+100% increased damage, -35% i-frames, -20% enemy cooldowns`
  - Nightmare-EX `+150% damage, -50% i-frames, -30% enemy cooldowns`
  - Nightmare-Neo `NIGHTMARE-EX with -50% battle rewards, and no passive regen/revive for downed characters`
  - No-Hit `Infinite damage to every party member`

***Battle***
- **Damage Multi** — multiply all incoming damage by this value
  - Attacks that are scripted to leave a character at 1 HP, or other threshold, still do so.
  - For attacks that deal damage as a percentage of the current HP, instead uses exponential logic to determine damage scaling. e.g. An attack that normally does 50% of your HP in vanilla, instead does 70.7% with double damage, and 25% with half damage. The calculation is thus: `dmgratio = vanilladmgratio^(1/dmgmulti)`.
  - For any damage over time effects, either tick faster, apply more damage, or combination of both - where appropriate - proportionate with the multiplier that has been set.

- **Hit.All** — when switched on, hits that target a single character instead hits the entire party
  - Additionally, attacks that adjust their damage for certain characters, no longer do so.
  - For no-hit runs, combine this setting with `Damage Multi=INF`.
- **I-Frames** — multiply the number of i-frames received after damage by this value
- **Enemy Cooldowns** — multiplier for the cooldown between enemy attacks & bullet patterns
  - Heavily scripted attacks/encounters may not change much if at all.
  - A few tweaks have been made to some attacks:
    - Reduce random placement of heart shapers (Head Hathy) at lower cooldowns as the attack can trap you.
    - Cap timer reduction for wave chain (Spade King) as it becomes a solid wall quite quickly.
    - Shorten tails for chain of hell (Spade King's chain bullets) at lower cooldowns as the attack can trap you.
    - Ensure Queen's leg stomp attack waits for an available slot before spawning a leg, instead of flooding the box.
    - Chapter 3's super boss' full-screen attack is hard capped at >25% cooldown to prevent crashing.
    - Winglade's aim attack is hard capped at >50% cooldown to prevent it from getting stuck.
- **Battle Rewards** — multiply post-battle rewards by this value

***Down***
- **Down Deficit** — when a party member is downed, their HP is set to -50% max HP; this option overrides that
- **Downed Regen** — when downed, party members regen 12.5% max HP every turn; this option overrides that
- **Victory Res** — when a battle is won, all downed party members are healed up to 12.5% max HP; this option overrides that
  - OFF: Can also be switched off entirely by reducing past 0%.

***Player***
- **Damage** — multiply outgoing damage by this value
- **Healing** — multiply healing from spells & actions by this value
- **TP Gain** — multiply TP gain from all sources (except item use) by this value
- **Mercy** — multiply mercy build-up by this value
<details> 
  <summary><strong>CHAPTER 3 SPOILERS...</strong></summary>


  ***Battle***
  - **Gameboard Dmg X** — multiplier for the incoming damage in the chapter 3 game boards
    - Only shows up in the menu in chapter 3.
    - INHERIT - Can be set to inherit from the 'Damage Multi' setting by reducing past 0%.
    - Attacks that are scripted to leave a character at 1 HP, or other threshold, still do so.

  - **Gmbrd Enemy CDs** — multiplier for the enemy attack cooldowns in the chapter 3 game boards
    - Only shows up in the menu in chapter 3.
    - INHERIT - Can be set to inherit from the 'Enemy Cooldowns' setting by reducing past 0%.

  ***Player***
  - **Gmbrd Damage** — multiplier for outgoing damage in the chapter 3 game boards
    - Only shows up in the menu in chapter 3.
    - INHERIT - Can be set to inherit from the 'Damage' setting by reducing past 0%.

  - **Gmbrd Healing** — multiplier for healing from collecting candy in the chapter 3 game boards
    - Only shows up in the menu in chapter 3.
    - INHERIT - Can be set to inherit from the 'Healing' setting by reducing past 0%.
</details>

## How to use

1. Open the menu in a dark world.
2. Go to **MODS** and adjust settings.

## Installation via Deltamod/Deltahub
**Full game** — custom-difficulty-drmod-fullgame-{version number}.zip

**Demo** — custom-difficulty-drmod-demo-{version number}.zip

## Installation without a mod-manager

### Windows
**Quick start**
1. Download and unzip the release.
2. Double-click `install-windows.cmd`.

**Installer does**
- Detects your DELTARUNE install (prompts if needed)
- Downloads UndertaleModTool CLI if missing
- Backs up originals to `ModBackups/<timestamp>`
- Patches all chapters; safe to re-run

<details>
  <summary><strong>Command line</strong></summary>

  ```powershell
  .\install-windows.cmd
````

</details>

<details>
  <summary><strong>If Windows blocks it</strong></summary>

* SmartScreen: “More info → Run anyway” or right-click → Properties → **Unblock**
* Permission errors under Program Files/Steam: run as Administrator

</details>

### macOS

**Quick start**

1. Download and unzip the release.
2. Double-click `install-macos.command`.

**Installer does**

* Detects your DELTARUNE install (prompts if needed)
* Downloads UndertaleModTool CLI if missing
* Backs up to `ModBackups/<timestamp>`
* Patches all chapters; safe to re-run

<details>
  <summary><strong>Command line</strong></summary>

```bash
./install-macos.command
```

</details>

<details>
  <summary><strong>If macOS blocks it</strong></summary>

* Right-click → Open, or allow in System Settings → Privacy & Security

</details>

### Linux w\ Proton

**Quick start**

1. Download and unzip the release.
2. Double-click `install-linux-proton.sh`.

**Installer does**

* Detects your DELTARUNE install (prompts if needed)
* Downloads UndertaleModTool CLI if missing
* Backs up to `ModBackups/<timestamp>`
* Patches all chapters; safe to re-run

<details>
  <summary><strong>Command line</strong></summary>

```bash
./install-linux-proton.sh
```

</details>

## Advanced

<details>
  <summary><strong>Windows script flags</strong> (<code>install-windows.ps1</code>)</summary>

* `-Uninstall` — restore from most recent backup
* `-NoBackup` — skip creating backups
* `-GameDir <path>` — set DELTARUNE folder path
* `-UtmtCli <path>` — path to UndertaleModCli.exe

Example:

```powershell
.\install-windows.ps1 -NoBackup -GameDir "D:\Games\DELTARUNE"
```

</details>

<details>
  <summary><strong>macOS script flags</strong> (<code>install-macos.command</code>)</summary>

* `--uninstall` — restore from most recent backup
* `--no-backup` — skip creating backups
* `--app <path>` — path to DELTARUNE.app
* `--utmt <path>` — path to UndertaleModCli

Example:

```bash
./install-macos.command --no-backup --app /Applications/DELTARUNE.app
```

</details>

<details>
  <summary><strong>Linux w\ Proton script flags</strong> (<code>install-linux-proton.sh</code>)</summary>

* `--uninstall` — restore from most recent backup
* `--no-backup` — skip creating backups
* `--game-dir <path>` — set DELTARUNE folder path
* `--utmt <path>` — path to UndertaleModCli

Example:

```bash
./install-linux-proton.sh --no-backup --game-dir /Games/DELTARUNE
```

</details>

<details>
  <summary><strong>Manual install</strong> (UndertaleModTool CLI)</summary>

1. Download [UndertaleModTool CLI](https://github.com/UnderminersTeam/UndertaleModTool/releases) for your platform
2. Extract it next to the game files
3. Apply scripts in this order: `modmenu.csx` then `customdifficulty_ch1to5.csx`

**Windows**

```bat
UndertaleModCli.exe load "chapter1_windows\data.win" --scripts "src\modmenu.csx" --verbose false --output "chapter1_windows\data.win"
UndertaleModCli.exe load "chapter2_windows\data.win" --scripts "src\modmenu.csx" --verbose false --output "chapter2_windows\data.win"
UndertaleModCli.exe load "chapter3_windows\data.win" --scripts "src\modmenu.csx" --verbose false --output "chapter3_windows\data.win"
UndertaleModCli.exe load "chapter4_windows\data.win" --scripts "src\modmenu.csx" --verbose false --output "chapter4_windows\data.win"
UndertaleModCli.exe load "chapter5_windows\data.win" --scripts "src\modmenu.csx" --verbose false --output "chapter5_windows\data.win"

UndertaleModCli.exe load "chapter1_windows\data.win" --scripts "src\customdifficulty_ch1to5.csx" --verbose false --output "chapter1_windows\data.win"
UndertaleModCli.exe load "chapter2_windows\data.win" --scripts "src\customdifficulty_ch1to5.csx" --verbose false --output "chapter2_windows\data.win"
UndertaleModCli.exe load "chapter3_windows\data.win" --scripts "src\customdifficulty_ch1to5.csx" --verbose false --output "chapter3_windows\data.win"
UndertaleModCli.exe load "chapter4_windows\data.win" --scripts "src\customdifficulty_ch1to5.csx" --verbose false --output "chapter4_windows\data.win"
UndertaleModCli.exe load "chapter5_windows\data.win" --scripts "src\customdifficulty_ch1to5.csx" --verbose false --output "chapter5_windows\data.win"
```

**macOS**

```bash
./UndertaleModCli load chapter1_mac/game.ios --scripts src/modmenu.csx --verbose false --output chapter1_mac/game.ios
./UndertaleModCli load chapter2_mac/game.ios --scripts src/modmenu.csx --verbose false --output chapter2_mac/game.ios
./UndertaleModCli load chapter3_mac/game.ios --scripts src/modmenu.csx --verbose false --output chapter3_mac/game.ios
./UndertaleModCli load chapter4_mac/game.ios --scripts src/modmenu.csx --verbose false --output chapter4_mac/game.ios
./UndertaleModCli load chapter5_mac/game.ios --scripts src/modmenu.csx --verbose false --output chapter5_mac/game.ios

./UndertaleModCli load chapter1_mac/game.ios --scripts src/customdifficulty_ch1to5.csx --verbose false --output chapter1_mac/game.ios
./UndertaleModCli load chapter2_mac/game.ios --scripts src/customdifficulty_ch1to5.csx --verbose false --output chapter2_mac/game.ios
./UndertaleModCli load chapter3_mac/game.ios --scripts src/customdifficulty_ch1to5.csx --verbose false --output chapter3_mac/game.ios
./UndertaleModCli load chapter4_mac/game.ios --scripts src/customdifficulty_ch1to5.csx --verbose false --output chapter4_mac/game.ios
./UndertaleModCli load chapter5_mac/game.ios --scripts src/customdifficulty_ch1to5.csx --verbose false --output chapter5_mac/game.ios
```

**Linux w\ Proton**

```bash
./UndertaleModCli load chapter1_windows/data.win --scripts src/modmenu.csx --verbose false --output chapter1_windows/data.win
./UndertaleModCli load chapter2_windows/data.win --scripts src/modmenu.csx --verbose false --output chapter2_windows/data.win
./UndertaleModCli load chapter3_windows/data.win --scripts src/modmenu.csx --verbose false --output chapter3_windows/data.win
./UndertaleModCli load chapter4_windows/data.win --scripts src/modmenu.csx --verbose false --output chapter4_windows/data.win
./UndertaleModCli load chapter5_windows/data.win --scripts src/modmenu.csx --verbose false --output chapter5_windows/data.win

./UndertaleModCli load chapter1_windows/data.win --scripts src/customdifficulty_ch1to5.csx --verbose false --output chapter1_windows/data.win
./UndertaleModCli load chapter2_windows/data.win --scripts src/customdifficulty_ch1to5.csx --verbose false --output chapter2_windows/data.win
./UndertaleModCli load chapter3_windows/data.win --scripts src/customdifficulty_ch1to5.csx --verbose false --output chapter3_windows/data.win
./UndertaleModCli load chapter4_windows/data.win --scripts src/customdifficulty_ch1to5.csx --verbose false --output chapter4_windows/data.win
./UndertaleModCli load chapter5_windows/data.win --scripts src/customdifficulty_ch1to5.csx --verbose false --output chapter5_windows/data.win
```

**Notes**

* Back up your files first
* Apply `modmenu` before `customdifficulty`
* On macOS, chapter folders live in `DELTARUNE.app/Contents/Resources/`

</details>

## Compatibility

**Saves:** Compatible with vanilla saves. The mod reads and writes to a new `.ini` file; no change to vanilla save data.

**Other mods:** Not guaranteed. This mod makes specific line edits to the dark world menu, various damage scripts, enemy attack code, and battle controller code; so those areas will be the source of any potential conflicts. Mods that avoid those areas are more likely to work.
