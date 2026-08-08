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
    ScriptMessage($"Skiping mod menu install for '{displayName}' as it is already installed.");
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
ScriptMessage($"Adding mod menu to '{displayName}'...");

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

// The demo is on an old version of game maker that doesn't have the string_split, string_ends_with, or string_trim functions so add (very) basic implementations
if (ch_no == 0) {
    foreach (string darkcon in darkcons)
    {
        // WARNING: only works for delimiters 1 char long
        // WARNING: does not have optional args from GM's impl
        string string_split = @"
            function string_split(arg0, arg1)
            {
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
            }

        ";
        importGroup.QueuePrepend(darkcon + "_Step_0", string_split);
        importGroup.QueuePrepend(darkcon + "_Draw_0", string_split);

        // WARNING: only works for substr 1 char long
        string string_ends_with = @"
            function string_ends_with(arg0, arg1)
            {
                length = string_length(arg0);
                // string_char_at index starts at 1 for some reason
                lastchar = string_char_at(arg0, length);

                return (lastchar == arg1);
            }

        ";
        importGroup.QueuePrepend(darkcon + "_Step_0", string_ends_with);
        importGroup.QueuePrepend(darkcon + "_Draw_0", string_ends_with);

        // WARNING: only trims spaces, not other types of whitespace
        // WARNING: only trims whitespace from the start of the string
        string string_trim = @"
            function string_trim(arg0)
            {
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
            }

        ";
        importGroup.QueuePrepend(darkcon + "_Step_0", string_trim);
        importGroup.QueuePrepend(darkcon + "_Draw_0", string_trim);
    }
}

