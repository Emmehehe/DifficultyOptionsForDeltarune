# Mod Menu Guide for Mod Devs
Mod Menu is a mod framework that can be used to quickly add config menus for other mods. Does nothing on its own.

For an example of this mod in action see the [Custom Difficulty mod](https://gamebanana.com/mods/613308).
For reference, you can see the exact code that this mod uses to configure its menu [here](https://github.com/Emmehehe/CustomDifficultyModForDeltarune/blob/1.3.0/src/customdifficulty_ch1to4.csx#L159-L250).
There's also an example of setting up the variables and saving/loading them if you scroll up.

## How to add the mod tool to the game

1. Open the data.win for any chapter (or demo) in [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool/releases)
2. Scripts > Run other script...
3. Open the [modmenu.csx file](https://gamebanana.com/tools/20839)
4. Done! 'MODS' button should appear in the dark world menu in-game. Now you'll need to follow the instructions below to add your custom menu.

## Steps to create a menu using the framework

In `gml_Object_obj_darkcontroller_Create_0` & `gml_Object_obj_darkcontroller_ch1_Create_0`(for demo only):
1. Make sure the `modmenu_data` var is set up.
```
if (!variable_instance_exists(global, "modmenu_data"))
  global.modmenu_data = array_create(0);
```
2. Start defining your mod's menu and give it a title. 
```
var menudata = ds_map_create();
ds_map_add(menudata, "title_en", "<My Cool Mod for Deltarune>");
```
3. Start defining the form controls for your menu.
```
var formdata = array_create(0);
```
4. Add as many rows to the form as you need. Each row could be a slider/toggle that controls a global variable, a button that triggers a global function, or a header.

<i>Slider/Toggle:</i>
```
var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "<My Cool Option>");
ds_map_add(rowdata, "value_range_en", "<value range string (see #Value Ranges below)>");
ds_map_add(rowdata, "value_name", "<somevarname>");
array_push(formdata, rowdata);
```
<i>Button:</i>
  ```
var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "<My Cool Button>");
ds_map_add(rowdata, "func_name", "<somefuncname>");
array_push(formdata, rowdata);
  ```
<i>Header:</i>
  ```
var rowdata = ds_map_create();
ds_map_add(rowdata, "title_en", "<My Cool Header>");
array_push(formdata, rowdata);
  ```
5. Finish defining the form controls, and menu.
```
ds_map_add(menudata, "form", formdata);
array_push(global.modmenu_data, menudata);
```

## Config Explained

- title_en — English title for the mod's menu.
- left_margin_en (optional, default: 40) — Adjusts the menu's left side margin. Useful if you're having trouble fitting text in the box.
- left_value_pos_en (optional, default: 300) — Adjusts the left side position of the values in the menu. Useful if you're having trouble fitting text in the box.
- on_close (optional) — Name of a global function to call when the mod's menu is closed.
- form — Array containing rows for controls, buttons, and headers.
  - title_en — English title for the control/button/header.
  - value_range_en (optional) — English value range string for the control.
    - Simple examples: `"0~100%"`, `"OFF=false;ON=true"`, `"OFF=-1;0~100%"`.
    - See [Value Ranges](#Value-Ranges) for a more detailed explaination.
  - value_name (required if value_range_en is set) — Name of the global variable this control should adjust.
    - e.g. Set to `"coolmod_funvalue"` for variable `global.coolmod_funvalue`.
  - on_change (optional) — Name of a global function to call for every step that the value is changed.
  - force_scroll (optional) — For value ranges that are entirely labels, force scroll behaviour rather than normal toggle behaviour.
  - func_name (optional) — Name of the global function this control/button should trigger.
    - e.g. Set to `"coolmod_dofunthings"` for function `global.coolmod_dofunthings`.
    - This can also be specified for a control, will trigger immediately after the user confirms the control if so.

## Value Ranges

Value range strings allow you to define how a control behaves when the user interacts with it.

Types of value range:
 - Label: ``<label name>=<decimal|percentage%|string`|true|false>`` — Sets the variable to the given decimal, percentage, string, or bool value, the user sees the label name. 
 - MinMax: `<min>~<max>` — Sets the variable between a range of integer values. Inclusive.
 - MinMax(%): `<min>~<max>%` — Sets the variable between a range of decimal values, the user sees a percentage. Inclusive.

Multiple ranges can be combined using `;`.
- If Labels and MinMaxes are combined then all ranges MUST be decimal or percentage and MUST be defined in order. e.g. `"OFF=-1;0~100%;[999]=999"` is valid, but `"OFF=false;0~100%;[999]=-1"` is invalid.

**Example range strings:**
 - `"0~10"` — User can slide the value between 0 to 10, the value is set between 0 and 10.
 - `"0~100%"` — User can slide the value between 0% to 100%, the value is set between 0 and 1.
 - `"0~200%"` — User can slide the value between 0% to 200%, the value is set between 0 and 2.
 - `"-100~100%"` — User can slide the value between -100% to 100%, the value is set between -1 and 1.
 - `"OFF=false;ON=true"` — User can toggle between 'OFF' and 'ON', the value is set to either false (off) or true (on).
 - `"RED=0x0000FF;GREEN=0x00FF00;BLUE=0xFF0000"` — User can toggle through 'RED', 'GREEN', and 'BLUE', the value is set appropriately. GML uses the color format BGR.
 - `"SMALL=50%;MEDIUM=100%;LARGE=200%"` — User can toggle through 'SMALL'(0.5), 'MEDIUM'(1), and 'LARGE'(2), the percentage value is set appropriately.
 - ``"EASY=Easy`;NORMAL=Normal`;HARD=Hard`"`` — User can toggle through 'EASY'(Easy), 'NORMAL'(Normal), and 'HARD'(Hard), the string value is set appropriately.
 - `"OFF=-1;0~1000%"` — User can slide the value between 0% to 1000%, the value is set between 0 and 10. Additionally, if the user slides the value below 0%, they can set the option to 'OFF', aka -1.

## Localisation
The mod menu supports localisation by reading the `global.lang` variable that comes with Deltarune, and looking up attributes that are postfixed with that lang string.

For English the mod menu will read all attributes with the '_en' postfix, for Japanese it will look for the '_ja' postfix. If no attribute for the lang postfix can be found the mod menu will default to English.

It's recommended to use the [ISO 639-1](https://en.wikipedia.org/wiki/List_of_ISO_639_language_codes) standard for lang strings if you are adding additional languages.
