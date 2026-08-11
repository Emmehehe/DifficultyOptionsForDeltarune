using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UndertaleModLib.Util;
using System.Text.RegularExpressions;
using System.Linq;

EnsureDataLoaded();
var displayName = Data?.GeneralInfo?.DisplayName?.Content;

// check version
UndertaleVariable alreadyInstalled = Data.Variables.ByName("installed_modmenu");
if (alreadyInstalled != null) {
    ScriptMessage($"Skiping ModMenu framework install for '{displayName}' as it is already installed.");
    return;
}

// Prefire checks
const string expectedDisplayName = "DELTARUNE \\S+ ([1-7](?:&2)?)";
if (!Regex.IsMatch(displayName, expectedDisplayName, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500)))
{
    ScriptError($"Error 0: data file display name does not match expected: '{expectedDisplayName}', actual display name: '{displayName}'.");
    return;
}

// Determine chapter
string ch_no_str = Regex.Match(displayName, expectedDisplayName, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500)).Groups[1].Captures[0].Value;
ushort ch_no = 0;
if (ch_no_str == "1&2")
    ch_no = 0; // 0 = demo
else
    ch_no = ushort.Parse(Regex.Match(displayName, expectedDisplayName, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500)).Groups[1].Captures[0].Value);

// Begin edit
ScriptMessage($"Installing ModMenu framework to '{displayName}'...");

// Load texture file
Dictionary<string, UndertaleEmbeddedTexture> textures = new Dictionary<string, UndertaleEmbeddedTexture>();

UndertaleEmbeddedTexture modmenuTexturePage = new UndertaleEmbeddedTexture();
modmenuTexturePage.TextureData.Image = GMImage.FromPng(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(ScriptPath), "modmenu.png")));
Data.EmbeddedTextures.Add(modmenuTexturePage);
textures.Add(Path.GetFileName(Path.Combine(Path.GetDirectoryName(ScriptPath), "modmenu.png")), modmenuTexturePage);

UndertaleTexturePageItem AddNewTexturePageItem(ushort sourceX, ushort sourceY, ushort sourceWidth, ushort sourceHeight)
{
    ushort targetX = 0;
    ushort targetY = 0;
    ushort targetWidth = sourceWidth;
    ushort targetHeight = sourceHeight;
    ushort boundingWidth = sourceWidth;
    ushort boundingHeight = sourceHeight;
    var texturePage = textures["modmenu.png"];

    UndertaleTexturePageItem tpItem = new() 
    { 
        SourceX = sourceX, 
        SourceY = sourceY, 
        SourceWidth = sourceWidth, 
        SourceHeight = sourceHeight, 
        TargetX = targetX, 
        TargetY = targetY, 
        TargetWidth = targetWidth, 
        TargetHeight = targetHeight, 
        BoundingWidth = boundingWidth, 
        BoundingHeight = boundingHeight, 
        TexturePage = texturePage,
        Name = new UndertaleString($"PageItem {Data.TexturePageItems.Count}")
    };
    Data.TexturePageItems.Add(tpItem);
    return tpItem;
}

UndertaleTexturePageItem pg_modsbt1 = AddNewTexturePageItem(0, 0, 33, 24);
UndertaleTexturePageItem pg_modsbt2 = AddNewTexturePageItem(0, 24, 33, 24);
UndertaleTexturePageItem pg_modsbt3 = AddNewTexturePageItem(0, 48, 33, 24);
UndertaleTexturePageItem pg_modsdesc = AddNewTexturePageItem(33, 0, 35, 18);
UndertaleTexturePageItem pg_modsfade = AddNewTexturePageItem(33, 18, 35, 35);

// add 'mods' button
{
    UndertaleSprite referenceSprite = Data.Sprites.ByName("spr_darkconfigbt");
    var name = Data.Strings.MakeString("spr_darkmodsbt");
    uint width = referenceSprite.Width;
    uint height = referenceSprite.Height;
    ushort marginLeft = 0;
    int marginRight = (int)width - 1;
    ushort marginTop = 0;
    int marginBottom = (int)height - 1;

    var sItem = new UndertaleSprite { Name = name, Width = width, Height = height, MarginLeft = marginLeft, MarginRight = marginRight, MarginTop = marginTop, MarginBottom = marginBottom };

    UndertaleTexturePageItem[] spriteTextures = { pg_modsbt1, pg_modsbt2, pg_modsbt3 };
    foreach (var spriteTexture in spriteTextures)
    {
        sItem.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = spriteTexture });
    }
    Data.Sprites.Add(sItem);
}

// add 'mods' menu description
if (ch_no == 0) {
    UndertaleSprite spr_darkmenudesc = Data.Sprites.ByName("spr_darkmenudesc_ch1");
    spr_darkmenudesc.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_modsdesc });
}
{
    UndertaleSprite spr_darkmenudesc = Data.Sprites.ByName("spr_darkmenudesc");
    spr_darkmenudesc.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_modsdesc });
}

// add modtitles fade
{
    var name = Data.Strings.MakeString("spr_darkmodsfade");
    uint width = 35;
    uint height = 35;
    ushort marginLeft = 0;
    int marginRight = (int)width - 1;
    ushort marginTop = 0;
    int marginBottom = (int)height - 1;

    var sItem = new UndertaleSprite { Name = name, Width = width, Height = height, MarginLeft = marginLeft, MarginRight = marginRight, MarginTop = marginTop, MarginBottom = marginBottom };

    UndertaleTexturePageItem[] spriteTextures = { pg_modsfade };
    foreach (var spriteTexture in spriteTextures)
    {
        sItem.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = spriteTexture });
    }
    Data.Sprites.Add(sItem);
}

// Code edits
UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data){
    ThrowOnNoOpFindReplace = true
};

// Script lists
string[] gamestarts = {"gml_GlobalScript_scr_gamestart"};
if (ch_no == 0)
{
    string[] demoGamestarts = {"gml_GlobalScript_scr_gamestart_ch1"};
    gamestarts = gamestarts.Concat(demoGamestarts).ToArray();
}
string[] darkcons = {"gml_Object_obj_darkcontroller"};
if (ch_no == 0)
{
    string[] demoDarkcons = {"gml_Object_obj_darkcontroller_ch1"};
    darkcons = darkcons.Concat(demoDarkcons).ToArray();
}

