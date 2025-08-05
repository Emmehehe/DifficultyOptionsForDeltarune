# Difficulty Options For Deltarune
Mod that adds difficulty options to DELTARUNE.
 - I'm still playing through the game w/ the mod atm, so its very untested, but what I've played so far seems to work fine (some way through chapter 1).

## Download
Just click the green `<> Code` button up there and download zip.
 - I'll add releases when I can be bothered.

## Installation
1. (optional) Backup your `DELTARUNE\` folder.
2. Copy `diffops_installfiles\` (for Windows & MacOS) & `diffops_installer_windows.bat` (for Windows only) into your `DELTARUNE\` folder.
3. Run `diffops_installer_windows.bat` (if on Windows).
   - If you're on MacOS, you could try the script from [this pull request](https://github.com/Emmehehe/DifficultyOptionsForDeltarune/pull/2) ([branch for DL](https://github.com/Emmehehe/DifficultyOptionsForDeltarune/tree/add-installer-for-macos)). Let me know if it works.
   - Or you can manually apply the scripts to each chapter file using [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool/releases). Scripts > Run other script...
4. Done! You can remove the installer & install files now if you want.

## How set options??
1. Open menu in a dark world.
2. Difficulty options are found in the 'MODS' section.

## What options do?
#### Damage Multi
Multiply all incoming damage by this value. 100% = normal damage, 50% = half damage, 200% = double damage, etc.
- Default: 100%
- **Warning:** [0% could cause crashes atm](https://github.com/Emmehehe/DifficultyOptionsForDeltarune/issues/4).
- Attacks that are scripted to leave a character at 1 HP, or other threshold, still do so.
- For wierd attacks that deal damage as a percentage of the current HP, instead uses exponential logic to determine damage scaling. e.g. An attack that normally does 1/2 your HP in vanilla, instead does 70.7% with double damage, and 1/4 with half damage. The calculation is thus: `dmgratio = vanilladmgratio^(1/dmgmulti)`.
- For any damage over time effects, either tick faster, or apply more damage, or combination of both - where appropriate - proportionate with the multiplier that has been set.

#### Down Deficit
When a character is 'downed', their health is put as far into the negatives as half their maximum HP. This option lets you override that. 
- Default: 50%

#### Victory Res
When a battle is won, the game automatically resurrects any downed characters and heals them to 1/8th HP. This option lets you override that. 12.5% = normal victory res, 25% = 1/4th res, 100% = full res, etc.
- Default: 12.5%
- 0% ought to mean the downed character's HP is brought up to 0 (from negative), but I've not tested it.
- OFF: Can also be switched off entirely by reducing past 0%.

<details> 
  <summary><strong>CHAPTER 3 SPOILERS...</strong></summary>

  > #### Gameboard Dmg Multi
  > Multiplier for the damage in the chapter 3 game boards. 100% = normal damage, 50% = half damage, 200% = double damage, etc.
  > - Warning: This entire option hasn't been tested yet!
  > - Only shows up in the menu in chapter 3.
  > - Default: INHERIT
  > - INHERIT - Can also be set to inherit from 'Damage Multi' by reducing past 0%.
  > - Attacks that are scripted to leave a character at 1 HP, or other threshold, still do so.
</details>

## Compatibility
Will my vanilla saves work with this mod and vice-versa?
> **Yes.**
> This just reads and writes to a new .ini file. So no change to vanilla save data.

Is this compatible with X mod?
> **I can't garauntee anything, but most likely.**
> The mod install script makes changes to specific lines of vanilla damage code, so anything that doesn't mess with those should be compatible.
> The mod menu script makes changes to the dark world menu logic and draw code, so disable the mod menu script if you're getting compatibility issues with the dark world menu.

## Feachure considerations
 - [MacOS install script](https://github.com/Emmehehe/DifficultyOptionsForDeltarune/pull/2). This needs testing.
 - Seperate damage modifier for bosses vs. normal guys? Would have to track down every boss script in order to apply the boss modifier :/
 - Battle speed/frame-pace (I've not even had a think about how this would be acheived yet).
   - NuclearThroneTogether changes the whole game to run off deltaTime w/ configurable framerate + gamespeed option. Might be worth looking into how that was done, one day...
   - I imagine you can cheat engine the game speed anyway (though would be much nicer if it was only for battle box segments)
