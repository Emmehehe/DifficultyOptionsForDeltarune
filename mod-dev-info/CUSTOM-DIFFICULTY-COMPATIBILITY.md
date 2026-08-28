# Custom Difficulty Compatibility Guide for Mod Devs
This guide is for mod devs who are adding custom encounters to the game (or porting content between chapters), and want to include support for custom difficulty.

The Custom Difficulty mod adds a function `global.diff_apply` for devs to integrate options into their mod.

Say you have a piece of code to do damage to a character:
```
var damage = <some calculation or constant>;
global.hp[global.char[i]] -= damage;
```
To apply the `Damage Multi` option from Custom Difficulty, your code would look like this:
```
var damage = <some calculation or constant>;
damage = !is_undefined(global.diff_apply) ? global.diff_apply("DIFFOP_DAMAGE", damage) : damage;
global.hp[global.char[i]] -= damage;
```
Explaination:
 - Check that the `diff_apply` func exists
 - If exists - apply the difficulty option for damage multi (`"DIFFOP_DAMAGE"`) to the damage
 - Else - make no change to the damage

This way your content will be compatible with Custom Difficulty, but will still work normally if Custom Difficulty is not installed.

## Difficulty Options

There are various difficulty options you can apply via the `diff_apply` function, detailed below.
- **Damage Multi** — `global.diff_apply("DIFFOP_DAMAGE", damage)`
  - Apply damage multiplier.
- **Gameboard Dmg X** — `global.diff_apply("DIFFOP_DAMAGE_GB", damage)`
  - Apply gameboard damage multiplier.
- **Hit.All** — `global.diff_apply("DIFFOP_HITALL", isHitAll)`
  - Union of 'Hit.All' & input (= 'Hit.All' || isHitAll)
- **I-Frames** — `global.diff_apply("DIFFOP_IFRAMES", iFrames)`
  - Apply i-frames multiplier.
- **Enemy Cooldowns** — `global.diff_apply("DIFFOP_ENEMYCD", cooldown)`
  - Apply enemy cooldown multiplier.
- **Gmbrd Enemy CDs** — `global.diff_apply("DIFFOP_ENEMYCD_GB", cooldown)`
  - Apply gameboard enemy cooldown multiplier.
- **Extra Enemies** — `global.diff_apply("DIFFOP_EXTRAENEMIES")`
  - Returns how many extra enemies the user has configured, between 0 to 2.
- **Battle Rewards** — `global.diff_apply("DIFFOP_REWARDS", reward)`
  - Apply battle rewards multiplier.
- **Down Deficit** — `global.diff_apply("DIFFOP_DOWNDEF", maxhp)`
  - Get the HP to set the character to, based on their max HP.
  - This is 1/2 in vanilla Deltarune.
- **Downed Regen** — `global.diff_apply("DIFFOP_DOWNREGEN", maxhp)`
  - Get the HP to restore each turn, based on their max HP.
  - This is 1/8 in vanilla Deltarune.
- **Victory Res** — `global.diff_apply("DIFFOP_VICRES", maxhp, hp)`
  - Get the HP to set the character to, based on their max HP & current HP.
  - This is 1/8 in vanilla Deltarune.
- **Player Damage** — `global.diff_apply("DIFFOP_PLRDMG", plrdmg)`
  - Apply player damage multiplier.
- **Gameboard Player Damage** — `global.diff_apply("DIFFOP_PLRDMG_GB", plrdmg)`
  - Apply gameboard player damage multiplier.
- **Player Heal** — `global.diff_apply("DIFFOP_PLRHEAL", plrheal)`
  - Apply player healing multiplier.
- **Gameboard Player Heal** — `global.diff_apply("DIFFOP_PLRHEAL_GB", plrheal)`
  - Apply gameboard player healing multiplier.
- **Heal Items** — `global.diff_apply("DIFFOP_HEALITEMS", healamount)`
  - Apply item healing multiplier.
- **TP Gain** — `global.diff_apply("DIFFOP_TPGAIN", tpadd)`
  - Apply TP gain multiplier.
- **TP Items** — `global.diff_apply("DIFFOP_TPITEMS", tpadd)`
  - Apply TP items multiplier.
- **Mercy** — `global.diff_apply("DIFFOP_MERCY", mercyadd)`
  - Apply mercy multiplier.
- **SAVE Point Heal** — `global.diff_apply("DIFFOP_SAVEHEAL", isHeal)`
  - Union of 'SAVE Point Heal' & input (= 'SAVE Point Heal' || isHeal)
- **Enemy Scaling** — `global.diff_apply("DIFFOP_ENEMYSCALING", isScaling)`
  - Union of 'Enemy Scaling' & input (= 'Enemy Scaling' || isScaling)
- **Health (regular)** — `global.diff_apply("DIFFOP_HEALTHREGULAR", maxhp)`
  - Apply health multiplier for Regular enemies.
- **Health (miniboss)** — `global.diff_apply("DIFFOP_HEALTHMINIBOSS", maxhp)`
  - Apply health multiplier for Mini-Boss enemies.
- **Health (boss)** — `global.diff_apply("DIFFOP_HEALTHBOSS", maxhp)`
  - Apply health multiplier for Boss enemies.
- **Gameboard Scaling** — `global.diff_apply("DIFFOP_ENEMYSCALING_GB", isScaling)`
  - Union of 'Gameboard Scaling' & input (= 'Gameboard Scaling' || isScaling)
- **GB HP (regular)** — `global.diff_apply("DIFFOP_HEALTHREGULAR_GB", maxhp)`
  - Apply health multiplier for Regular gameboard enemies.
- **GB HP (miniboss)** — `global.diff_apply("DIFFOP_HEALTHMINIBOSS_GB", maxhp)`
  - Apply health multiplier for Mini-Boss gameboard enemies.
- **GB HP (boss)** — `global.diff_apply("DIFFOP_HEALTHBOSS_GB", maxhp)`
  - Apply health multiplier for Boss gameboard enemies.
