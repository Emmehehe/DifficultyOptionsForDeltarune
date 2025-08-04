# Difficulty Options For Deltarune
Mod that adds difficulty options to DELTARUNE.
 - I'm still playing through the game w/ the mod atm, so its very untested, but what I've played so far seems to work fine (some way through chapter 1).

## Download
Just click the green `<> Code` button up there and download zip.
 - I'll add releases when I can be bothered.

## Installation
1. (optional) Backup your `DELTARUNE\` folder.
2. Copy `diffops_installfiles\` (for Windows & MacOS) & `diffops_installer_windows.bat` (for Windows only) into your `DELTARUNE\` folder.
4. Run `diffops_installer_windows.bat` (if on Windows).
   - If you're on MacOS, you could try the script from [this pull request](https://github.com/Emmehehe/DifficultyOptionsForDeltarune/pull/2) ([branch for DL](https://github.com/Emmehehe/DifficultyOptionsForDeltarune/tree/add-installer-for-macos)). Let me know if it works.
   - Or you can manually apply the scripts to each chapter file using [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool/releases). Scripts > Run other script...
5. Done! You can remove the installer & install files now if you want.

## How set options??
1. Load up any save file in-game. This prompts a `difficulty_[fileslot].ini` to spawn in Deltarune's save place.
2. Win + R. Enter `%localappdata%/deltarune`.
3. Make changes to the `difficulty_[fileslot].ini` that matches your save file.
4. Reload the save file.

## What options do?
#### DAMAGE_MULTIPLIER
Multiply all incoming damage by this value. 1 = normal damage, 0.5 = half damage, 2 = double damage, etc.
- Default: 1
- 0 ought to mean you're invincible, but I've not tested it.
- Attacks that are scripted to leave a character at 1 HP, or other threshold, still do so.
- For wierd attacks that deal damage as a percentage of the current HP, instead uses exponential logic to determine damage scaling. e.g. An attack that normally does 1/2 your HP in vanilla, instead does 70.7% with double damage, and 1/4 with half damage. The calculation is thus: `dmgratio = vanilladmgratio^(1/dmgmulti)`.
- For any damage over time effects, either tick faster, or apply more damage, or combination of both - where appropriate - proportionate with the multiplier that has been set.

#### DOWN_PENALTY
When a character is 'downed', their health is put as far into the negatives as half their maximum HP. This option lets you override that. 0.5 = normal down penalty (50%), 1 = negative 100% max HP, 0.25 = negative 25% max HP, etc.
- Default: 0.5
- I've not tested values that are 0 or less. This might cause wierd behaviour, idk.

#### VICTORY_RES
When a battle is won, the game automatically resurrects any downed characters with 1/8th HP. This option lets you override that. 0.125 = normal victory res, 0.25 = 1/4th res, 1 = full res, etc.
- Default: 0.125
- 0 ought to mean the downed character's HP is brought up to 0 (from negative), but I've not tested it.
- Negative (<0) values disable the victory res entirely, in case you want to play without it.

<details> 
  <summary><strong>CHAPTER 3 SPOILERS...</strong></summary>

  > #### BATTLEBOARD_DAMAGE_MULTIPLIER
  > Multiplier for the damage in the chapter 3 game boards. 1 = normal damage, 0.5 = half damage, 2 = double damage, etc.
  > - Warning: This entire option hasn't been tested yet!
  > - Only shows up in the config file after loading a save file in chapter 3.
  > - Default: -1
  > - Negative (<0) value = Just use the same multiplier as DAMAGE_MULTIPLIER.
  > - Attacks that are scripted to leave a character at 1 HP, or other threshold, still do so.
</details>

## Compatibility
Will my vanilla saves work with this mod and vice-versa?
> **Yes.**
> This just reads and writes to a new .ini file. So no change to vanilla save data.

Is this compatible with X mod?
> **I can't garauntee anything, but most likely.**
> The mod install script makes changes to specific lines of vanilla damage code, so anything that doesn't mess with those should be compatible.

## Feachure considerations
 - [MacOS install script](https://github.com/Emmehehe/DifficultyOptionsForDeltarune/pull/2). This needs testing.
 - In-game config menu (the config menu is a pile of if-else so not keen).
 - Seperate damage modifier for bosses vs. normal guys? Would have to track down every boss script in order to apply the boss modifier :/
 - Battle speed/frame-pace (I've not even had a think about how this would be acheived yet).
   - NuclearThroneTogether changes the whole game to run off deltaTime w/ configurable framerate + gamespeed option. Might be worth looking into how that was done, one day...
   - I imagine you can cheat engine the game speed anyway (though would be much nicer if it was only for battle box segments)
