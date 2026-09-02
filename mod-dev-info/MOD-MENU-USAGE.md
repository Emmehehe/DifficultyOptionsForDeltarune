# Mod Menu Usage Guide
Mod Menu is a mod framework that can be used to quickly add settings menus for your mods. Optionally, the tool can also save your settings between sessions.

For an example of this mod in action see the [Custom Difficulty mod](https://gamebanana.com/mods/613308).
For reference, you can see the exact code that this mod uses to configure its menu [here](https://github.com/Emmehehe/CustomDifficultyModForDeltarune/blob/190-release/src/customdifficulty_ch1to5.csx#L587-L816) (although this is a very complex example).

#### Migrating From 1.X to v2:
- Move your menu config from `obj_darkcontroller(_ch1)_create_0` to the bottom of `function scr_gamestart(_ch1)()`
- Refactor your menu config from the old 'ds_map_' format, to the new struct/json-style format
- [Migration examples](https://github.com/Emmehehe/CustomDifficultyModForDeltarune/pull/119/changes)

## Adding the mod tool to your game

1. Open the data.win for any chapter (or demo) in [UndertaleModTool](https://github.com/UnderminersTeam/UndertaleModTool/releases)
2. Scripts > Run other script...
3. Open the [modmenu.csx file](https://gamebanana.com/tools/20839)
4. Done! 'MODS' button should appear in the dark world menu in-game. Now you'll need to follow the instructions below to add your custom menu.

## Creating your menu

Either: run the script `add_modmenu.csx`.

Or; add this code to the end of the `scr_gamestart(_ch1)` function, found at gml_GlobalScript_scr_gamestart(_ch1):
```js
if (variable_instance_exists(global, "modmenu")) {
  global.menu_my_mods_menu = global.modmenu.create({
    title: "My Mod's Menu",
    ini_name: "my_mods_menu",
    form: [
      {
        type: "Toggle",
        title: "Example Toggle",
        data_ref: { var_name: "example_toggle", default_value: false },
        value_range: "OFF=false;ON=true"
      },{
        type: "Slider",
        title: "Example Slider",
        data_ref: { var_name: "example_slider", default_value: -1 },
        value_range: "OFF=-1;0~1000%;INF=2147483647"
      },{
        type: "UserInput",
        title: "Example UserInput",
        data_ref: {{ var_name: "example_userinput", default_value: "" }},
        max_length: 12, // optional (default=12)
        cutoff_length: 12 // optional (default=12)
      },{
        type: "Header",
        title: "Example Header"
      },{
        type: "Button",
        title: "Example Button",
        trigger_func: function () {}
      },{
        type: "Reset"
      }
    ]
  });
}
```
A menu will now appear in-game, titled "My Mod's Menu"! (you can change the name)

The menu's `form` contains a basic example of every type of menu-item available. You can have as many or as few menu-items as you want.

- Toggle — When clicked; cycles through a range of values ([`value_range`](#Value-Ranges)), updates a variable indicated by [`data_ref`](#Data-Refs) (global scoped by default).
- Slider — When clicked, and then left/right pressed; slides through a range of values ([`value_range`](#Value-Ranges)), updates a variable indicated by [`data_ref`](#Data-Refs) (global scoped by default).
- UserInput — When clicked; opens the namer UI for the user to input custom text (A-Z) up to a length of `max_length`, updates a variable indicated by [`data_ref`](#Data-Refs) (global scoped by default).
- Header — No behaviour, just used to divide your menu into sections.
- Button — When clicked; run a function of your choice.
- Reset — When clicked; resets all menu data to the defaults.

There are also a multitude of optional properties that you can add to your [config](#All-Config-Options) to further customize your menu.

## Some common examples of how you can customize your menu:

**Save settings between play sessions:**
```js
global.modmenu.create({
  title: "My Mod's Menu",
  ini_name: "my-mods-menu",
  save_type: "Single", // other options: PerSlot, PerFile, Never
  form: [
    // ...
  ]
});
```

**Make adjustments to game state based on settings (apply your settings):**
```js
global.modmenu.create({
  title: "My Mod's Menu",
  apply: { type: "OnClose", func: global.cool_function_that_runs_on_close_of_menu }, // other options: OnChange
  form: [
  // ...
```

**The above can be combined, the apply function will also run when the settings are loaded from file:**
```js
global.modmenu.create({
  title: "My Mod's Menu",
  apply: { type: "OnClose", func: global.cool_function_that_runs_on_close_of_menu_and_on_load_from_file }, // other options: OnChange
  ini_name: "my-mods-menu",
  save_type: "Single", // other options: PerSlot, PerFile, Never
  form: [
  // ...
```

**Disabling save for a menu-item:**
```js
{
  type: "Toggle",
  title: "Example Toggle",
  data_ref: { var_name: "example_toggle", default_value: false },
  value_range: "OFF=false;ON=true",
  no_save: true // won't be saved to file!
}
```

**Adding extra save file data without affecting the menu:**
```js
global.modmenu.create({
  title: "My Mod's Menu",
  ini_name: "my-mods-menu",
  save_type: "Single", // other options: PerSlot, PerFile, Never
  form: [
    // ...
  ],
  additional_save_data_refs: [
    { var_name: "example_save_data" /*(global scoped by default)*/, default_value: "Normal Mode" } // won't show up in the menu!
  ]
});
```

**Revert slider value on cancel [X]/(B):**
```js
{
  type: "Slider",
  title: "Example Slider",
  data_ref: { var_name: "example_slider", default_value: -1 },
  value_range: "OFF=-1;0~1000%;INF=2147483647",
  revert_on_cancel: true // e.g. if slider was set to OFF, then the user slides it to 20%, then cancels (presses [X]/(B)); the value will be set back to OFF
}
```

**Adjust the positioning of menu-items:**
```js
global.modmenu.create({
  title: "My Mod's Menu",
  style: {
    dark: {
      left_margin: 40, // set left edge of menu-item title column to 40 pixels (default 0)
      left_value_pos: 300 // set left edge of menu-item value column to 300 pixels (default 240)
    }
  },
  form: [
  // ...
```

**[Intermediate] Disable menu-items when a toggle is set to OFF:**
```js
{
  type: "Toggle",
  title: "Mod Toggle",
  data_ref: { var_name: "my_mod_toggle", default_value: false },
  value_range: "OFF=false;ON=true"
},{
  type: "Slider",
  title: "Example Slider",
  data_ref: { var_name: "example_slider", default_value: -1 },
  value_range: "OFF=-1;0~1000%;INF=2147483647",
  disabled: function () { return !global.my_mod_toggle; } // reads value of my_mod_toggle and sets disabled=true if my_mod_toggle=false
},{
  type: "Header",
  title: "Example Header",
  disabled: function () { return !global.my_mod_toggle; } // reads value of my_mod_toggle and sets disabled=true if my_mod_toggle=false
},{
  type: "Button",
  title: "Example Button",
  trigger_func: function () {},
  disabled: function () { return !global.my_mod_toggle; } // reads value of my_mod_toggle and sets disabled=true if my_mod_toggle=false
}
```

**[Intermediate] Localize to Japanese or to languages added by translation mods (see [Localisation](#Localisation)):**
```js
global.modmenu.create({
  title: [{lang: "en", val: "Hello"}, {lang: "fr", val: "Bonjour"}], // if language not found (e.g. "ja":japanese), uses first entry ("Hello" in this case)
  style: {
    dark: {
      left_margin: [{lang: "en", val: 0}, {lang: "fr", val: 40}], // if language not found (e.g. "ja":japanese), uses first entry (0 in this case)
      left_value_pos: [{lang: "en", val: 240}, {lang: "fr", val: 300}] // etc...
    }
  },
  form: [
    {
      type: "Toggle",
      title: [{lang: "en", val: "Baguette"}, {lang: "fr", val: "Baguette"}],
      data_ref: { var_name: "my_mod_toggle", default_value: false },
      value_range: [{lang: "en", val: "OFF=false;ON=true"}, {lang: "fr", val: "Non=false;Oui=true"}]
    }
// ...
```

**[Advanced] Add function callbacks (listeners) for various events:**
```js
global.modmenu.create({
  title: "My Mod's Menu",
  open_func: global.runs_when_user_opens_this_menu,
  close_func: global.runs_when_user_closes_this_menu,
  form: [
    {
      type: "Toggle",
      title: "Example Toggle",
      data_ref: { var_name: "example_toggle", default_value: false },
      value_range: "OFF=false;ON=true",
      trigger_func: global.runs_when_user_clicks_on_this_menu_item,
      change_func: global.runs_when_value_is_changed_by_the_user // not much different to trigger_func here, but does happen after value change whereas trigger runs before change
    },{
      type: "Slider",
      title: "Example Slider",
      data_ref: { var_name: "example_slider", default_value: -1 },
      value_range: "OFF=-1;0~1000%;INF=2147483647",
      trigger_func: global.runs_when_user_clicks_into_this_menu_item,
      change_func: global.runs_when_value_is_changed_by_the_user,
      cancel_func: global.runs_when_user_cancels_out_of_this_slider, // [X]/(B)
      accept_func: global.runs_when_user_accepts_this_slider // [Z]/(A)
    },{
      type: "Button",
      title: "Example Button",
      trigger_func: global.runs_when_user_clicks_on_this_menu_item
    }
  ]
});
```

**[Advanced] Get reference to a menu-item, so that you can add any dynamic behaviour that isn't already covered by the config:**
```js
{
  type: "Toggle",
  title: "Example Toggle",
  data_ref: { var_name: "example_toggle", default_value: false },
  value_range: "OFF=false;ON=true",
  ref: {var_name: "my_toggle_ref"}
},{
  type: "Slider",
  title: "Example Slider",
  data_ref: { var_name: "example_slider", default_value: -1 },
  value_range: "OFF=-1;0~1000%;INF=2147483647",
  ref: {var_name: "my_ref_arr[0]"}
},{
  type: "Button",
  title: "Example Button",
  ref: {var_name: "my_ref_arr[1]"}
}
// ...
if (something_happens)
  global.my_toggle_ref.title = "Something happened!";

for (var i = 0; i < array_length(my_ref_arr); i++) {
  var menu_item = my_ref_arr[i];
  // do stuff with menu-item...
}
// ...
```

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

## Data Refs

These tell your menu what variables to get/set/save/load when it is interacted with (or when the game saves/loads).

```js
{
  handle: handle, // optional - instance id (or global scope) for the variable (default=global) NOTE: If using instance id instead of global - unlikely that the menu will be able to load data from ini, as the instance may not have been created yet
  var_name: string, // name of the variable e.g. global.fun_time -> var_name: "fun_time" - this can also be an array entry e.g. global.some_arr[0] -> var_name: "some_arr[0]"
  default_value: any, // default value to use if the variable doesn't exist, or is not found in save data (if using save feature); this also helps ModMenu understand what data type to use when reading/writing to the save file
  ini_key: string  // optional - the ini key to use when saving/loading this data (defaults to the var_name)
}
```

## Localisation
The mod menu supports localisation by reading the `global.lang` variable that comes with Deltarune (en/ja in vanilla), and looking up config data that matches the lang string.

It also can detect these community language patches:
- [DeltaESP](https://deltaesp.site/)'s Spanish patch (es)
- [Korean patch](https://www.deltarunekr.kro.kr/) (ko) - can't detect the patch in Chapter 2

To add localisation, replace a field's value with an array of this format `title: [{lang: "en", val: "Hello"}, {lang: "fr", val: "Bonjour"}]`. The first entry in the array will be the default if the language can't be found.

These properties all support localisation:
- title
- style.dark.left_margin
- style.dark.left_value_pos
- form[].title
- form[].value_range
- form[].max_length
- form[].cutoff_length

It's recommended to use the [ISO 639-1](https://en.wikipedia.org/wiki/List_of_ISO_639_language_codes) standard for lang strings if you are adding additional languages.

## All Config Options
```js
{
  title: localised string,
  style: { // optional
     dark: { left_margin: localised int /* optional (default=0) */, left_value_pos: localised int /* optional (default=240) */} // optional - adjust menu-item columns
  },
  apply: {type: "OnChange" | "OnClose", func: callable}, // optional - add a function that applies your settings; also runs on load of save file (if using save feature)
  ini_name: string, // optional (defaults to an ini-safe version of the menu's title)
  save_type: "Never" | "Single" | "PerSlot" | "PerFile", // optional - never save, save to a single slot, save to up to 3 slots (based on current save slot), save per each game save data (same behaviour as vanilla saves)
  open_func: callable, // optional - when menu is opened
  close_func: callable, // optional - when menu is closed
  form: [
    {
      type: "Toggle",
      title: localised string,
      data_ref: {handle: handle /* optional(default=global) */, var_name: string, default_value: any, ini_key: string  /* optional */}, // reference to the variable that this menu-item should get/set, see Data Refs below
      value_range: localised string, // representation of the range of values that this menu-item can go through, see Value Ranges below
      no_save: bool, // optional - set true if you don't want this setting to be saved (if using save feature)
      preset_id: string, // optional - the id of the preset this menu-item belongs to, if any
      trigger_func: callable, // optional - when menu-item is clicked
      change_func: callable, // optional - when value is changed
      disabled: bool | callable, // optional - grey out menu item and prevent interaction
      hidden: bool | callable, // optional - prevent display of menu item
      ref: {handle: handle /* optional (default=global) */, var_name: string} // optional - reference to the variable that should hold a pointer to this menu-item
    },{
      type: "Slider",
      title: localised string,
      data_ref: {handle: handle /* optional (default=global)*/, var_name: string, default_value: any, ini_key: string  /* optional */}, // reference to the variable that this menu-item should get/set, see Data Refs below
      value_range: localised string, // representation of the range of values that this menu-item can go through, see Value Ranges below
      no_save: bool, // optional - set true if you don't want this setting to be saved (if using save feature)
      revert_on_cancel: bool | callable, // optional
      preset_id: string, // optional - the id of the preset this menu-item belongs to, if any
      trigger_func: callable, // optional - when menu-item is clicked
      change_func: callable, // optional - when value is changed
      cancel_func: callable, // optional - when slider is cancelled [X]/(B)
      accept_func: callable, // optional - when slider is accepted [Z]/(A)
      disabled: bool | callable, // optional - grey out menu item and prevent interaction
      hidden: bool | callable, // optional - prevent display of menu item
      ref: {handle: handle /* optional(default=global) */, var_name: string} // optional - reference to the variable that should hold a pointer to this menu-item
    },{
      type: "UserInput",
      title: localised string,
      data_ref: {handle: handle /* optional (default=global)*/, var_name: string, default_value: any, ini_key: string  /* optional */}, // reference to the variable that this menu-item should get/set, see Data Refs below
      max_length: localised numeric, // optional(default=12) - maximum length of the text the user can inout
      cutoff_length: localised numeric, // optional(default=12) - if value exceeds this length, display it cut-off with a '...' at the end
      no_save: bool, // optional - set true if you don't want this setting to be saved (if using save feature)
      revert_on_cancel: bool | callable, // optional
      preset_id: string, // optional - the id of the preset this menu-item belongs to, if any
      trigger_func: callable, // optional - when menu-item is clicked
      change_func: callable, // optional - when value is changed
      cancel_func: callable, // optional - when user-input is cancelled
      accept_func: callable, // optional - when user-input is accepted
      disabled: bool | callable, // optional - grey out menu item and prevent interaction
      hidden: bool | callable, // optional - prevent display of menu item
      ref: {handle: handle /* optional(default=global) */, var_name: string} // optional - reference to the variable that should hold a pointer to this menu-item
    },{
      type: "Button",
      title: localised string,
      trigger_func: callable, // when menu-item is clicked
      disabled: bool | callable, // optional - grey out menu item and prevent interaction
      hidden: bool | callable, // optional - prevent display of menu item
      ref: {handle: handle /* optional (default=global) */, var_name: string} // optional - reference to the variable that should hold a pointer to this menu-item
    },{
      type: "Header",
      title: localised string, // optional
      disabled: bool | callable, // optional - grey out menu item and prevent interaction
      hidden: bool | callable, // optional - prevent display of menu item
      ref: {handle: handle /* optional (default=global) */, var_name: string} // optional - reference to the variable that should hold a pointer to this menu-item
    },{
      type: "Reset",
      title: localised string, // optional(default="Reset to Defaults")
      preset_id: string, // optional - the id of the preset this menu-item belongs to, if any
      trigger_func: callable, // when menu-item is clicked
      disabled: bool | callable, // optional - grey out menu item and prevent interaction
      hidden: bool | callable, // optional - prevent display of menu item
      ref: {handle: handle /* optional (default=global) */, var_name: string} // optional - reference to the variable that should hold a pointer to this menu-item
    }
  ],
  additional_save_data_refs: [ // optional - all data that should not appear in the menu, but should still be saved/loaded (if using save feature)
    {handle: handle /* optional (default=global) */, var_name: string, default_value: any, ini_key: string  /* optional */} // reference to a variable that the menu should save/load, see Data Refs below
  ]
}
```
