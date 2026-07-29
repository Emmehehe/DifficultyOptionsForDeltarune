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

// The demo is on an old version of game maker that doesn't have the string_split, string_ends_with, or string_trim functions so add (very) basic implementations
string[] darkcons = {"gml_Object_obj_darkcontroller"};
if (ch_no == 0)
{
    string[] demoDarkcons = {"gml_Object_obj_darkcontroller_ch1"};
    darkcons = darkcons.Concat(demoDarkcons).ToArray();
}
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

// Add menu create code
foreach (string darkcon in darkcons)
{
    importGroup.QueueAppend(darkcon + "_Create_0", @"

        var installed_modmenu = true;

        global.modmenuno = 0;
        global.modsubmenuno = -1;
        global.modsubmenuselected = false;
        global.modsubmenuscroll = 0;
        global.modmenu_data = array_create(0);
        surf_modtitles = -1;

        // Apply acceleration to the scrollers so that they're not too fidly but not too slow
        modscroller_step = 1; // reset to 1 as first interaction should be instantaneous
        modscroller_speed_min = 0;
        modscroller_speed_max = 3;
        modscroller_speed = modscroller_speed_min;
        modscroller_accel = 1 / 20;

        // some translation mods replace the english translation rather than using DR's built in localisation support, so can't always rely on global.lang and have to override for certain mods
        global.modmenu_langoverride = """";
    ");
}

string global_lang = @"(global.modmenu_langoverride != """" ? global.modmenu_langoverride : global.lang)";
Func<string,string,string> ds_map_find_value_lang =
    (id, key) => @$"(ds_map_exists({id}, {key} + ""_"" + {global_lang}) ? ds_map_find_value({id}, {key} + ""_"" + {global_lang}) :  ds_map_find_value({id}, {key} + ""_en""))";


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

            if ({global_lang} == ""ja"")
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

            if (array_length(global.modmenu_data) > 0)
            {{
                // top row buttons
                var isSubmenu = (global.modsubmenuno >= 0);
                var isMenuLonely = array_length(global.modmenu_data) == 1;

                var allmodmenus = """";

                for (var i = global.modmenuno; i < array_length(global.modmenu_data); i++)
                {{
                    allmodmenus += string_upper({ds_map_find_value_lang("global.modmenu_data[i]", @"""title""")}) + (i + 1 < array_length(global.modmenu_data) ? ""        "" : """");
                }}

                if (!surface_exists(surf_modtitles))
                {{
                    surf_modtitles = surface_create(410, 35);
                }}
                surface_set_target(surf_modtitles);
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
                    draw_text(0, 0, string_upper({ds_map_find_value_lang("global.modmenu_data[global.modmenuno]", @"""title""")}));
                }}

                draw_sprite(spr_darkmodsfade, 0, 410 - 35, 0);

                surface_reset_target();
                draw_surface(surf_modtitles, xx + 110, yy + 110);

                if (!isSubmenu) {{
                    menusiner += 1;
                    draw_sprite_part(spr_heart_harrows, menusiner / 20, 8 - 8 * (global.modmenuno > 0), 0, 16 + 8 * (global.modmenuno > 0) + 8 * (global.modmenuno < (array_length(global.modmenu_data) - 1)), 16, xx + 85 - 8 * (global.modmenuno > 0), yy + 120);
                }}

                // form buttons
                var left_margin = {ds_map_find_value_lang("global.modmenu_data[global.modmenuno]", @"""left_margin""")};
                if (is_undefined(left_margin))
                    left_margin = 40;
                var _xPos = xx + 130 + left_margin;
                var _heartXPos = xx + 105 + left_margin;

                var left_value_pos = {ds_map_find_value_lang("global.modmenu_data[global.modmenuno]", @"""left_value_pos""")};
                if (is_undefined(left_value_pos))
                    left_value_pos = 300;
                var _selectXPos = xx + 130 + left_value_pos;

                draw_set_color(c_white);

                if (!isSubmenu)
                    draw_set_color(c_gray);

                var form_data = ds_map_find_value(global.modmenu_data[global.modmenuno], ""form"");

                var heartyprogress = 150;
                if (array_length(form_data) >= 0)
                {{
                    var i = global.modsubmenuscroll;
                    var yprogress = 150;
                    while ((yprogress <= 150 + 6 * 35) && (i < array_length(form_data) + 1))
                    {{
                        if (i >= array_length(form_data))
                        {{
                            draw_set_color(c_white);
                            draw_text(_xPos, yy + yprogress{(ch_no == 1 ? " + 1" : "")}, string_hash_to_newline({(darkcon.EndsWith("_ch1") ? ch1_back_text : back_text)})); // Back
                            if (global.modsubmenuno == i)
                                heartyprogress = yprogress;
                            yprogress += 35;
                            break;
                        }}

                        var row_data = form_data[i];
                        var row_hidden_data = ds_map_find_value(row_data, ""hidden"");
                        var row_hidden = !is_undefined(row_hidden_data) ? row_hidden_data : false;
                        if (row_hidden) {{
                            i++;
                            continue;
                        }}

                        var row_disabled_data = ds_map_find_value(row_data, ""disabled"");
                        var row_disabled = !is_undefined(row_disabled_data) ? row_disabled_data : false;
                        if (row_disabled)
                            draw_set_color(c_gray);
                        else if (global.modsubmenuselected && global.modsubmenuno == i)
                            draw_set_color(c_yellow);
                        else
                            draw_set_color(c_white);

                        var value_name = ds_map_find_value(row_data, ""value_name"");
                        var value = !is_undefined(value_name) ? variable_instance_get(global, value_name) : -1;
                        var value_range = {ds_map_find_value_lang("row_data", @"""value_range""")};
                        var ranges = !is_undefined(value_range) ? string_split(value_range, "";"") : [];
                        var valueString = """";
                        var isCategory = is_undefined(value_range) && is_undefined(ds_map_find_value(row_data, ""func_name""));

                        draw_text_transformed(_xPos - (isCategory * 28), yy + yprogress - (isCategory * 5){(ch_no == 1 ? " + 1" : "")}, string_hash_to_newline({ds_map_find_value_lang("row_data", @"""title""")}), (isCategory ? 0.5 : 1), (isCategory ? 0.5 : 1), 0);
                        if (isCategory){{
                            draw_line(_xPos - 28 - 3, yy + yprogress + 9, _xPos + 400, yy + yprogress + 9);
                        }}

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

                        draw_text(_selectXPos, yy + yprogress{(ch_no == 1 ? " + 1" : "")}, string_hash_to_newline(valueString));

                        if (global.modsubmenuno == i){{
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
                            if (global.modsubmenuscroll == i){{
                                scrollprogress = totalmenulength;
                            }}
                            totalmenulength += 35;
                            continue;
                        }}

                        if (global.modsubmenuscroll == i){{
                            scrollprogress = totalmenulength;
                        }}
                        var isCategory = is_undefined({ds_map_find_value_lang("form_data[i]", @"""value_range""")}) && is_undefined(ds_map_find_value(form_data[i], ""func_name""));
                        totalmenulength += (isCategory ? 12 : 35);
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
                            var isCategory = is_undefined({ds_map_find_value_lang("form_data[i]", @"""value_range""")}) && is_undefined(ds_map_find_value(form_data[i], ""func_name""));
                            newlastscreenlength += (isCategory ? 12 : 35);
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

                        if (global.modsubmenuscroll > 0)
                            draw_sprite_ext(spr_morearrow, 0, xx + 81, (yy + modscrollbary) - 10 - (sin(cur_jewel / 12) * 3), 1, -1, 0, c_white, 1);

                        if ((global.modsubmenuscroll + 7) < (array_length(form_data) + 1))
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
        if (global.modmenu_langoverride != ""es"" && global.lang == ""en"" && variable_instance_exists(global, ""esp_names""))
        {{
            global.modmenu_langoverride = ""es"";
        }}

        function scrolldownforcontent()
        {{
            var form_data = ds_map_find_value(global.modmenu_data[global.modmenuno], ""form"");
            global.modsubmenuscroll = global.modsubmenuno + 1;
            var menuscreenlength = 7 * 35;
            var lastscreenlength = 0;
            for (var i = global.modsubmenuno; i >= 0; i--) {{
                var newlastscreenlength = lastscreenlength;
                if (i >= array_length(form_data))
                {{
                    newlastscreenlength += 35;
                }}
                else
                {{
                    var isCategory = is_undefined({ds_map_find_value_lang("form_data[i]", @"""value_range""")}) && is_undefined(ds_map_find_value(form_data[i], ""func_name""));
                    newlastscreenlength += (isCategory ? 12 : 35);
                }}

                if (newlastscreenlength > menuscreenlength)
                {{
                    break;
                }}
                lastscreenlength = newlastscreenlength;
                global.modsubmenuscroll--;
            }}
        }}

        function isneedscrolldown()
        {{
            var form_data = ds_map_find_value(global.modmenu_data[global.modmenuno], ""form"");
            var menuscreenlength = 7 * 35;
            var currentscreenlength = 0;
            var foundselected = false;
            for (var i = global.modsubmenuscroll; i < array_length(form_data) + 1; i++) {{
                var newcurrentscreenlength = currentscreenlength;
                if (i >= array_length(form_data))
                {{
                    newcurrentscreenlength += 35;
                }}
                else
                {{
                    var isCategory = is_undefined({ds_map_find_value_lang("form_data[i]", @"""value_range""")}) && is_undefined(ds_map_find_value(form_data[i], ""func_name""));
                    newcurrentscreenlength += (isCategory ? 12 : 35);
                }}

                if (newcurrentscreenlength > menuscreenlength)
                {{
                    break;
                }}
                currentscreenlength = newcurrentscreenlength;
                if (i == global.modsubmenuno)
                {{
                    foundselected = true;
                }}
            }}
            return !foundselected;
        }}

        function modsubmenu_up(arg0)
        {{
            global.modsubmenuno--;

            if (global.modsubmenuno < 0)
            {{
                global.modsubmenuno = arg0 - 1;

                scrolldownforcontent();
            }}
            else
            {{
                if (global.modsubmenuno < global.modsubmenuscroll)
                    global.modsubmenuscroll = global.modsubmenuno;
            }}
        }}

        function modsubmenu_down(arg0)
        {{
            global.modsubmenuno++;

            if (global.modsubmenuno >= arg0)
            {{
                global.modsubmenuno = 0;
                global.modsubmenuscroll = 0;
            }}
            else if (isneedscrolldown())
            {{
                scrolldownforcontent();
            }}
        }}

        function issubmenucategory(arg0, arg1)
        {{
            if (global.modsubmenuno >= (arg0 - 1))
                return false;

            return
                is_undefined({ds_map_find_value_lang("arg1[global.modsubmenuno]", @"""value_range""")}) && is_undefined(ds_map_find_value(arg1[global.modsubmenuno], ""func_name""))
        }}

        function ishidden(arg0, arg1)
        {{
            if (global.modsubmenuno >= (arg0 - 1))
                return false;

            var row_hidden_data = ds_map_find_value(arg1[global.modsubmenuno], ""hidden"");
            var row_hidden = !is_undefined(row_hidden_data) ? row_hidden_data : false;
            if  (row_hidden)
                return true;

            return false;
        }}

        function isdisabled(arg0, arg1)
        {{
            if (global.modsubmenuno >= (arg0 - 1))
                return false;

            var row_disabled_data = ds_map_find_value(arg1[global.modsubmenuno], ""disabled"");
            var row_disabled = !is_undefined(row_disabled_data) ? row_disabled_data : false;
            if  (row_disabled)
                return true;

            return false;
        }}

        function shouldskiprow(arg0, arg1)
        {{
            return issubmenucategory(arg0, arg1) || ishidden(arg0, arg1);
        }}

        if (global.menuno == 6)
        {{
            var isSubmenu = (global.modsubmenuno >= 0);

            if (!isSubmenu) {{
                // enter submenu right away if there is only one submenu
                if (array_length(global.modmenu_data) == 1)
                    global.modsubmenuno = 0;

                if (array_length(global.modmenu_data) > 0)
                {{
                    if (left_p())
                    {{
                        movenoise = 1;

                        global.modmenuno--;
                        if (global.modmenuno < 0)
                            global.modmenuno = array_length(global.modmenu_data) - 1;
                    }}
                    if (right_p())
                    {{
                        movenoise = 1;

                        global.modmenuno++;
                        if (global.modmenuno >= array_length(global.modmenu_data))
                            global.modmenuno = 0;
                    }}
                    if (button1_p() && onebuffer < 0 && twobuffer < 0)
                    {{
                        onebuffer = 2;
                        selectnoise = 1;
                        global.modsubmenuno = 0;

                        // make sure category header or hidden/disabled row isn't selected
                        var form_data = ds_map_find_value(global.modmenu_data[global.modmenuno], ""form"");
                        var form_length = ds_map_exists(global.modmenu_data[global.modmenuno], ""form"") ? array_length(form_data) : 0;
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
            }} else if (!global.modsubmenuselected) {{
                var form_data = ds_map_find_value(global.modmenu_data[global.modmenuno], ""form"");
                var form_length = ds_map_exists(global.modmenu_data[global.modmenuno], ""form"") ? array_length(form_data) : 0;

                if (form_length <= 0) {{
                    global.modsubmenuno = -1;
                    global.modsubmenuscroll = 0;
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

                    if (global.modsubmenuno >= array_length(form_data)) {{
                        global.modsubmenuno = -1;
                        global.modsubmenuscroll = 0;

                        if (array_length(global.modmenu_data) == 1)
                        {{
                            global.menuno = 0;
                            global.submenu = 0;
                        }}

                        var on_close = ds_map_find_value(global.modmenu_data[global.modmenuno], ""on_close"");
                        if (!is_undefined(on_close))
                        {{
                            var functocall = variable_instance_get(global, on_close);
                            functocall();
                        }}
                    }}
                    else
                    {{
                        global.modsubmenuselected = true;

                        // if range is only labels just cycle through them
                        var row_data = form_data[global.modsubmenuno];
                        var value_range = {ds_map_find_value_lang("row_data", @"""value_range""")};
                        var ranges = !is_undefined(value_range) ? string_split(value_range, "";"") : [];
                        var force_scroll = ds_map_exists(row_data, ""force_scroll"") ? ds_map_find_value(row_data, ""force_scroll"") : false;
                        var doToggle = !force_scroll;

                        if (doToggle) {{
                            for (var i = 0; i < array_length(ranges); i++) {{
                                var range = ranges[i];
                                if (!string_pos(""="", range)) {{
                                    doToggle = false;
                                    break;
                                }}
                            }}
                        }}

                        if (doToggle || array_length(ranges) <= 0) {{
                            global.modsubmenuselected = false;
                        }}

                        if (doToggle && array_length(ranges) > 0) {{
                            var value = variable_instance_get(global, ds_map_find_value(row_data, ""value_name""));

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

                            variable_instance_set(global, ds_map_find_value(row_data, ""value_name""), value);

                            var on_change = ds_map_find_value(row_data, ""on_change"");
                            if (!is_undefined(on_change))
                            {{
                                var functocall = variable_instance_get(global, on_change);
                                functocall();
                            }}
                        }}


                        if (doToggle || array_length(ranges) <= 0) {{
                            var func_name = ds_map_find_value(row_data, ""func_name"");
                            if (!is_undefined(func_name))
                            {{
                                var functocall = variable_instance_get(global, func_name);
                                functocall(true);
                            }}
                        }}
                    }}
                }}
                if (button2_p() && onebuffer < 0 && twobuffer < 0)
                {{
                    cancelnoise = 1;
                    twobuffer = 2;
                    global.modsubmenuno = -1;
                    global.modsubmenuscroll = 0;

                    if (array_length(global.modmenu_data) == 1)
                    {{
                        global.menuno = 0;
                        global.submenu = 0;
                    }}

                    var on_close = ds_map_find_value(global.modmenu_data[global.modmenuno], ""on_close"");
                    if (!is_undefined(on_close))
                    {{
                        var functocall = variable_instance_get(global, on_close);
                        functocall();
                    }}
                }}
            }} else {{
                var form_data = ds_map_find_value(global.modmenu_data[global.modmenuno], ""form"");
                var row_data = form_data[global.modsubmenuno];
                var value_range = {ds_map_find_value_lang("row_data", @"""value_range""")};
                var ranges = !is_undefined(value_range) ? string_split(value_range, "";"") : [];
                var value_name = ds_map_find_value(row_data, ""value_name"");
                var value = !is_undefined(value_name) ? variable_instance_get(global, value_name) : -1;

                var scroll_todo = modscroller_step div 1;

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

                    variable_instance_set(global, ds_map_find_value(row_data, ""value_name""), value);

                    var on_change = ds_map_find_value(row_data, ""on_change"");
                    if (!is_undefined(on_change))
                    {{
                        var functocall = variable_instance_get(global, on_change);
                        functocall();
                    }}

                    modscroller_step = modscroller_step % 1;
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

                        var scroll_todo = modscroller_step div 1;
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

                    variable_instance_set(global, ds_map_find_value(row_data, ""value_name""), value);

                    var on_change = ds_map_find_value(row_data, ""on_change"");
                    if (!is_undefined(on_change))
                    {{
                        var functocall = variable_instance_get(global, on_change);
                        functocall();
                    }}

                    modscroller_step = modscroller_step % 1;
                }}

                if (right_h() || left_h())
                {{
                    modscroller_step += modscroller_speed;
                    modscroller_speed = clamp(modscroller_speed + modscroller_accel, modscroller_speed_min, modscroller_speed_max);
                }}
                else
                {{
                    modscroller_step = 1; // reset to 1 as first interaction should be instantaneous
                    modscroller_speed = modscroller_speed_min;
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
                    global.modsubmenuselected = false;

                    var func_name = ds_map_find_value(row_data, ""func_name"");
                    if (!is_undefined(func_name))
                    {{
                        var functocall = variable_instance_get(global, func_name);
                        functocall(se_select);
                    }}

                    modscroller_step = 1; // reset to 1 as first interaction should be instantaneous
                    modscroller_speed = modscroller_speed_min;
                }}
            }}
        }}
    ");
}

// Finish edit
importGroup.Import();
ScriptMessage($"Success: Mod menu added to '{displayName}'!");