// Add modmenu gamestart code
foreach (string gamestart in gamestarts)
{
    importGroup.QueueRegexFindReplace(gamestart, "function scr_gamestart(?:_ch1)?\\(\\)\\s*{", @$"
        function scr_gamestart{(gamestart.EndsWith("_ch1") ? "_ch1" : "")}()

        var installed_modmenu = true;

        modmenu = {{
            menu_no: 0,
            row_no: -1,
            row_selected: false,
            row_scroll: 0,
            menus: [], // array_create(0),
            menu_count: 0,
            active_menu: function () {{ return menus[menu_no] }},

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
            slider_speed: modmenu.slider_speed_min,
            slider_accel: 1 / 20,

            // some translation mods replace the english translation rather than using DR's built in localisation support, so can't always rely on global.lang and have to override for certain mods
            lang_override: """",
            get_lang: function () {{ return (modmenu.lang_override != """" ? modmenu.lang_override : global.lang) }},
            find_loc: function (arg0, arg1) {{
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
            string_savename: function(arg0)
            {{
                var result = string_lower(arg0);
                if (string_ends_with(result, "".ini""))
                    result = string_delete(result, 0, -4);
                result = string_lettersdigits(arg0);
                return result + "".ini"";
            }},
            string_savename_addini: function(arg0)
            {{
                if (arg0 != string_lower(arg0))
                    return arg0;
                if (string_ends_with(arg0, "".ini""))
                    return arg0;
                if (arg0 != string_lettersdigits(arg0))
                    return arg0;
                return arg0 + "".ini"";
            }},
            is_savenamestring: function(arg0)
            {{
                if (arg0 != string_lower(arg0))
                    return false;
                if (!string_ends_with(arg0, "".ini""))
                    return false;
                if (arg0 != string_lettersdigits(arg0))
                    return false;
                return true;
            }},

            create: function (arg0) {{
                var menu = arg0;
                // Menu - mandatory
                try {{ var check = menu; if (is_undefined(check)) throw ""menu data is undefined""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but menu data was not supplied.""; }}
                if (!is_struct(menu)) throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but menu data is not a struct. "";
                try {{ var check = menu.title; if (!is_string(check)) check = check[0]; if (!is_string(check)) throw ""title[0] is not a string""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu without a title; or title is not a string.""; }}
                menu.title_loc = function(arg0) {{ return find_loc(menu.title, arg0); }};
                try {{ var check = menu.form[0]; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu without any form element.""; }}

                // Menu - optional
                try {{ var check = menu.left_margin; if (!is_numeric(check)) check = check[0]; } catch (_e) {{ menu.left_margin = 40; }}
                try {{ var check = menu.left_margin; if (!is_numeric(check)) check = check[0]; if (!is_numeric(check)) throw ""left_margin is not numeric""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but left_margin is not numeric.""; }}
                menu.left_margin_loc = function(arg0) {{ return find_loc(menu.left_margin, arg0); };
                try {{ var check = menu.left_value_pos; if (!is_numeric(check)) check = check[0]; } catch (_e) {{ menu.left_value_pos = 300; }}
                try {{ var check = menu.left_value_pos; if (!is_numeric(check)) check = check[0]; if (!is_numeric(check)) throw ""left_value_pos is not numeric""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but left_value_pos is not numeric.""; }}
                menu.left_value_pos_loc = function(arg0) {{ return find_loc(menu.left_value_pos, arg0); }};
                try {{ var check = menu.apply; } catch (_e) {{ menu.apply = undefined; }}
                try {{ var check = menu.apply; if (!is_undefined(check) && ((check.type != ""OnChange"" && check.type != ""OnClose"") || !is_callable(check.func))) throw ""apply type or func failed validation""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but apply.type is not in set: OnChange, OnClose; or apply.func is not callable.""; }}
                if (!is_undefined(menu.apply)) menu.apply.run_onchange = function() {{ if (menu.apply.type == ""OnChange"") menu.apply.func(); }};
                if (!is_undefined(menu.apply)) menu.apply.run_onclose = function() {{ if (menu.apply.type == ""OnClose"") menu.apply.func(); }};
                if (!is_undefined(menu.apply)) menu.apply.run_onload = function() {{ menu.apply.func(); }};
                try {{ var check = menu.save; } catch (_e) {{ menu.save = undefined; }}
                try {{ var check = menu.save; if (!is_undefined(check) && ((check.type != ""Single"" && check.type != ""PerSlot"" && check.type != ""PerFile""))) throw ""save type failed validation""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but save.type is not in set: Single, PerSlot, PerFile.""; }}
                try {{ var check = menu.save; if (!is_undefined(check)) check = check.name; } catch (_e) {{ menu.save.name = string_savename(find_loc(menu.title)); }}
                try {{ var check = menu.save; if (!is_undefined(check)) check = check.name; if (is_string(check) && !is_savenamestring(check)) menu.save.name = string_savename_addini(menu.save.name); }}
                try {{ var check = menu.save; if (!is_undefined(check)) check = check.name; if (!is_string(check) || !is_savenamestring(check)) throw ""save name isn't a string or contains invalid characters or no .ini"";}} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but save.name is missing; or is not a lower-case alphanumerical string.""; }}
                if (!is_undefined(menu.save)) menu.save.category = function (arg0) {{
                    switch (menu.save.type) {{
                        case ""Single"":
                            return ""SETTINGS"";
                        case ""PerSlot"":
                            return ""SLOT"" + string(is_undefined(arg0) ? global.filechoice : arg0);
                        case ""PerFile"":
                            return ""CH"" + string(global.chapter) + ""_"" + string(is_undefined(arg0) ? global.filechoice : arg0);
                        default:
                            throw (""Unsupported save type: "" + menu.save.type);
                    }}
                }};
                try {{ var check = menu.world; }} catch (_e) {{ menu.world = ""Dark""; }}
                try {{ var check = menu.world; if (!is_string(check) || (check != ""Dark"" && check != ""Light"" && check != ""Both"")) throw ""menu.world not a string in: Dark, Light, Both""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but world is not in set: Dark, Light, Both.""; }}
                try {{ var check = menu.open_func; }} catch (_e) {{ menu.open_func = function () {{}}; }}
                try {{ var check = menu.open_func; if (!is_callable(check)) throw ""open_func should be callable.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but open_func is not callable.""; }}
                try {{ var check = menu.close_func; }} catch (_e) {{ menu.close_func = function () {{}}; }}
                try {{ var check = menu.close_func; if (!is_callable(check)) throw ""close_func should be callable.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but close_func is not callable.""; }}
                try {{ var check = menu.additional_save_data_refs; }} catch (_e) {{ menu.additional_save_data_refs = []; }}
                try {{ var check = menu.additional_save_data_refs; if (!is_array(check)) throw ""additional_save_data_refs should be an array.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but additional_save_data_refs is not an array.""; }}

                // helper methods
                menu.load = function(arg0) {{
                    if (is_undefined(menu.save))
                        return;

                    var section = menu.save.category(arg0);
                    ossafe_ini_open(menu.save.name);
                    for (var i = 0; i < array_length(menu.form); i++) {{
                        if (menu.form[i].no_save) {{}} else if (menu.form[i].type == ""Slider"" || menu.form[i].type == ""Toggle"") {{
                            menu.form[i].data_ref.load(section);
                        }} else if (menu.form[i].type == ""Button"" || menu.form[i].type == ""Header"") {{}} else throw (""Unsupported row type: "" + menu.form[i].type);
                    }}
                    for (var i = 0; i < array_length(menu.additional_save_data_refs); i++) {{
                        menu.additional_save_data_refs[i].load(section);
                    }}
                    ossafe_ini_close();

                    if (!is_undefined(menu.apply))
                        menu.apply.run_onload()
                }};
                menu.save = function(arg0) {{
                    if (is_undefined(menu.save))
                        return;

                    var section = menu.save.category(arg0);
                    ossafe_ini_open(menu.save.name);
                    for (var i = 0; i < array_length(menu.form); i++) {{
                        if (menu.form[i].no_save) {{}} else if (menu.form[i].type == ""Slider"" || menu.form[i].type == ""Toggle"") {{
                            menu.form[i].data_ref.save(section);
                        }} else if (menu.form[i].type == ""Button"" || menu.form[i].type == ""Header"") {{}} else throw (""Unsupported row type: "" + menu.form[i].type);
                    }}
                    for (var i = 0; i < array_length(menu.additional_save_data_refs); i++) {{
                        menu.additional_save_data_refs[i].save(section);
                    }}
                    ossafe_ini_close();
                }};
                menu.copy = function(arg0, arg1) {{
                    if (is_undefined(menu.save))
                        return;

                    var from = menu.save.category(arg0);
                    var to = menu.save.category(arg0);
                    ossafe_ini_open(menu.save.name);
                    for (var i = 0; i < array_length(menu.form); i++) {{
                        if (menu.form[i].no_save) {{}} else if (menu.form[i].type == ""Slider"" || menu.form[i].type == ""Toggle"") {{
                            menu.form[i].data_ref.copy(from, to);
                        }} else if (menu.form[i].type == ""Button"" || menu.form[i].type == ""Header"") {{}} else throw (""Unsupported row type: "" + menu.form[i].type);
                    }}
                    for (var i = 0; i < array_length(menu.additional_save_data_refs); i++) {{
                        menu.additional_save_data_refs[i].copy(from, to);
                    }}
                    ossafe_ini_close();
                }};
                menu.delete = function(arg0) {{
                    if (is_undefined(menu.save))
                        return;

                    var section = menu.save.category(arg0);
                    ossafe_ini_open(menu.save.name);
                    if (ini_section_exists(section))
                        ini_section_delete(section);
                    ossafe_ini_close();
                }};

                var init_data_ref = function(arg0) {{
                    var data_ref = arg0;
                    // data ref - mandatory
                    try {{ var check = data_ref; if (is_undefined(check)) throw ""data ref is undefined""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but one or more data refs are undefined. ""; }}
                    if (!is_struct(data_ref)) throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but one or more data refs are not a struct. "";
                    try {{ var check = data_ref.var; if (!is_string(check)) throw ""data ref var should be a string.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but data ref var is missing; or is not a string.""; }}
                    try {{ var check = data_ref.default; if (!is_string(check) && !is_numeric(check)) throw ""data ref default should be a string or numeric.""; } catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but data ref default is missing; or is not a string/numeric.""; }}
                    // data ref - optional
                    try {{ var check = data_ref.handle; }} catch (_e) {{ data_ref.handle = global; }}
                    try {{ var check = data_ref.handle; if (!is_handle(check)) throw ""data ref handle should be a handle.""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but data ref handle is not a handle.""; }}
                    // strip 'global.' from data_ref.var
                    {{ var check = data_ref.var; if (data_ref.handle == global && string_starts_with(check, ""global."")) data_ref.var = string_delete(data_ref.var, 0, 7); }}
                    // helper methods
                    data_ref.get = function() {{ return variable_instance_exists(data_ref.handle, data_ref.name) ? variable_instance_get(data_ref.handle, data_ref.name) : data_ref.default; }};
                    data_ref.set = function(arg0) {{ variable_instance_set(data_ref.handle, data_ref.name, (!is_undefined(arg0) ? arg0 : data_ref.default)); }};
                    data_ref.read = function(arg0 /* section */) {{
                        if (is_string(data_ref.default)) return ini_read_string(arg0, data_ref.name, data_ref.default);
                        if (is_numeric(data_ref.default)) return ini_read_real(arg0, data_ref.name, data_ref.default);
                        return data_ref.default;
                    }};
                    data_ref.write = function(arg0 /* section */, arg1 /* value */) {{
                        if (is_string(data_ref.default)) ini_write_string(arg0, data_ref.name, arg1);
                        if (is_numeric(data_ref.default)) ini_write_real(arg0, data_ref.name, arg1);
                    }};
                    data_ref.load = function(arg0 /* section */) {{
                        data_ref.set(data_ref.read(arg0));
                    }};
                    data_ref.save = function(arg0 /* section */) {{
                        data_ref.write(arg0, data_ref.get());
                    }};
                    data_ref.copy = function(arg0 /* from section */, arg1 /* to section */) {{
                        data_ref.write(arg1, data_ref.read(arg0));
                    }};
                }};
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
                    row.title_loc = function(arg0) {{ return find_loc(row.title, arg0); }};

                    // Button - mandatory | Slider/Toggle - optional | Header - invalid
                    if (row.type == ""Button"") {{
                        try {{ var check = row.trigger_func; if (!is_callable(check)) throw ""row trigger_func should be a callable""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but Button does not have a trigger_func; or it is not callable.""; }}
                    }} else if (row.type == ""Slider"" || row.type == ""Toggle"") {{
                        try {{ var check = row.trigger_func; }} catch (_e) {{ row.trigger_func = function() {{}}; }}
                        try {{ var check = row.trigger_func; if (!is_callable(check)) throw ""row trigger_func should be a callable""; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but Slider/Toggle trigger_func is not callable.""; }}
                    }} else if (row.type == ""Header"") {{}} else throw (""Unsupported row type: "" + row.type);

                    // Slider/Toggle - mandatory | Button/Header - invalid
                    if (row.type == ""Slider"" || row.type == ""Toggle"") {{
                        try {{ var check = row.data_ref; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but form Slider/Toggle does not have a data_ref.""; }}
                        init_data_ref(row.data_ref);
                        try {{ var check = row.value_range; if (!is_string(check) && !is_array(check)) throw ""row value_range must be of type string or array""; if (is_array(check)) check = check[0]; }} catch (_e) {{ throw ""MODMENU VALIDATION ERROR: Tried to create a menu, but form Slider/Toggle does not have a value_range.""; }}
                        row.value_range_loc = function(arg0) {{ return find_loc(row.value_range, arg0); }};
                        row.value_string = function() {{
                            var value = row.data_ref.get();
                            var value_range = row.value_range_loc();
                            var ranges = string_split(value_range, "";"");
                            var valueString = """";

                            for (var j = 0; j < array_length(ranges); j++) {{
                                var range = ranges[j];
                                if (string_pos(""~"", range)) {{
                                    var minMax = string_split(string_replace(range, ""%"", """"), ""~"");
                                    var isPercent = string_ends_with(range, ""%"");
                                    var convVal = isPercent ? value * 100 : value;
                                    if (convVal <= minMax[1] || j+1 == array_length(ranges)) {{
                                        valueString = string_trim(string_format(convVal, 3, (isPercent && convVal > -20 && convVal < 20) ? 1 : 0) + (isPercent ? ""%"" : """"));
                                        break;
                                    }}
                                }} else if (string_pos(""="", range)) {{
                                    var labelValue = string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                    var isString = string_ends_with(range, ""`"");
                                    var isPercent = !isString && string_ends_with(range, ""%"");
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
                                }} else if (string_ends_with(range, ""%"")) {{
                                    var minMax = string_split(string_replace(range, ""%"", """"), ""-"");
                                    if (value * 100 <= minMax[1] || j+1 == array_length(ranges)) {{
                                        valueString = string_trim(string_format(value * 100, 3, value < 0.2 ? 1 : 0) + ""%"");
                                        break;
                                    }}
                                }}
                            }}

                            return valueString;
                        }};
                    }} else if (row.type == ""Button"" || row.type == ""Header"") {{}} else throw (""Unsupported row type: "" + row.type);

                    // Slider/Toggle - optional | Button/Header - invalid
                    if (row.type == ""Slider"" || row.type == ""Toggle"") {{
                        try {{ var check = row.no_save; }} catch (_e) {{ row.no_save = false; }}
                        try {{ var check = row.change_func; if (!is_callable(check)) throw ""row change_func should be a callable""; }} catch (_e) {{ row.change_func = function() {{}}; }}
                    }} else if (row.type == ""Button"" || row.type == ""Header"") {{}} else throw (""Unsupported row type: "" + row.type);

                    // Slider - optional | Toggle/Button/Header - invalid
                    if (row.type == ""Slider"") {{
                        try {{ var check = row.revert_on_cancel; if (!is_bool(check) && !is_callable(check)) throw ""row revert_on_cancel should be a bool or a callable""; }} catch (_e) {{ row.revert_on_cancel = false; }}
                        try {{ var check = row.cancel_func; if (!is_callable(check)) throw ""row cancel_func should be a callable""; }} catch (_e) {{ row.cancel_func = function() {{}}; }}
                        try {{ var check = row.accept_func; if (!is_callable(check)) throw ""row accept_func should be a callable""; }} catch (_e) {{ row.accept_func = function() {{}}; }}
                    }} else if (row.type == ""Toggle"" || row.type == ""Button"" || row.type == ""Header"") {{}} else throw (""Unsupported row type: "" + row.type);

                    // Slider/Toggle/Button/Header - optional
                    if (row.type == ""Slider"" || row.type == ""Toggle"" || row.type == ""Button"" || row.type == ""Header"") {{
                        try {{ var check = row.disabled; if (!is_bool(check) && !is_callable(check)) throw ""row disabled should be a bool or a callable""; }} catch (_e) {{ row.disabled = false; }}
                        try {{ var check = row.hidden; if (!is_bool(check) && !is_callable(check)) throw ""row hidden should be a bool or a callable""; }} catch (_e) {{ row.hidden = false; }}
                        row.is_disabled = function() {{ return is_callable(row.disabled) ? row.disabled() : row.disabled; }};
                        row.is_hidden = function() {{ return is_callable(row.hidden) ? row.hidden() : row.hidden; }};
                    }} else throw (""Unsupported row type: "" + row.type);
                }}
                for (var i = 0; i < array_length(menu.additional_save_data_refs); i++) {{
                    init_data_ref(menu.additional_save_data_refs[i]);
                }}

                array_insert(menus, array_length(menus), menu);
                menu_count++;
                return menu;
            }}
        }};
        global.modmenu = modmenu;
    ");
}

// Add dark menu create code
foreach (string darkcon in darkcons)
{
    importGroup.QueueAppend(darkcon + "_Create_0", "modmenu = global.modmenu;");
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

            if (modmenu.menu_count > 0)
            {{
                // top row buttons
                var isSubmenu = (modmenu.row_no >= 0);
                var isMenuLonely = modmenu.menu_count == 1;

                var allmodmenus = """";

                for (var i = modmenu.menu_no; i < modmenu.menu_count; i++)
                {{
                    allmodmenus += string_upper(modmenu.menus[i].title_loc()) + (i + 1 < modmenu.menu_count ? ""        "" : """");
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
                    draw_sprite_part(spr_heart_harrows, menusiner / 20, 8 - 8 * (modmenu.menu_no > 0), 0, 16 + 8 * (modmenu.menu_no > 0) + 8 * (modmenu.menu_no < (modmenu.menu_count - 1)), 16, xx + 85 - 8 * (modmenu.menu_no > 0), yy + 120);
                }}

                // form buttons
                var left_margin = modmenu.active_menu().left_margin_loc();
                var _xPos = xx + 130 + left_margin;
                var _heartXPos = xx + 105 + left_margin;
                var _selectXPos = xx + 130 + modmenu.active_menu().left_value_pos_loc();

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

            return (form_data[i].type == ""Header"");
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
                if (modmenu.menu_count == 1)
                    modmenu.row_no = 0;

                if (modmenu.menu_count > 0)
                {{
                    if (left_p())
                    {{
                        movenoise = 1;

                        modmenu.menu_no--;
                        if (modmenu.menu_no < 0)
                            modmenu.menu_no = modmenu.menu_count - 1;
                    }}
                    if (right_p())
                    {{
                        movenoise = 1;

                        modmenu.menu_no++;
                        if (modmenu.menu_no >= modmenu.menu_count)
                            modmenu.menu_no = 0;
                    }}
                    if (button1_p() && onebuffer < 0 && twobuffer < 0)
                    {{
                        onebuffer = 2;
                        selectnoise = 1;
                        modmenu.row_no = 0;

                        // make sure category header or hidden/disabled row isn't selected
                        var form_data = modmenu.active_menu().form;
                        var form_length = array_length(form_data);
                        // back button
                        form_length++;
                        var movecount = 0;
                        while ((movecount < form_length + 1) && shouldskiprow(form_length, form_data)) {{
                            modsubmenu_down(form_length);
                            movecount++;
                        }}
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

                        if (modmenu.menu_count == 1)
                        {{
                            global.menuno = 0;
                            global.submenu = 0;
                        }}

                        modmenu.active_menu().close_func();
                    }}
                    else
                    {{
                        modmenu.row_selected = true;

                        // if range is only labels just cycle through them
                        var row_data = form_data[modmenu.row_no];
                        var value_range = row_data.value_range_loc();
                        var ranges = string_split(value_range, "";"");

                        if (row_data.type != ""Slider"") {{
                            modmenu.row_selected = false;
                            modmenu.slider_orig_value = row_data.data_ref.get();
                        }}

                        if (row_data.type == ""Toggle"") {{
                            // TODO does this handle spread ranges properly?
                            var value = row_data.data_ref.get();

                            var foundOption = false;
                            for (var i = 0; i < array_length(ranges); i++) {{
                                var range = ranges[i];
                                if (string_pos(""="", range)) {{
                                    var labelValue = string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                    var isString = string_ends_with(range, ""`"");
                                    var isPercent = !isString && string_ends_with(range, ""%"");
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
                                        labelValue = string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                        isString = string_ends_with(range, ""`"");
                                        isPercent = !isString && string_ends_with(range, ""%"");
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
                        }}

                        if (row_data.type != ""Header"")
                            row_data.trigger_func();
                    }}
                }}
                if (button2_p() && onebuffer < 0 && twobuffer < 0)
                {{
                    cancelnoise = 1;
                    twobuffer = 2;
                    modmenu.row_no = -1;
                    modmenu.row_scroll = 0;

                    if (modmenu.menu_count == 1)
                    {{
                        global.menuno = 0;
                        global.submenu = 0;
                    }}

                    modmenu.active_menu().close_func();
                }}
            }} else {{
                var form_data = modmenu.active_menu().form;
                var row_data = form_data[modmenu.row_no];
                var value_range = row_data.value_range_loc();
                var ranges = string_split(value_range, "";"");
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
                                var labelValue = string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                var isString = string_ends_with(range, ""`"");
                                var isPercent = !isString && string_ends_with(range, ""%"");
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
                                    labelValue = string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                    isString = string_ends_with(range, ""`"");
                                    isPercent = !isString && string_ends_with(range, ""%"");
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
                                var minMax = string_split(string_replace(range, ""%"", """"), ""~"");
                                var isPercent = string_ends_with(range, ""%"");
                                if (!isPercent)
                                    value = ceil(value);
                                var convVal = isPercent ? value * 100 : value;
                                var convBack = isPercent ? 1 / 100 : 1;
                                if (convVal <= real(minMax[1]) || i+1 == array_length(ranges)) {{
                                    value = clamp(value, real(minMax[0]) * convBack, real(minMax[1]) * convBack);
                                    break;
                                }}
                            }} else if (string_pos(""="", range)) {{
                                var labelValue = string_split(string_replace(range, ""%"", """"), ""="");
                                var isPercent = string_ends_with(range, ""%"");
                                var convBack = isPercent ? 1 / 100 : 1;
                                if (value <= (real(labelValue[1]) * convBack) || i+1 == array_length(ranges)) {{
                                    value = real(labelValue[1]) * convBack;
                                    break;
                                }}
                            }} else if (string_ends_with(range, ""%"")) {{
                                var minMax = string_split(string_replace(range, ""%"", """"), ""-"");
                                if (value * 100 <= real(minMax[1]) || i+1 == array_length(ranges)) {{
                                    value = clamp(value, real(minMax[0]) / 100, real(minMax[1]) / 100);
                                    break;
                                }}
                            }}
                        }}
                    }}

                    row_data.data_ref.set(value);

                    row_data.change_func();

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
                                var labelValue = string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                var isString = string_ends_with(range, ""`"");
                                var isPercent = !isString && string_ends_with(range, ""%"");
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
                                    labelValue = string_split(string_replace(string_replace(range, ""%"", """"), ""`"", """"), ""="");
                                    isString = string_ends_with(range, ""`"");
                                    isPercent = !isString && string_ends_with(range, ""%"");
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
                                var minMax = string_split(string_replace(range, ""%"", """"), ""~"");
                                var isPercent = string_ends_with(range, ""%"");
                                if (!isPercent)
                                    value = floor(value);
                                var convVal = isPercent ? value * 100 : value;
                                var convBack = isPercent ? 1 / 100 : 1;
                                if (convVal >= real(minMax[0]) || i == 0) {{
                                    value = clamp(value, real(minMax[0]) * convBack, real(minMax[1]) * convBack);
                                    break;
                                }}
                            }} else if (string_pos(""="", range)) {{
                                var labelValue = string_split(string_replace(range, ""%"", """"), ""="");
                                var isPercent = string_ends_with(range, ""%"");
                                var convBack = isPercent ? 1 / 100 : 1;
                                if (value >= (real(labelValue[1]) * convBack) || i == 0) {{
                                    value = real(labelValue[1]) * convBack;
                                    break;
                                }}
                            }} else if (string_ends_with(range, ""%"")) {{
                                var minMax = string_split(string_replace(range, ""%"", """"), ""-"");
                                if (value * 100 >= real(minMax[0]) || i == 0) {{
                                    value = clamp(value, real(minMax[0]) / 100, real(minMax[1]) / 100);
                                    break;
                                }}
                            }}
                        }}
                    }}

                    row_data.data_ref.set(value);

                    row_data.change_func();

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

// Finish edit
importGroup.Import();
ScriptMessage($"Success: Mod menu added to '{displayName}'!");