// modmenu core init
string modmenu_core_init = @$"
    var installed_modmenu = true;

    global.modmenu = {{
        {(ch_no == 0 ? @"
            // The demo is on an old version of game maker that doesn't have the string_split, string_ends_with, or string_trim functions so add (very) basic implementations
            // WARNING: only works for delimiters 1 char long
            // WARNING: does not have optional args from GM's impl
            string_split: function(arg0, arg1) {
                length = string_length(arg0);
                var result = array_create(0);
                array_push(result, """");

                // string_char_at index starts at 1 for some reason
                for (i = 1; i <= length; i++)
                {
                    thischar = string_char_at(arg0, i);

                    if (thischar != arg1) {
                        result[array_length(result) - 1] = result[array_length(result) - 1] + thischar;
                    }
                    else
                    {
                        array_push(result, """");
                    }
                }

                return result;
            },
            string_starts_with: function(arg0, arg1) {
                sublength = string_length(arg1);
                if (string_length(arg0) < sublength)
                    return false;

                // string_char_at index starts at 1 for some reason
                for (var i = 1; i <= sublength; i++) {
                    if (string_char_at(arg0, i) != string_char_at(arg1, i))
                        return false;
                }

                return true;
            },
            string_ends_with: function(arg0, arg1) {
                length = string_length(arg0);
                sublength = string_length(arg1);
                if (length < sublength)
                    return false;

                // string_char_at index starts at 1 for some reason
                for (var i = 1; i <= sublength; i++) {
                    if (string_char_at(arg0, length - sublength + i) != string_char_at(arg1, i))
                        return false;
                }

                return true;
            },
            // WARNING: only trims spaces, not other types of whitespace
            // WARNING: only trims whitespace from the start of the string
            string_trim: function(arg0) {
                length = string_length(arg0);
                result = """";
                var foundNonWS = false;

                // string_char_at index starts at 1 for some reason
                for (i = 1; i <= length; i++)
                {
                    thischar = string_char_at(arg0, i);

                    if (thischar != "" "") {
                        foundNonWS = true;
                    }

                    if (foundNonWS) {
                        result += thischar;
                    }
                }

                return result;
            },
        " : @"
            string_split: function(arg0, arg1) { return string_split(arg0, arg1); },
            string_starts_with: function(arg0, arg1) { return string_starts_with(arg0, arg1); },
            string_ends_with: function(arg0, arg1) { return string_ends_with(arg0, arg1); },
            string_trim: function(arg0) { return string_trim(arg0); },
        ")}
        menu_no: 0,
        row_no: -1,
        row_selected: false,
        row_scroll: 0,
        menus: [], // array_create(0),
        menus_light: [], // array_create(0),
        menus_dark: [], // array_create(0),
        menu_count: 0,
        menu_light_count: 0,
        menu_dark_count: 0,
        world_menus: function () {{
            if (!global.darkzone)
                return menus_light;
            else
                return menus_dark;
        }},
        world_menu_count: function () {{
            if (!global.darkzone)
                return menu_light_count;
            else
                return menu_dark_count;
        }},
        active_menu: function () {{
            var arr = world_menus();
            return arr[menu_no];
        }},

        surf_titles: -1,
        get_surf_titles: function () {{
            if (!surface_exists(surf_titles))
            {{
                surf_titles = surface_create(410, 35);
            }}
            return surf_titles;
        }},

        // Apply acceleration to the scrollers so that they're not too fidly but not too slow
        slider_step: 1, // reset to 1 as first interaction should be instantaneous
        slider_speed_min: 0,
        slider_speed_max: 3,
        slider_speed: 0,
        slider_accel: 1 / 20,
        slider_orig_value: undefined,

        // some translation mods replace the english translation rather than using DR's built in localisation support, so can't always rely on global.lang and have to override for certain mods
        lang_override: """",
        get_lang: function() {{ return (lang_override != """" ? lang_override : global.lang) }},
        find_loc: function(arg0, arg1) {{
            if (!is_array(arg0))
                return arg0;
            var lang = is_undefined(arg1) ? get_lang() : arg1;
            var first = """";
            for (var i = 0; i < array_length(arg0); i++) {{
                if (arg0[i].lang == lang)
                    return arg0[i].val;
                if (i == 0)
                    first = arg0[i].val;
            }}
            return first;
        }},

        // save/load
        string_savename_namechars: function(arg0) {{
            // only take alphanumeric, _, -, (, ), &; white space is converted to _
            var result = """";
            for (var i = 1; i <= string_length(arg0); i++) {{
                var thischar = string_char_at(arg0, i);
                if (thischar == string_lettersdigits(thischar) || thischar == ""_"" || thischar == ""-"" || thischar == ""("" || thischar == "")"" || thischar == ""&"")
                    result += thischar;
                else if (thischar == "" "" || thischar == ""\n"" || thischar == ""\t"" || thischar == ""\v"")
                    result += ""_"";
            }}

            // consolidate multiple _s (e.g. '_my__mod___' -> 'my_mod')
            var consolidated = """";
            var shouldconsolidate = true; // initially true to remove _s at start of string
            for (var i = 1; i <= string_length(result); i++) {{
                var thischar = string_char_at(result, i);
                var isunderscore = thischar == ""_"";
                if (shouldconsolidate && isunderscore) {{}} else
                    consolidated += thischar;
                shouldconsolidate = isunderscore;
            }}
            var lastchar = string_char_at(consolidated, string_length(consolidated));
            if (lastchar == ""_"")
                consolidated = string_delete(consolidated, string_length(consolidated), 1);

            return consolidated;
        }},
        string_savename: function(arg0) {{
            var result = arg0;
            if (global.modmenu.string_ends_with(result, "".ini""))
                result = string_delete(result, string_length(result)-3, 4);
            result = global.modmenu.string_savename_namechars(arg0);
            return result + "".ini"";
        }},
        string_savename_addini: function(arg0) {{
            if (global.modmenu.string_ends_with(arg0, "".ini""))
                return arg0;
            if (arg0 != global.modmenu.string_savename_namechars(arg0))
                return arg0;
            return arg0 + "".ini"";
        }},
        is_savenamestring: function(arg0) {{
            if (!global.modmenu.string_ends_with(arg0, "".ini""))
                return false;
            if (string_delete(arg0, string_length(arg0)-3, 4) != global.modmenu.string_savename_namechars(string_delete(arg0, string_length(arg0)-3, 4)))
                return false;
            return true;
        }},
        load: function(arg0, arg1) {{
            for (var i = 0; i < menu_count; i++) {{
                menus[i].load(arg0, arg1);
            }}
        }},
        save: function(arg0) {{
            for (var i = 0; i < menu_count; i++) {{
                menus[i].save(arg0);
            }}
        }},
        copy: function(arg0, arg1) {{
            for (var i = 0; i < menu_count; i++) {{
                menus[i].copy(arg0, arg1);
            }}
        }},
        del: function(arg0) {{
            for (var i = 0; i < menu_count; i++) {{
                menus[i].del(arg0);
            }}
        }},

        create: function (arg0) {{
            var menu = arg0;
            // Menu - mandatory
            try {{ var check = menu; if (is_undefined(check)) throw ""menu data is undefined""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but menu data was not supplied.""; }}
            if (!is_struct(menu)) throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but menu data is not a struct. "";
            try {{ var check = menu.title; if (!is_string(check)) check = check[0]; if (!is_string(check)) throw ""title[0] is not a string""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu without a title; or title is not a string.""; }}
            try {{ var check = menu.form[0]; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu without any form element.""; }}

            // Menu - optional
            try {{ var check = menu.style; if (is_undefined(check)) throw ""style not found""; }} catch (_e) {{ menu.style = {{ dark: {{ left_margin: 0, left_value_pos: 240 }} }}; }}
            try {{ var check = menu.style.dark; if (is_undefined(check)) throw ""style.dark not found""; }} catch (_e) {{ menu.style.dark = {{ left_margin: 0, left_value_pos: 240 }}; }}
            try {{ var check = menu.style.dark.left_margin; if (!is_numeric(check)) check = check[0]; }} catch (_e) {{ menu.style.dark.left_margin = 0; }}
            try {{ var check = menu.style.dark.left_margin; if (!is_numeric(check)) check = check[0]; if (!is_numeric(check)) throw ""style.dark.left_margin is not numeric""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but style.dark.left_margin is not numeric.""; }}
            try {{ var check = menu.style.dark.left_value_pos; if (!is_numeric(check)) check = check[0]; }} catch (_e) {{ menu.style.dark.left_value_pos = 240; }}
            try {{ var check = menu.style.dark.left_value_pos; if (!is_numeric(check)) check = check[0]; if (!is_numeric(check)) throw ""style.dark.left_value_pos is not numeric""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but style.dark.left_value_pos is not numeric.""; }}
            menu.style.dark = {{
                left_margin: menu.style.dark.left_margin,
                left_value_pos: menu.style.dark.left_value_pos,
                left_margin_loc: function(arg0) {{ return global.modmenu.find_loc(left_margin, arg0); }},
                left_value_pos_loc: function(arg0) {{ return global.modmenu.find_loc(left_value_pos, arg0); }},
            }};
            try {{ var check = menu.apply; }} catch (_e) {{ menu.apply = undefined; }}
            try {{ var check = menu.apply; if (!is_undefined(check) && ((check.type != ""OnChange"" && check.type != ""OnClose""))) throw ""apply type failed validation""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but apply.type is not in set: OnChange, OnClose.""; }}
            if (!is_undefined(menu.apply)) menu.apply = {{
                type: menu.apply.type,
                func: menu.apply.func,
                run_onchange: function() {{ if (type == ""OnChange"") func(); }},
                run_onclose: function() {{ if (type == ""OnClose"") func(); }},
                run_onload: function() {{ func(); }}
            }};
            // TODO auto generation for ini_name not work, haven't tested it
            try {{ var check = menu.ini_name; }} catch (_e) {{ menu.ini_name = string_savename(find_loc(menu.title)); }}
            {{ var check = menu.ini_name; if (is_string(check) && !is_savenamestring(check)) menu.ini_name = string_savename_addini(menu.ini_name); }}
            try {{ var check = menu.ini_name; if (!is_string(check) || !is_savenamestring(check)) throw ""ini_name isn't a string or contains invalid characters or no .ini""; }} catch (_e) {{ throw (""MODMENU VALIDATION ERROR: Tried to create a menu, but ini_name is missing; or is not a lower-case alphanumerical string. ini_name = '"" + string(menu.ini_name) + ""'""); }}
            try {{ var check = menu.save_type; }} catch (_e) {{ menu.save_type = ""Never""; }}
            try {{ var check = menu.save_type; if (check != ""Never"" && check != ""Single"" && check != ""PerSlot"" && check != ""PerFile"") throw ""save_type failed validation""; }} catch (_e) {{ throw (""MODMENU VALIDATION ERROR: Tried to create a menu, but save_type is not in set: Never, Single, PerSlot, PerFile.""); }}
            try {{ var check = menu.world; }} catch (_e) {{ menu.world = ""Dark""; }}
            try {{ var check = menu.world; if (!is_string(check) || (check != ""Dark"" && check != ""Light"" && check != ""Both"")) throw ""menu.world not a string in: Dark, Light, Both""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but world is not in set: Dark, Light, Both.""; }}
            try {{ var check = menu.open_func; }} catch (_e) {{ menu.open_func = function () {{}}; }}
            try {{ var check = menu.open_func; if (is_undefined(check)) throw ""open_func should not be undefined.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but open_func is undefined.""; }}
            try {{ var check = menu.close_func; }} catch (_e) {{ menu.close_func = function () {{}}; }}
            try {{ var check = menu.close_func; if (is_undefined(check)) throw ""close_func should not be undefined.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but close_func is undefined.""; }}
            try {{ var check = menu.additional_save_data_refs; }} catch (_e) {{ menu.additional_save_data_refs = []; }}
            try {{ var check = menu.additional_save_data_refs; if (!is_array(check)) throw ""additional_save_data_refs should be an array.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but additional_save_data_refs is not an array.""; }}

            var init_data_ref = function(arg0) {{
                var data_ref = arg0;
                // data ref - mandatory
                try {{ var check = data_ref; if (is_undefined(check)) throw ""data ref is undefined""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but one or more data refs are undefined. ""; }}
                if (!is_struct(data_ref)) throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but one or more data refs are not a struct. "";
                try {{ var check = data_ref.var_name; if (!is_string(check)) throw ""data ref var_name should be a string.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but data ref var_name is missing; or is not a string.""; }}
                try {{ var check = data_ref.default_value; if (!is_string(check) && !is_numeric(check)) throw ""data ref default_value should be a string or numeric.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but data ref default_value is missing; or is not a string/numeric.""; }}
                // data ref - optional
                try {{ var check = data_ref.handle; }} catch (_e) {{ data_ref.handle = global; }}
                try {{ var check = data_ref.handle; if (is_undefined(check)) throw ""data ref handle should not be undefined.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but data ref handle is not a handle.""; }}
                // strip 'global.' from data_ref.var_name
                {{ var check = data_ref.var_name; if (data_ref.handle == global && global.modmenu.string_starts_with(check, ""global."")) data_ref.var_name = string_delete(data_ref.var_name, 1, 7); }}
                try {{ var check = data_ref.ini_key; }} catch (_e) {{ data_ref.ini_key = data_ref.var_name; }}
                try {{ var check = data_ref.ini_key; if (!is_string(check)) throw ""data ref ini_key should be a string.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but data ref ini_key is not a string.""; }}
                // helper methods
                return {{
                    var_name: data_ref.var_name,
                    default_value: data_ref.default_value,
                    handle: data_ref.handle,
                    ini_key: data_ref.ini_key,
                    get: function() {{ return variable_instance_exists(handle, var_name) ? variable_instance_get(handle, var_name) : default_value; }},
                    set: function(arg0) {{ variable_instance_set(handle, var_name, (!is_undefined(arg0) ? arg0 : default_value)); }},
                    read: function(arg0 /* section */) {{
                        if (is_string(default_value)) return ini_read_string(arg0, ini_key, default_value);
                        if (is_numeric(default_value)) return ini_read_real(arg0, ini_key, default_value);
                        return default_value;
                    }},
                    write: function(arg0 /* section */, arg1 /* value */) {{
                        if (is_string(default_value)) ini_write_string(arg0, ini_key, arg1);
                        if (is_numeric(default_value)) ini_write_real(arg0, ini_key, arg1);
                    }},
                    load: function(arg0 /* section */) {{ set(read(arg0)); }},
                    save: function(arg0 /* section */) {{ write(arg0, get()); }},
                    copy: function(arg0 /* from section */, arg1 /* to section */) {{ write(arg1, read(arg0)); }}
                }};
            }};
            inited_form = [];
            for (var i = 0; i < array_length(menu.form); i++) {{
                var row = menu.form[i];
                // Form - mandatory
                try {{ var check = row; if (is_undefined(check)) throw ""row data is undefined""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but one or more form rows are undefined. ""; }}
                if (!is_struct(row)) throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but one or more form rows are not a struct. "";
                try {{ var check = row.type; if (!is_string(check) || (check != ""Slider"" && check != ""Toggle"" && check != ""Button"" && check != ""Header"")) throw ""row type should be a string in the set: Slider, Toggle, Button, Header""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but form row type is not a string in set: Slider, Toggle, Button, Header""; }}

                // Slider/Toggle/Button - mandatory | Header - optional
                if (row.type == ""Slider"" || row.type == ""Toggle"" || row.type == ""Button"") {{
                    try {{ var check = row.title; if (!is_string(check) && !is_array(check)) throw ""row title must be of type string or array""; if (is_array(check)) check = check[0]; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but form Slider/Toggle/Button does not have a title.""; }}
                }} else if (row.type == ""Header"") {{
                    try {{ var check = row.title; if (is_array(check)) check = check[0]; }} catch (_e) {{ row.title = """"; }}
                }} else throw (""Unsupported row type: "" + row.type);

                // Button - mandatory | Slider/Toggle - optional | Header - invalid
                if (row.type == ""Button"") {{
                    try {{ var check = row.trigger_func; if (is_undefined(check)) throw ""row trigger_func should not be undefined""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but Button does not have a trigger_func; or it is undefined.""; }}
                }} else if (row.type == ""Slider"" || row.type == ""Toggle"") {{
                    try {{ var check = row.trigger_func; }} catch (_e) {{ row.trigger_func = function() {{}}; }}
                    try {{ var check = row.trigger_func; if (is_undefined(check)) throw ""row trigger_func should not be undefined""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but Slider/Toggle trigger_func is undefined.""; }}
                }} else if (row.type == ""Header"") {{}} else throw (""Unsupported row type: "" + row.type);

                // Slider/Toggle - mandatory | Button/Header - invalid
                if (row.type == ""Slider"" || row.type == ""Toggle"") {{
                    try {{ var check = row.data_ref; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but form Slider/Toggle does not have a data_ref.""; }}
                    row.data_ref = init_data_ref(row.data_ref);
                    try {{ var check = row.value_range; if (!is_string(check) && !is_array(check)) throw ""row value_range must be of type string or array""; if (is_array(check)) check = check[0]; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but form Slider/Toggle does not have a value_range.""; }}
                }} else if (row.type == ""Button"" || row.type == ""Header"") {{}} else throw (""Unsupported row type: "" + row.type);

                // Slider/Toggle - optional | Button/Header - invalid
                if (row.type == ""Slider"" || row.type == ""Toggle"") {{
                    try {{ var check = row.no_save; }} catch (_e) {{ row.no_save = false; }}
                    try {{ var check = row.change_func; if (is_undefined(check)) throw ""row change_func should not be undefined""; }} catch (_e) {{ row.change_func = function() {{}}; }}
                }} else if (row.type == ""Button"" || row.type == ""Header"") {{}} else throw (""Unsupported row type: "" + row.type);

                // Slider - optional | Toggle/Button/Header - invalid
                if (row.type == ""Slider"") {{
                    try {{ var check = row.revert_on_cancel; if (is_undefined(check)) throw ""row revert_on_cancel should be a bool or a callable""; }} catch (_e) {{ row.revert_on_cancel = false; }}
                    try {{ var check = row.cancel_func; if (is_undefined(check)) throw ""row cancel_func should not be a undefined""; }} catch (_e) {{ row.cancel_func = function() {{}}; }}
                    try {{ var check = row.accept_func; if (is_undefined(check)) throw ""row accept_func should not be a undefined""; }} catch (_e) {{ row.accept_func = function() {{}}; }}
                }} else if (row.type == ""Toggle"" || row.type == ""Button"" || row.type == ""Header"") {{}} else throw (""Unsupported row type: "" + row.type);

                // Slider/Toggle/Button/Header - optional
                if (row.type == ""Slider"" || row.type == ""Toggle"" || row.type == ""Button"" || row.type == ""Header"") {{
                    try {{ var check = row.disabled; if (is_undefined(check)) throw ""row disabled should be a bool or a callable""; }} catch (_e) {{ row.disabled = false; }}
                    try {{ var check = row.hidden; if (is_undefined(check)) throw ""row hidden should be a bool or a callable""; }} catch (_e) {{ row.hidden = false; }}
                    try {{ var check = row.ref; }} catch (_e) {{ row.ref = undefined; }}
                    try {{ var check = row.ref; if (!is_undefined(check) && !is_string(check.var_name)) throw ""row ref var_name should be a string""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but form row ref does not have a valid value for 'var_name'.""; }}
                    try {{ var check = row.ref; if (!is_undefined(check)) check = check.handle; }} catch (_e) {{ row.ref.handle = global; }}
                    try {{ var check = row.ref; if (!is_undefined(check) && is_undefined(check.handle)) throw ""row ref handle should not be undefined""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but form row ref does not have a valid value for 'handle'.""; }}
                }} else throw (""Unsupported row type: "" + row.type);

                if (row.type == ""Toggle"")
                    row = {{
                        type: row.type,
                        title: row.title,
                        data_ref: row.data_ref,
                        value_range: row.value_range,
                        no_save: row.no_save,
                        trigger_func: row.trigger_func,
                        change_func: row.change_func,
                        disabled: row.disabled,
                        hidden: row.hidden,
                        ref: row.ref,
                        title_loc: function(arg0) {{ return global.modmenu.find_loc(title, arg0); }},
                        value_range_loc: function(arg0) {{ return global.modmenu.find_loc(value_range, arg0); }},
                        value_string: function() {{
                            var value = data_ref.get();
                            var value_range = value_range_loc();
                            var ranges = global.modmenu.string_split(value_range, "";"");
                            var valueString = """";

                            for (var j = 0; j < array_length(ranges); j++) {{
                                var range = ranges[j];
                                if (string_pos(""~"", range)) {{
                                    var minMax = global.modmenu.string_split(string_replace(range, ""%"", """"), ""~"");
                                    var isPercent = global.modmenu.string_ends_with(range, ""%"");
                                    var convVal = isPercent ? value * 100 : value;
                                    if (convVal <= minMax[1] || j+1 == array_length(ranges)) {{
                                        valueString = global.modmenu.string_trim(string_format(convVal, 3, (isPercent && convVal > -20 && convVal < 20) ? 1 : 0) + (isPercent ? ""%"" : """"));
                                        break;
                                    }}
                                }} else if (string_pos(""="", range)) {{
                                    var labelValue = global.modmenu.string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                    var isString = global.modmenu.string_ends_with(range, ""`"");
                                    var isPercent = !isString && global.modmenu.string_ends_with(range, ""%"");
                                    var isBool = !isPercent && (labelValue[1] == ""false"" || labelValue[1] == ""true"");

                                    var isMatch = false;
                                    if (isString)
                                        isMatch = value == labelValue[1];
                                    else if (isBool)
                                        isMatch = value == bool(labelValue[1]);
                                    else {{ // number
                                        var convBack = isPercent ? 1 / 100 : 1;
                                        isMatch = value == real(labelValue[1]) * convBack;
                                    }}

                                    if (isMatch || j+1 == array_length(ranges)) {{
                                        valueString = labelValue[0];
                                        break;
                                    }}
                                }} else if (global.modmenu.string_ends_with(range, ""%"")) {{
                                    var minMax = global.modmenu.string_split(string_replace(range, ""%"", """"), ""-"");
                                    if (value * 100 <= minMax[1] || j+1 == array_length(ranges)) {{
                                        valueString = global.modmenu.string_trim(string_format(value * 100, 3, value < 0.2 ? 1 : 0) + ""%"");
                                        break;
                                    }}
                                }}
                            }}

                            return valueString;
                        }},
                        is_disabled: function() {{ return !is_bool(disabled) ? disabled() : disabled; }},
                        is_hidden: function() {{ return !is_bool(hidden) ? hidden() : hidden; }}
                    }};
                else if (row.type == ""Slider"")
                    row = {{
                        type: row.type,
                        title: row.title,
                        data_ref: row.data_ref,
                        value_range: row.value_range,
                        no_save: row.no_save,
                        revert_on_cancel: row.revert_on_cancel,
                        trigger_func: row.trigger_func,
                        change_func: row.change_func,
                        cancel_func: row.cancel_func,
                        accept_func: row.accept_func,
                        disabled: row.disabled,
                        hidden: row.hidden,
                        ref: row.ref,
                        title_loc: function(arg0) {{ return global.modmenu.find_loc(title, arg0); }},
                        value_range_loc: function(arg0) {{ return global.modmenu.find_loc(value_range, arg0); }},
                        value_string: function() {{
                            var value = data_ref.get();
                            var value_range = value_range_loc();
                            var ranges = global.modmenu.string_split(value_range, "";"");
                            var valueString = """";

                            for (var j = 0; j < array_length(ranges); j++) {{
                                var range = ranges[j];
                                if (string_pos(""~"", range)) {{
                                    var minMax = global.modmenu.string_split(string_replace(range, ""%"", """"), ""~"");
                                    var isPercent = global.modmenu.string_ends_with(range, ""%"");
                                    var convVal = isPercent ? value * 100 : value;
                                    if (convVal <= minMax[1] || j+1 == array_length(ranges)) {{
                                        valueString = global.modmenu.string_trim(string_format(convVal, 3, (isPercent && convVal > -20 && convVal < 20) ? 1 : 0) + (isPercent ? ""%"" : """"));
                                        break;
                                    }}
                                }} else if (string_pos(""="", range)) {{
                                    var labelValue = global.modmenu.string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                    var isString = global.modmenu.string_ends_with(range, ""`"");
                                    var isPercent = !isString && global.modmenu.string_ends_with(range, ""%"");
                                    var isBool = !isPercent && (labelValue[1] == ""false"" || labelValue[1] == ""true"");

                                    var isMatch = false;
                                    if (isString)
                                        isMatch = value == labelValue[1];
                                    else if (isBool)
                                        isMatch = value == bool(labelValue[1]);
                                    else {{ // number
                                        var convBack = isPercent ? 1 / 100 : 1;
                                        isMatch = value == real(labelValue[1]) * convBack;
                                    }}

                                    if (isMatch || j+1 == array_length(ranges)) {{
                                        valueString = labelValue[0];
                                        break;
                                    }}
                                }} else if (global.modmenu.string_ends_with(range, ""%"")) {{
                                    var minMax = global.modmenu.string_split(string_replace(range, ""%"", """"), ""-"");
                                    if (value * 100 <= minMax[1] || j+1 == array_length(ranges)) {{
                                        valueString = global.modmenu.string_trim(string_format(value * 100, 3, value < 0.2 ? 1 : 0) + ""%"");
                                        break;
                                    }}
                                }}
                            }}

                            return valueString;
                        }},
                        is_disabled: function() {{ return !is_bool(disabled) ? disabled() : disabled; }},
                        is_hidden: function() {{ return !is_bool(hidden) ? hidden() : hidden; }}
                    }};
                else if (row.type == ""Button"")
                    row = {{
                        type: row.type,
                        title: row.title,
                        trigger_func: row.trigger_func,
                        disabled: row.disabled,
                        hidden: row.hidden,
                        ref: row.ref,
                        title_loc: function(arg0) {{ return global.modmenu.find_loc(title, arg0); }},
                        is_disabled: function() {{ return !is_bool(disabled) ? disabled() : disabled; }},
                        is_hidden: function() {{ return !is_bool(hidden) ? hidden() : hidden; }}
                    }};
                else if (row.type == ""Header"")
                    row = {{
                        type: row.type,
                        title: row.title,
                        disabled: row.disabled,
                        hidden: row.hidden,
                        ref: row.ref,
                        title_loc: function(arg0) {{ return global.modmenu.find_loc(title, arg0); }},
                        is_disabled: function() {{ return !is_bool(disabled) ? disabled() : disabled; }},
                        is_hidden: function() {{ return !is_bool(hidden) ? hidden() : hidden; }}
                    }};
                else throw (""Unsupported row type: "" + row.type);

                if (!is_undefined(row.ref)) variable_instance_set(row.ref.handle, row.ref.var_name, row);
                array_insert(inited_form, array_length(inited_form), row);
            }}
            menu.form = inited_form;
            inited_add_data_refs = [];
            for (var i = 0; i < array_length(menu.additional_save_data_refs); i++) {{
                array_insert(inited_add_data_refs, array_length(inited_add_data_refs), init_data_ref(menu.additional_save_data_refs[i]));
            }}
            menu.additional_save_data_refs = inited_add_data_refs;

            menu = {{
                title: menu.title,
                style: menu.style,
                apply: menu.apply,
                ini_name: menu.ini_name,
                save_type: menu.save_type,
                world: menu.world,
                open_func: menu.open_func,
                close_func: menu.close_func,
                form: menu.form,
                additional_save_data_refs: menu.additional_save_data_refs,
                title_loc: function(arg0) {{ return global.modmenu.find_loc(title, arg0); }},
                save_category: function(arg0, arg1) {{
                    switch (save_type) {{
                        case ""Single"":
                            return ""SETTINGS"";
                        case ""PerSlot"":
                            return ""SLOT"" + string(is_undefined(arg1) ? global.filechoice : arg1);
                        case ""PerFile"":
                            return ""CH"" + string(is_undefined(arg0) ? global.chapter : arg0) + ""_"" + string(is_undefined(arg1) ? global.filechoice : arg1);
                        default:
                            throw (""Unsupported save_type: "" + save_type);
                    }}
                }},
                load: function(arg0, arg1) {{
                    if (save_type == ""Never"")
                        return;

                    var section = save_category(arg0, arg1);
                    ossafe_ini_open(ini_name);
                    for (var i = 0; i < array_length(form); i++) {{
                        if (form[i].type == ""Slider"" || form[i].type == ""Toggle"") {{
                            if (!form[i].no_save)
                                form[i].data_ref.load(section);
                        }} else if (form[i].type == ""Button"" || form[i].type == ""Header"") {{}} else throw (""Unsupported row type: "" + form[i].type);
                    }}
                    for (var i = 0; i < array_length(additional_save_data_refs); i++) {{
                        additional_save_data_refs[i].load(section);
                    }}
                    ossafe_ini_close();

                    if (!is_undefined(apply))
                        apply.run_onload()
                }},
                save: function(arg0) {{
                    if (save_type == ""Never"")
                        return;

                    var section = save_category(undefined, arg0);
                    ossafe_ini_open(ini_name);
                    for (var i = 0; i < array_length(form); i++) {{
                        if (form[i].type == ""Slider"" || form[i].type == ""Toggle"") {{
                            if (!form[i].no_save)
                                form[i].data_ref.save(section);
                        }} else if (form[i].type == ""Button"" || form[i].type == ""Header"") {{}} else throw (""Unsupported row type: "" + form[i].type);
                    }}
                    for (var i = 0; i < array_length(additional_save_data_refs); i++) {{
                        additional_save_data_refs[i].save(section);
                    }}
                    ossafe_ini_close();
                }},
                copy: function(arg0, arg1) {{
                    if (save_type == ""Never"")
                        return;

                    var from = save_category(undefined, arg0);
                    var to = save_category(undefined, arg1);
                    ossafe_ini_open(ini_name);
                    for (var i = 0; i < array_length(form); i++) {{
                        if (form[i].type == ""Slider"" || form[i].type == ""Toggle"") {{
                            if (!form[i].no_save)
                                form[i].data_ref.copy(from, to);
                        }} else if (form[i].type == ""Button"" || form[i].type == ""Header"") {{}} else throw (""Unsupported row type: "" + form[i].type);
                    }}
                    for (var i = 0; i < array_length(additional_save_data_refs); i++) {{
                        additional_save_data_refs[i].copy(from, to);
                    }}
                    ossafe_ini_close();
                }},
                del: function(arg0) {{
                    if (save_type == ""Never"")
                        return;

                    var section = save_category(undefined, arg0);
                    ossafe_ini_open(ini_name);
                    if (ini_section_exists(section))
                        ini_section_delete(section);
                    ossafe_ini_close();
                }},
                all_data_refs: function() {{
                    var data_refs = [];
                    array_copy(data_refs, 0, additional_save_data_refs, 0, array_length(additional_save_data_refs));
                    for (var i = 0; i < array_length(form); i++) {{
                        if (form[i].type == ""Slider"" || form[i].type == ""Toggle"") {{
                            array_insert(data_refs, array_length(data_refs), form[i].data_ref);
                        }} else if (form[i].type == ""Button"" || form[i].type == ""Header"") {{}} else throw (""Unsupported row type: "" + form[i].type);
                    }}
                    return data_refs;
                }},
                save_data_refs: function() {{
                    var data_refs = [];
                    array_copy(data_refs, 0, additional_save_data_refs, 0, array_length(additional_save_data_refs));
                    for (var i = 0; i < array_length(form); i++) {{
                        if (form[i].type == ""Slider"" || form[i].type == ""Toggle"") {{
                            if (!form[i].no_save)
                                array_insert(data_refs, array_length(data_refs), form[i].data_ref);
                        }} else if (form[i].type == ""Button"" || form[i].type == ""Header"") {{}} else throw (""Unsupported row type: "" + form[i].type);
                    }}
                    return data_refs;
                }}
            }};

            array_insert(menus, array_length(menus), menu);
            menu_count++;
            if (menu.world == ""Light"" || menu.world == ""Both"") {{
                array_insert(menus_light, array_length(menus_light), menu);
                menu_light_count++;
            }}
            if (menu.world == ""Dark"" || menu.world == ""Both"") {{
                array_insert(menus_dark, array_length(menus_dark), menu);
                menu_dark_count++;
            }}
            return menu;
        }}
    }};
";

// Add modmenu init code
const useModularScripts = false;
if (useModularScripts) {
    importGroup.QueueAppend("gml_GlobalScript_scr_modmenu_init", modmenu_core_init);
} else {
    foreach (string gamestart in gamestarts)
    {
        importGroup.QueueRegexFindReplace(gamestart, "function scr_gamestart(?:_ch1)?\\(\\)\\s*{", @$"
            function scr_gamestart{(gamestart.EndsWith("_ch1") ? "_ch1" : "")}(){{
                {modmenu_core_init}
        ");
    }
}

// Add dark menu create code
foreach (string darkcon in darkcons)
{
    importGroup.QueuePrepend(darkcon + "_Create_0", "modmenu = global.modmenu;");
    if (ch_no == 0)
        importGroup.QueuePrepend(darkcon + "_Create_0", @"
            string_split = global.modmenu.string_split;
            string_starts_with = global.modmenu.string_starts_with;
            string_ends_with = global.modmenu.string_ends_with;
            string_trim = global.modmenu.string_trim;
        ");
}

// Add menu draw code
foreach (string darkcon in darkcons)
{
    importGroup.QueueTrimmedLinesFindReplace(darkcon + "_Draw_0", $"msprite[4] = spr_darkconfigbt{(darkcon.EndsWith("_ch1") ? "_ch1" : "")};", @$"
        msprite[4] = spr_darkconfigbt{(darkcon.EndsWith("_ch1") ? "_ch1" : "")};
        msprite[5] = spr_darkmodsbt;
        ");
    importGroup.QueueFindReplace(darkcon + "_Draw_0", "i = 0; i < 5; i += 1)", "i = 0; i < 6; i += 1)");
    importGroup.QueueTrimmedLinesFindReplace(darkcon + "_Draw_0", "spritemx = -100;", "spritemx = -80;");
    importGroup.QueueTrimmedLinesFindReplace(darkcon + "_Draw_0",
        "draw_sprite_ext(msprite[i], off, xx + 120 + (i * 100) + spritemx, (yy + tp) - 60, 2, 2, 0, c_white, 1);",
        "draw_sprite_ext(msprite[i], off, xx + 110 + (i * 80) + spritemx, (yy + tp) - 60, 2, 2, 0, c_white, 1);");
    string ch1_back_text = "scr_84_get_lang_string(\"obj_darkcontroller_slash_Draw_0_gml_96_0\")";
    string back_text = (ch_no >= 2 || ch_no == 0) ? "back_text" : ch1_back_text;
    importGroup.QueueAppend(darkcon + "_Draw_0", @$"
        if (global.menuno == 6)
        {{
            draw_set_color(c_black);

            if (global.lang == ""ja"")
            {{
                draw_rectangle(xx + 60, yy + 85, xx + 580, yy + 412, false);
                scr_darkbox(xx + 50, yy + 75, xx + 590, yy + 422);
            }}
            else
            {{
                draw_rectangle(xx + 60, yy + 90, xx + 580, yy + 410, false);
                scr_darkbox(xx + 50, yy + 80, xx + 590, yy + 420);
            }}

            draw_set_color(c_white);

            if (modmenu.menu_dark_count > 0)
            {{
                // top row buttons
                var isSubmenu = (modmenu.row_no >= 0);
                var isMenuLonely = modmenu.menu_dark_count == 1;

                var allmodmenus = """";

                for (var i = modmenu.menu_no; i < modmenu.menu_dark_count; i++)
                {{
                    allmodmenus += string_upper(modmenu.menus_dark[i].title_loc()) + (i + 1 < modmenu.menu_dark_count ? ""        "" : """");
                }}

                surface_set_target(modmenu.get_surf_titles());
                draw_clear_alpha(c_black, 0);

                if (isMenuLonely || !isSubmenu)
                {{
                    draw_set_color(c_white);
                    if (isMenuLonely)
                    {{
                        draw_set_halign(fa_center);
                        draw_text(205, 0, allmodmenus);
                        draw_set_halign(fa_left);
                    }}
                    else
                    {{
                        draw_text(0, 0, allmodmenus);
                    }}
                }}
                else
                {{
                    draw_set_color(c_gray);
                    draw_text(0, 0, allmodmenus);
                    draw_set_color(c_orange);
                    draw_text(0, 0, string_upper(modmenu.active_menu().title_loc()));
                }}

                draw_sprite(spr_darkmodsfade, 0, 410 - 35, 0);

                surface_reset_target();
                draw_surface(modmenu.get_surf_titles(), xx + 110, yy + 110);

                if (!isSubmenu) {{
                    menusiner += 1;
                    draw_sprite_part(spr_heart_harrows, menusiner / 20, 8 - 8 * (modmenu.menu_no > 0), 0, 16 + 8 * (modmenu.menu_no > 0) + 8 * (modmenu.menu_no < (modmenu.menu_dark_count - 1)), 16, xx + 85 - 8 * (modmenu.menu_no > 0), yy + 120);
                }}

                // form buttons
                var left_margin = modmenu.active_menu().style.dark.left_margin_loc();
                var _xPos = xx + 130 + left_margin;
                var _heartXPos = xx + 105 + left_margin;
                var _selectXPos = xx + 130 + modmenu.active_menu().style.dark.left_value_pos_loc();

                draw_set_color(c_white);

                if (!isSubmenu)
                    draw_set_color(c_gray);

                var form_data = modmenu.active_menu().form;

                var heartyprogress = 150;
                if (array_length(form_data) >= 0)
                {{
                    var i = modmenu.row_scroll;
                    var yprogress = 150;
                    while ((yprogress <= 150 + 6 * 35) && (i < array_length(form_data) + 1))
                    {{
                        if (i >= array_length(form_data))
                        {{
                            draw_set_color(c_white);
                            draw_text(_xPos, yy + yprogress{(ch_no == 1 ? " + 1" : "")}, string_hash_to_newline({(darkcon.EndsWith("_ch1") ? ch1_back_text : back_text)})); // Back
                            if (modmenu.row_no == i)
                                heartyprogress = yprogress;
                            yprogress += 35;
                            break;
                        }}

                        var row_data = form_data[i];
                        if (row_data.is_hidden()) {{
                            i++;
                            continue;
                        }}

                        if (row_data.is_disabled())
                            draw_set_color(c_gray);
                        else if (modmenu.row_selected && modmenu.row_no == i)
                            draw_set_color(c_yellow);
                        else
                            draw_set_color(c_white);

                        var isCategory = (row_data.type == ""Header"");
                        draw_text_transformed(_xPos - (isCategory * 28), yy + yprogress - (isCategory * 5){(ch_no == 1 ? " + 1" : "")}, string_hash_to_newline(row_data.title_loc()), (isCategory ? 0.5 : 1), (isCategory ? 0.5 : 1), 0);
                        if (isCategory){{
                            draw_line(_xPos - 28 - 3, yy + yprogress + 9, _xPos + 400, yy + yprogress + 9);
                        }}

                        if (row_data.type == ""Slider"" || row_data.type == ""Toggle"")
                            draw_text(_selectXPos, yy + yprogress{(ch_no == 1 ? " + 1" : "")}, string_hash_to_newline(row_data.value_string()));

                        if (modmenu.row_no == i){{
                            heartyprogress = yprogress;
                        }}
                        yprogress += (isCategory ? 12 : 35);
                        i++;
                    }}

                    // calcs required to get the scroller size & position correct: need to know how far we've scrolled & total length of menu in pixels
                    var menuscreenlength = 7 * 35;
                    var totalmenulength = 0;
                    var scrollprogress = 0;
                    for (var i = 0; i < array_length(form_data) + 1; i++) {{
                        if (i >= array_length(form_data))
                        {{
                            if (modmenu.row_scroll == i){{
                                scrollprogress = totalmenulength;
                            }}
                            totalmenulength += 35;
                            continue;
                        }}

                        if (modmenu.row_scroll == i){{
                            scrollprogress = totalmenulength;
                        }}
                        totalmenulength += ((form_data[i].type == ""Header"") ? 12 : 35);
                    }}

                    // also need to account for empty space at the bottom of the menu
                    var lastscreenlength = 0;
                    for (var i = array_length(form_data); i >= 0; i--) {{
                        var newlastscreenlength = lastscreenlength;
                        if (i >= array_length(form_data))
                        {{
                            newlastscreenlength += 35;
                        }}
                        else
                        {{
                            newlastscreenlength += ((form_data[i].type == ""Header"") ? 12 : 35);
                        }}

                        if (newlastscreenlength > menuscreenlength)
                        {{
                            break;
                        }}
                        lastscreenlength = newlastscreenlength;
                    }}
                    totalmenulength += menuscreenlength - lastscreenlength;

                    // draw scroll bar based on previous calcs
                    if (totalmenulength > menuscreenlength)
                    {{
                        var modscrollbary = 180;
                        var modscrollbarlength = 190;
                        var modscrollery = modscrollbarlength * (scrollprogress / totalmenulength);
                        var modscrollerlength = modscrollbarlength * (menuscreenlength / totalmenulength);
                        draw_set_color(c_dkgray);
                        draw_rectangle(xx + 85, yy + modscrollbary, xx + 90, yy + modscrollbary + modscrollbarlength, false);
                        draw_set_color(c_white);
                        draw_rectangle(xx + 85, yy + modscrollbary + modscrollery, xx + 90, yy + modscrollbary + modscrollerlength + modscrollery, false);

                        if (modmenu.row_scroll > 0)
                            draw_sprite_ext(spr_morearrow, 0, xx + 81, (yy + modscrollbary) - 10 - (sin(cur_jewel / 12) * 3), 1, -1, 0, c_white, 1);

                        if ((modmenu.row_scroll + 7) < (array_length(form_data) + 1))
                            draw_sprite_ext(spr_morearrow, 0, xx + 81, yy + 10 + modscrollbary + modscrollbarlength + (sin(cur_jewel / 12) * 3), 1, 1, 0, c_white, 1);
                    }}
                }}

                if (isSubmenu)
                    draw_sprite(spr_heart, 0, _heartXPos, yy + 10 + heartyprogress);
            }}
            else
            {{
                draw_set_halign(fa_center);
                draw_set_valign(fa_middle);
                draw_text(xx + 320, yy + 250, string_hash_to_newline(""NO MOD MENUS FOUND""));
                draw_set_halign(fa_left);
                draw_set_valign(fa_top);
            }}
        }}
    ");
}

// Add menu step code
foreach (string darkcon in darkcons)
{
    importGroup.QueueTrimmedLinesFindReplace(darkcon + "_Step_0", "global.menucoord[0] = 4;", "global.menucoord[0] = 5;");
    importGroup.QueueTrimmedLinesFindReplace(darkcon + "_Step_0", "if (global.menucoord[0] == 4)", "if (global.menucoord[0] == 5)");
    importGroup.QueueAppend(darkcon + "_Step_0", @$"
        // override for deltaesp's spanish translation
        if (modmenu.lang_override != ""es"" && global.lang == ""en"" && variable_instance_exists(global, ""esp_names""))
        {{
            modmenu.lang_override = ""es"";
        }}
        // override for the Korean translation
        // TODO this doesn't work for chapter 2 as the dubbing feature hasn't been added
        if (modmenu.lang_override != ""ko"" && global.lang == ""ja"" && variable_instance_exists(global, ""krdub""))
        {{
            modmenu.lang_override = ""ko"";
        }}

        function scrolldownforcontent()
        {{
            var form_data = modmenu.active_menu().form;
            modmenu.row_scroll = modmenu.row_no + 1;
            var menuscreenlength = 7 * 35;
            var lastscreenlength = 0;
            for (var i = modmenu.row_no; i >= 0; i--) {{
                var newlastscreenlength = lastscreenlength;
                if (i >= array_length(form_data))
                {{
                    newlastscreenlength += 35;
                }}
                else
                {{
                    newlastscreenlength += ((form_data[i].type == ""Header"") ? 12 : 35);
                }}

                if (newlastscreenlength > menuscreenlength)
                {{
                    break;
                }}
                lastscreenlength = newlastscreenlength;
                modmenu.row_scroll--;
            }}
        }}

        function isneedscrolldown()
        {{
            var form_data = modmenu.active_menu().form;
            var menuscreenlength = 7 * 35;
            var currentscreenlength = 0;
            var foundselected = false;
            for (var i = modmenu.row_scroll; i < array_length(form_data) + 1; i++) {{
                var newcurrentscreenlength = currentscreenlength;
                if (i >= array_length(form_data))
                {{
                    newcurrentscreenlength += 35;
                }}
                else
                {{
                    newcurrentscreenlength += ((form_data[i].type == ""Header"") ? 12 : 35);
                }}

                if (newcurrentscreenlength > menuscreenlength)
                {{
                    break;
                }}
                currentscreenlength = newcurrentscreenlength;
                if (i == modmenu.row_no)
                {{
                    foundselected = true;
                }}
            }}
            return !foundselected;
        }}

        function modsubmenu_up(arg0)
        {{
            modmenu.row_no--;

            if (modmenu.row_no < 0)
            {{
                modmenu.row_no = arg0 - 1;

                scrolldownforcontent();
            }}
            else
            {{
                if (modmenu.row_no < modmenu.row_scroll)
                    modmenu.row_scroll = modmenu.row_no;
            }}
        }}

        function modsubmenu_down(arg0)
        {{
            modmenu.row_no++;

            if (modmenu.row_no >= arg0)
            {{
                modmenu.row_no = 0;
                modmenu.row_scroll = 0;
            }}
            else if (isneedscrolldown())
            {{
                scrolldownforcontent();
            }}
        }}

        function issubmenucategory(arg0, arg1)
        {{
            if (modmenu.row_no >= (arg0 - 1))
                return false;

            return (arg1[modmenu.row_no].type == ""Header"");
        }}

        function ishidden(arg0, arg1)
        {{
            if (modmenu.row_no >= (arg0 - 1))
                return false;

            return arg1[modmenu.row_no].is_hidden();
        }}

        function isdisabled(arg0, arg1)
        {{
            if (modmenu.row_no >= (arg0 - 1))
                return false;

            return arg1[modmenu.row_no].is_disabled();
        }}

        function shouldskiprow(arg0, arg1)
        {{
            return issubmenucategory(arg0, arg1) || ishidden(arg0, arg1);
        }}

        if (global.menuno == 6)
        {{
            var isSubmenu = (modmenu.row_no >= 0);

            if (!isSubmenu) {{
                // enter submenu right away if there is only one submenu
                if (modmenu.menu_dark_count == 1) {{
                    modmenu.row_no = 0;

                    modmenu.active_menu().open_func();
                }}

                if (modmenu.menu_dark_count > 0)
                {{
                    if (left_p())
                    {{
                        movenoise = 1;

                        modmenu.menu_no--;
                        if (modmenu.menu_no < 0)
                            modmenu.menu_no = modmenu.menu_dark_count - 1;
                    }}
                    if (right_p())
                    {{
                        movenoise = 1;

                        modmenu.menu_no++;
                        if (modmenu.menu_no >= modmenu.menu_dark_count)
                            modmenu.menu_no = 0;
                    }}
                    if ((button1_p() || down_p() || up_p()) && onebuffer < 0 && twobuffer < 0)
                    {{
                        onebuffer = 2;
                        selectnoise = 1;

                        // make sure category header or hidden/disabled row isn't selected
                        var form_data = modmenu.active_menu().form;
                        var form_length = array_length(form_data);
                        // nav to bottom if press up, top otherwise
                        modmenu.row_no = up_p() ? form_length : 0;
                        // back button
                        form_length++;
                        var movecount = 0;
                        while ((movecount < form_length + 1) && shouldskiprow(form_length, form_data)) {{
                            modsubmenu_down(form_length);
                            movecount++;
                        }}

                        modmenu.active_menu().open_func();
                    }}
                }}
                if (button2_p() && onebuffer < 0 && twobuffer < 0)
                {{
                    cancelnoise = 1;
                    twobuffer = 2;
                    global.menuno = 0;
                    global.submenu = 0;
                }}
            }} else if (!modmenu.row_selected) {{
                var form_data = modmenu.active_menu().form;
                var form_length = array_length(form_data);

                if (form_length <= 0) {{
                    modmenu.row_no = -1;
                    modmenu.row_scroll = 0;
                }}

                // back button
                form_length++;

                // TODO freezes game :/ // state change could leave us stranded on a non-selectable row, so need to check
                // var movecount = 0;
                // while ((movecount < form_length + 1) && shouldskiprow(form_length, form_data)) {{
                //     modsubmenu_down(form_length);
                //     movecount++;
                // }}

                if (up_p())
                {{
                    movenoise = 1;

                    modsubmenu_up(form_length);

                    // make sure category header or hidden/disabled row isn't selected
                    var movecount = 0;
                    while ((movecount < form_length + 1) && (shouldskiprow(form_length, form_data))) {{
                        modsubmenu_up(form_length);
                        movecount++;
                    }}
                }}
                if (down_p())
                {{
                    movenoise = 1;

                    modsubmenu_down(form_length);

                    // make sure category header or hidden/disabled row isn't selected
                    var movecount = 0;
                    while ((movecount < form_length + 1) && shouldskiprow(form_length, form_data)) {{
                        modsubmenu_down(form_length);
                        movecount++;
                    }}
                }}
                if (button1_p() && onebuffer < 0 && twobuffer < 0 && !isdisabled(form_length, form_data))
                {{
                    onebuffer = 2;
                    selectnoise = 1;

                    if (modmenu.row_no >= array_length(form_data)) {{
                        modmenu.row_no = -1;
                        modmenu.row_scroll = 0;

                        if (modmenu.menu_dark_count == 1)
                        {{
                            global.menuno = 0;
                            global.submenu = 0;
                        }}

                        modmenu.active_menu().close_func();
                        if (!is_undefined(modmenu.active_menu().apply)) modmenu.active_menu().apply.run_onclose();
                        if (modmenu.active_menu().save_type != ""Never"") modmenu.active_menu().save();
                    }}
                    else
                    {{
                        modmenu.row_selected = true;
                        var row_data = form_data[modmenu.row_no];

                        if (row_data.type != ""Header"")
                            row_data.trigger_func();

                        if (row_data.type != ""Slider"")
                            modmenu.row_selected = false;
                        else
                            modmenu.slider_orig_value = row_data.data_ref.get();

                        if (row_data.type == ""Toggle"") {{
                            // TODO does this handle spread ranges properly?
                            var value_range = row_data.value_range_loc();
                            var ranges = global.modmenu.string_split(value_range, "";"");
                            var value = row_data.data_ref.get();

                            var foundOption = false;
                            for (var i = 0; i < array_length(ranges); i++) {{
                                var range = ranges[i];
                                if (string_pos(""="", range)) {{
                                    var labelValue = global.modmenu.string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                    var isString = global.modmenu.string_ends_with(range, ""`"");
                                    var isPercent = !isString && global.modmenu.string_ends_with(range, ""%"");
                                    var isBool = !isPercent && (labelValue[1] == ""false"" || labelValue[1] == ""true"");

                                    var isMatch = false;
                                    if (isString)
                                        isMatch = value == labelValue[1];
                                    else if (isBool)
                                        isMatch = value == bool(labelValue[1]);
                                    else {{ // number
                                        var convBack = isPercent ? 1 / 100 : 1;
                                        isMatch = value == real(labelValue[1]) * convBack;
                                    }}

                                    if (!foundOption && i+1 == array_length(ranges)) {{
                                        range = ranges[0];
                                        labelValue = global.modmenu.string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                        isString = global.modmenu.string_ends_with(range, ""`"");
                                        isPercent = !isString && global.modmenu.string_ends_with(range, ""%"");
                                        isBool = !isPercent && (labelValue[1] == ""false"" || labelValue[1] == ""true"");
                                    }}

                                    if (foundOption || i+1 == array_length(ranges)) {{
                                        if (isString)
                                            value = labelValue[1];
                                        else if (isBool)
                                            value = bool(labelValue[1]);
                                        else {{ // number
                                            value = real(labelValue[1]) * convBack;
                                        }}
                                        break;
                                    }}

                                    if (isMatch) {{
                                        foundOption = true;
                                    }}
                                }}
                            }}

                            row_data.data_ref.set(value);
                            row_data.change_func();
                            if (!is_undefined(modmenu.active_menu().apply)) modmenu.active_menu().apply.run_onchange();
                        }}
                    }}
                }}
                if (button2_p() && onebuffer < 0 && twobuffer < 0)
                {{
                    cancelnoise = 1;
                    twobuffer = 2;
                    modmenu.row_no = -1;
                    modmenu.row_scroll = 0;

                    if (modmenu.menu_dark_count == 1)
                    {{
                        global.menuno = 0;
                        global.submenu = 0;
                    }}

                    modmenu.active_menu().close_func();
                    if (!is_undefined(modmenu.active_menu().apply)) modmenu.active_menu().apply.run_onclose();
                    if (modmenu.active_menu().save_type != ""Never"") modmenu.active_menu().save();
                }}
            }} else {{
                var form_data = modmenu.active_menu().form;
                var row_data = form_data[modmenu.row_no];
                var value_range = row_data.value_range_loc();
                var ranges = global.modmenu.string_split(value_range, "";"");
                var value = row_data.data_ref.get();

                var scroll_todo = modmenu.slider_step div 1;

                if (right_h() && scroll_todo > 0)
                {{
                    var isAllLabels = true;

                    for (var i = 0; i < array_length(ranges); i++) {{
                        var range = ranges[i];
                        if (!string_pos(""="", range)) {{
                            isAllLabels = false;
                            break;
                        }}
                    }}

                    if (isAllLabels) {{
                        var foundOption = false;
                        for (var i = 0; i < array_length(ranges); i++) {{
                            var range = ranges[i];
                            if (string_pos(""="", range)) {{
                                var labelValue = global.modmenu.string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                var isString = global.modmenu.string_ends_with(range, ""`"");
                                var isPercent = !isString && global.modmenu.string_ends_with(range, ""%"");
                                var isBool = !isPercent && (labelValue[1] == ""false"" || labelValue[1] == ""true"");

                                var isMatch = false;
                                if (isString)
                                    isMatch = value == labelValue[1];
                                else if (isBool)
                                    isMatch = value == bool(labelValue[1]);
                                else {{ // number
                                    var convBack = isPercent ? 1 / 100 : 1;
                                    isMatch = value == real(labelValue[1]) * convBack;
                                }}

                                if (!foundOption && i+1 == array_length(ranges)) {{
                                    range = ranges[0];
                                    labelValue = global.modmenu.string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                    isString = global.modmenu.string_ends_with(range, ""`"");
                                    isPercent = !isString && global.modmenu.string_ends_with(range, ""%"");
                                    isBool = !isPercent && (labelValue[1] == ""false"" || labelValue[1] == ""true"");
                                }}

                                if (foundOption || i+1 == array_length(ranges)) {{
                                    if (isString)
                                        value = labelValue[1];
                                    else if (isBool)
                                        value = bool(labelValue[1]);
                                    else {{ // number
                                        value = real(labelValue[1]) * convBack;
                                    }}
                                    break;
                                }}

                                if (isMatch) {{
                                    foundOption = true;
                                }}
                            }}
                        }}
                    }}
                    else
                    {{
                        var value_adjust = 0;
                        if (value <= -2)
                            value_adjust = 0.1;
                        else if (value <= -1)
                            value_adjust = 0.05;
                        else if (value <= -0.5)
                            value_adjust = 0.02;
                        else if (value <= -0.2)
                            value_adjust = 0.01;
                        else if (value < 0.2)
                            value_adjust = 0.005;
                        else if (value < 0.5)
                            value_adjust = 0.01;
                        else if (value < 1)
                            value_adjust = 0.02;
                        else if (value < 2)
                            value_adjust = 0.05;
                        else
                            value_adjust = 0.1;

                        value += value_adjust * scroll_todo;

                        for (var i = 0; i < array_length(ranges); i++) {{
                            var range = ranges[i];
                            if (string_pos(""~"", range)) {{
                                var minMax = global.modmenu.string_split(string_replace(range, ""%"", """"), ""~"");
                                var isPercent = global.modmenu.string_ends_with(range, ""%"");
                                if (!isPercent)
                                    value = ceil(value);
                                var convVal = isPercent ? value * 100 : value;
                                var convBack = isPercent ? 1 / 100 : 1;
                                if (convVal <= real(minMax[1]) || i+1 == array_length(ranges)) {{
                                    value = clamp(value, real(minMax[0]) * convBack, real(minMax[1]) * convBack);
                                    break;
                                }}
                            }} else if (string_pos(""="", range)) {{
                                var labelValue = global.modmenu.string_split(string_replace(range, ""%"", """"), ""="");
                                var isPercent = global.modmenu.string_ends_with(range, ""%"");
                                var convBack = isPercent ? 1 / 100 : 1;
                                if (value <= (real(labelValue[1]) * convBack) || i+1 == array_length(ranges)) {{
                                    value = real(labelValue[1]) * convBack;
                                    break;
                                }}
                            }} else if (global.modmenu.string_ends_with(range, ""%"")) {{
                                var minMax = global.modmenu.string_split(string_replace(range, ""%"", """"), ""-"");
                                if (value * 100 <= real(minMax[1]) || i+1 == array_length(ranges)) {{
                                    value = clamp(value, real(minMax[0]) / 100, real(minMax[1]) / 100);
                                    break;
                                }}
                            }}
                        }}
                    }}

                    row_data.data_ref.set(value);

                    row_data.change_func();
                    if (!is_undefined(modmenu.active_menu().apply)) modmenu.active_menu().apply.run_onchange();

                    modmenu.slider_step = modmenu.slider_step % 1;
                }}

                if (left_h() && scroll_todo > 0)
                {{
                    var isAllLabels = true;

                    for (var i = 0; i < array_length(ranges); i++) {{
                        var range = ranges[i];
                        if (!string_pos(""="", range)) {{
                            isAllLabels = false;
                            break;
                        }}
                    }}

                    if (isAllLabels) {{
                        var foundOption = false;
                        for (var i = array_length(ranges) - 1; i >= 0; i--) {{
                            var range = ranges[i];
                            if (string_pos(""="", range)) {{
                                var labelValue = global.modmenu.string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                var isString = global.modmenu.string_ends_with(range, ""`"");
                                var isPercent = !isString && global.modmenu.string_ends_with(range, ""%"");
                                var isBool = !isPercent && (labelValue[1] == ""false"" || labelValue[1] == ""true"");

                                var isMatch = false;
                                if (isString)
                                    isMatch = value == labelValue[1];
                                else if (isBool)
                                    isMatch = value == bool(labelValue[1]);
                                else {{ // number
                                    var convBack = isPercent ? 1 / 100 : 1;
                                    isMatch = value == real(labelValue[1]) * convBack;
                                }}

                                if (!foundOption && i == 0) {{
                                    range = ranges[array_length(ranges) - 1];
                                    labelValue = global.modmenu.string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                    isString = global.modmenu.string_ends_with(range, ""`"");
                                    isPercent = !isString && global.modmenu.string_ends_with(range, ""%"");
                                    isBool = !isPercent && (labelValue[1] == ""false"" || labelValue[1] == ""true"");
                                }}

                                if (foundOption || i == 0) {{
                                    if (isString)
                                        value = labelValue[1];
                                    else if (isBool)
                                        value = bool(labelValue[1]);
                                    else {{ // number
                                        value = real(labelValue[1]) * convBack;
                                    }}
                                    break;
                                }}

                                if (isMatch) {{
                                    foundOption = true;
                                }}
                            }}
                        }}
                    }}
                    else
                    {{
                        var value_adjust = 0;
                        if (value < -2)
                            value_adjust = -0.1;
                        else if (value < -1)
                            value_adjust = -0.05;
                        else if (value < -0.5)
                            value_adjust = -0.02;
                        else if (value < -0.2)
                            value_adjust = -0.01;
                        else if (value <= 0.2)
                            value_adjust = -0.005;
                        else if (value <= 0.5)
                            value_adjust = -0.01;
                        else if (value <= 1)
                            value_adjust = -0.02;
                        else if (value <= 2)
                            value_adjust = -0.05;
                        else
                            value_adjust = -0.1;

                        var scroll_todo = modmenu.slider_step div 1;
                        value += value_adjust * scroll_todo;

                        for (var i = array_length(ranges) - 1; i >= 0; i--) {{
                            var range = ranges[i];
                            if (string_pos(""~"", range)) {{
                                var minMax = global.modmenu.string_split(string_replace(range, ""%"", """"), ""~"");
                                var isPercent = global.modmenu.string_ends_with(range, ""%"");
                                if (!isPercent)
                                    value = floor(value);
                                var convVal = isPercent ? value * 100 : value;
                                var convBack = isPercent ? 1 / 100 : 1;
                                if (convVal >= real(minMax[0]) || i == 0) {{
                                    value = clamp(value, real(minMax[0]) * convBack, real(minMax[1]) * convBack);
                                    break;
                                }}
                            }} else if (string_pos(""="", range)) {{
                                var labelValue = global.modmenu.string_split(string_replace(range, ""%"", """"), ""="");
                                var isPercent = global.modmenu.string_ends_with(range, ""%"");
                                var convBack = isPercent ? 1 / 100 : 1;
                                if (value >= (real(labelValue[1]) * convBack) || i == 0) {{
                                    value = real(labelValue[1]) * convBack;
                                    break;
                                }}
                            }} else if (global.modmenu.string_ends_with(range, ""%"")) {{
                                var minMax = global.modmenu.string_split(string_replace(range, ""%"", """"), ""-"");
                                if (value * 100 >= real(minMax[0]) || i == 0) {{
                                    value = clamp(value, real(minMax[0]) / 100, real(minMax[1]) / 100);
                                    break;
                                }}
                            }}
                        }}
                    }}

                    row_data.data_ref.set(value);

                    row_data.change_func();
                    if (!is_undefined(modmenu.active_menu().apply)) modmenu.active_menu().apply.run_onchange();

                    modmenu.slider_step = modmenu.slider_step % 1;
                }}

                if (right_h() || left_h())
                {{
                    modmenu.slider_step += modmenu.slider_speed;
                    modmenu.slider_speed = clamp(modmenu.slider_speed + modmenu.slider_accel, modmenu.slider_speed_min, modmenu.slider_speed_max);
                }}
                else
                {{
                    modmenu.slider_step = 1; // reset to 1 as first interaction should be instantaneous
                    modmenu.slider_speed = modmenu.slider_speed_min;
                }}

                se_select = 0;
                se_cancel = 0;

                if (button1_p() && onebuffer < 0)
                    se_select = 1;

                if (button2_p() && twobuffer < 0)
                    se_cancel = 1;

                if (se_select == 1 || se_cancel == 1)
                {{
                    selectnoise = 1;
                    onebuffer = 2;
                    twobuffer = 2;
                    modmenu.row_selected = false;

                    if (se_select == 1)
                        row_data.accept_func();
                    if (se_cancel == 1) {{
                        if (row_data.revert_on_cancel && row_data.data_ref.get() != modmenu.slider_orig_value) {{
                            row_data.data_ref.set(modmenu.slider_orig_value);
                            row_data.change_func();
                            if (!is_undefined(modmenu.active_menu().apply)) modmenu.active_menu().apply.run_onchange();
                        }}
                        row_data.cancel_func();
                    }}

                    modmenu.slider_step = 1; // reset to 1 as first interaction should be instantaneous
                    modmenu.slider_speed = modmenu.slider_speed_min;
                    modmenu.slider_orig_value = undefined;
                }}
            }}
        }}
    ");
}

// Save menu data
string[] saveLikes = {"gml_GlobalScript_scr_saveprocess"};
if (ch_no == 0)
{
    string[] demoSaveLikes = {"gml_GlobalScript_scr_saveprocess_ch1"};
    saveLikes = saveLikes.Concat(demoSaveLikes).ToArray();
}
foreach (string scrName in saveLikes)
{
    importGroup.QueueTrimmedLinesFindReplace(scrName, $"{(ch_no == 0 ? "var is_valid = " : "")}ossafe_file_text_close{(scrName.EndsWith("_ch1") ? "_ch1" : "")}(myfileid);", @$"
        {(ch_no == 0 ? "var is_valid = " : "")}ossafe_file_text_close{(scrName.EndsWith("_ch1") ? "_ch1" : "")}(myfileid);

        global.modmenu.save();
        ");
}

// Load menu data
(string script, string chapter) [] loadLikes = {("gml_GlobalScript_scr_load", "global.chapter")};
if (ch_no == 0)
{
    (string script, string chapter) [] loadCh1 = {("gml_GlobalScript_scr_load_ch1", "global.chapter")};
    loadLikes = loadLikes.Concat(loadCh1).ToArray();
}
if (ch_no > 1 || ch_no == 0)
{
    (string script, string chapter) [] loadCh1 = {("gml_GlobalScript_scr_load_chapter1", "1")};
    loadLikes = loadLikes.Concat(loadCh1).ToArray();
}
if (ch_no > 2)
{
    (string script, string chapter) [] loadCh2 = {("gml_GlobalScript_scr_load_chapter2", "2")};
    loadLikes = loadLikes.Concat(loadCh2).ToArray();
}
if (ch_no > 3)
{
    (string script, string chapter) [] loadCh3 = {("gml_GlobalScript_scr_load_chapter3", "3")};
    loadLikes = loadLikes.Concat(loadCh3).ToArray();
}
if (ch_no > 4)
{
    (string script, string chapter) [] loadCh4 = {("gml_GlobalScript_scr_load_chapter4", "4")};
    loadLikes = loadLikes.Concat(loadCh4).ToArray();
}
// if (ch_no > 5)
// {
//     (string script, string chapter) [] loadCh5 = {("gml_GlobalScript_scr_load_chapter5", "5")};
//     loadLikes = loadLikes.Concat(loadCh5).ToArray();
// }
// if (ch_no > 6)
// {
//     (string script, string chapter) [] loadCh6 = {("gml_GlobalScript_scr_load_chapter6", "6")};
//     loadLikes = loadLikes.Concat(loadCh6).ToArray();
// }

foreach ((string script, string chapter) loadLike in loadLikes)
{
    importGroup.QueueTrimmedLinesFindReplace(loadLike.script, $"ossafe_file_text_close{(loadLike.script.EndsWith("_ch1") ? "_ch1" : "")}(myfileid);", @$"
        ossafe_file_text_close{(loadLike.script.EndsWith("_ch1") ? "_ch1" : "")}(myfileid);

        global.modmenu.load({loadLike.chapter});
        ");
}

// Copy menu data
string[] copyLikes = {"gml_Object_DEVICE_MENU_Other_15"};
if (ch_no == 0)
{
    string[] demoCopyLikes = {"gml_Object_DEVICE_MENU_ch1_Other_15"};
    copyLikes = copyLikes.Concat(demoCopyLikes).ToArray();
}
foreach (string scrName in copyLikes)
{
    importGroup.QueueTrimmedLinesFindReplace(scrName, @"file_copy(""keyconfig_"" + string(MENUCOORD[2]) + "".ini"", ""keyconfig_"" + string(MENUCOORD[3]) + "".ini"");", @$"
        file_copy(""keyconfig_"" + string(MENUCOORD[2]) + "".ini"", ""keyconfig_"" + string(MENUCOORD[3]) + "".ini"");

        global.modmenu.copy(MENUCOORD[2], MENUCOORD[3]);
    ");
}

// Delete menu data
string[] deleteLikes = {"gml_Object_DEVICE_MENU_Step_0"};
if (ch_no == 0)
{
    string[] demoDeleteLikes = {"gml_Object_DEVICE_MENU_ch1_Step_0"};
    copyLikes = deleteLikes.Concat(demoDeleteLikes).ToArray();
}
foreach (string scrName in deleteLikes)
{
    importGroup.QueueTrimmedLinesFindReplace(scrName, @"TIME_STRING[MENUCOORD[5]] = ""--:--"";", @"
        TIME_STRING[MENUCOORD[5]] = ""--:--"";

        global.modmenu.del(MENUCOORD[5]);
    ");
}

// Finish edit
importGroup.Import();
ScriptMessage($"Success: ModMenu framework installed to '{displayName}'!");
