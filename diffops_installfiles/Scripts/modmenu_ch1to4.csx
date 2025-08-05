using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using UndertaleModLib.Util;

// Prefire checks
EnsureDataLoaded();
const string expectedDisplayName = "DELTARUNE Chapter [1-4]";
var displayName = Data?.GeneralInfo?.DisplayName?.Content;
if (!Regex.IsMatch(displayName, expectedDisplayName, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500)))
{
    ScriptError($"Error 0: data file display name does not match expected: '{expectedDisplayName}', actual display name: '{displayName}'.");
    return;
}

// Load texture file
Dictionary<string, UndertaleEmbeddedTexture> textures = new Dictionary<string, UndertaleEmbeddedTexture>();

UndertaleEmbeddedTexture modsmenuTexturePage = new UndertaleEmbeddedTexture();
modsmenuTexturePage.TextureData.Image = GMImage.FromPng(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(ScriptPath), "modsmenu.png")));
Data.EmbeddedTextures.Add(modsmenuTexturePage);
textures.Add(Path.GetFileName(Path.Combine(Path.GetDirectoryName(ScriptPath), "modsmenu.png")), modsmenuTexturePage);

UndertaleTexturePageItem AddNewTexturePageItem(ushort sourceX, ushort sourceY, ushort sourceWidth, ushort sourceHeight)
{
    ushort targetX = 0;
    ushort targetY = 0;
    ushort targetWidth = sourceWidth;
    ushort targetHeight = sourceHeight;
    ushort boundingWidth = sourceWidth;
    ushort boundingHeight = sourceHeight;
    var texturePage = textures["modsmenu.png"];

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

// add mods button
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

// add mods menu description
{
    UndertaleSprite spr_darkmenudesc = Data.Sprites.ByName("spr_darkmenudesc");
    spr_darkmenudesc.Textures.Add(new UndertaleSprite.TextureEntry() { Texture = pg_modsdesc });
}

// Begin edit
ScriptMessage($"Adding mod menu to '{displayName}'...");
UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data){
    ThrowOnNoOpFindReplace = true
};

// Add menu create code
importGroup.QueueAppend("gml_Object_obj_darkcontroller_Create_0", @"
    
    global.modsmenuno = 0;
    global.modssubmenuno = -1;
    global.modssubmenuselected = false;
    global.modsmenu_data = array_create(0);
");

// Add menu draw code
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_darkcontroller_Draw_0", "msprite[4] = spr_darkconfigbt;", @"
    msprite[4] = spr_darkconfigbt;
    msprite[5] = spr_darkmodsbt;
    ");
importGroup.QueueFindReplace("gml_Object_obj_darkcontroller_Draw_0", "i = 0; i < 5; i += 1)", "i = 0; i < (array_length(global.modsmenu_data) > 0 ? 6 : 5); i += 1)");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_darkcontroller_Draw_0", "spritemx = -100;", "spritemx = (array_length(global.modsmenu_data) > 0 ? -80 : -100);");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_darkcontroller_Draw_0",
    "draw_sprite_ext(msprite[i], off, xx + 120 + (i * 100) + spritemx, (yy + tp) - 60, 2, 2, 0, c_white, 1);",
    "draw_sprite_ext(msprite[i], off, xx + (array_length(global.modsmenu_data) > 0 ? (110 + (i * 80)) : (120 + (i * 100))) + spritemx, (yy + tp) - 60, 2, 2, 0, c_white, 1);");
importGroup.QueueAppend("gml_Object_obj_darkcontroller_Draw_0", @"
    if (global.menuno == 6)
    {
        draw_set_color(c_black);
        
        if (global.lang == ""ja"")
        {
            draw_rectangle(xx + 60, yy + 85, xx + 580, yy + 412, false);
            scr_darkbox(xx + 50, yy + 75, xx + 590, yy + 422);
        }
        else
        {
            draw_rectangle(xx + 60, yy + 90, xx + 580, yy + 410, false);
            scr_darkbox(xx + 50, yy + 80, xx + 590, yy + 420);
        }

        // top row buttons
        var isSubmenu = (global.modssubmenuno >= 0);
        var isMenuLonely = array_length(global.modsmenu_data) == 1;

        var startPadding = 0;
        if (!isMenuLonely)
        {
            var xAcc = 0;
            for (var i = 0; i < array_length(global.modsmenu_data); i++)
            {
                var menu_data = global.modsmenu_data[i];
                xAcc += ds_map_find_value(menu_data, ""title_size_en"") + 25;
            }
            if (xAcc <= 410)
                startPadding = (410 - xAcc - 45) / 2;
        }
        else
            startPadding = (410 - ds_map_find_value(global.modsmenu_data[0], ""title_size_en"")) / 2;
        
        draw_set_color(c_white);
        var xAcc = 0;
        var xSelAcc = 0;
        var isHitMenuNo = false;
        
        for (var i = 0; i < array_length(global.modsmenu_data); i++)
        {
            var menu_data = global.modsmenu_data[i];
            if (isSubmenu)
            {
                if (isMenuLonely)
                    draw_set_color(c_white);
                else if (global.modsmenuno == i)
                    draw_set_color(c_orange);
                else
                    draw_set_color(c_gray);
            }
        
            draw_text(xx + 110 + startPadding + xAcc, yy + 100 + !isMenuLonely * 10, string_hash_to_newline(string_upper(ds_map_find_value(menu_data, ""title_en""))));
            xAcc += ds_map_find_value(menu_data, ""title_size_en"") + 45;
            if (!isHitMenuNo && !isSubmenu) {
                if (global.modsmenuno == i)
                    isHitMenuNo = true;
                else
                    xSelAcc += ds_map_find_value(menu_data, ""title_size_en"") + 45;
            }
        }

        if (!isSubmenu)
            draw_sprite(spr_heart, 0, xx + 85 + startPadding + xSelAcc, yy + 120);

        // form buttons
        var _xPos = (global.lang == ""en"") ? (xx + 170) : (xx + 150);
        var _heartXPos = (global.lang == ""en"") ? (xx + 145) : (xx + 125);
        var _selectXPos = (global.lang == ""ja"" && global.is_console) ? (xx + 385) : (xx + 430);

        draw_set_color(c_white);

        if (!isSubmenu)
            draw_set_color(c_gray);

        var form_data = ds_map_find_value(global.modsmenu_data[global.modsmenuno], ""form"");
        for (var i = 0; i < array_length(form_data); i++)
        {
            if (global.modssubmenuselected && global.modssubmenuno == i)
                draw_set_color(c_yellow);
            else
                draw_set_color(c_white);

            var row_data = form_data[i];
            draw_text(_xPos, yy + 150 + i * 35, string_hash_to_newline(ds_map_find_value(row_data, ""title_en"")));

            var value = variable_instance_get(global, ds_map_find_value(row_data, ""value_name""));
            var ranges = string_split(ds_map_find_value(row_data, ""value_range""), "";"");
            var valueString = """";

            for (var j = 0; j < array_length(ranges); j++) {
                var range = ranges[j];
                if (string_ends_with(range, ""%"")) {
                    var minMax = string_split(string_replace(range, ""%"", """"), ""-"");
                    if (value * 100 <= minMax[1] || j+1 == array_length(ranges)) {
                        valueString = string_trim(string_format(value * 100, 3, value < 0.2 ? 1 : 0) + ""%"");
                        break;
                    }
                } else if (string_pos(""="", range)) {
                    var labelValue = string_split(range, ""="");
                    if (value == real(labelValue[1]) || j+1 == array_length(ranges)) {
                        valueString = labelValue[0];
                        break;
                    }
                }
            }

            draw_text(_selectXPos, yy + 150 + i * 35, string_hash_to_newline(valueString));
        }

        draw_set_color(c_white);
        if (array_length(form_data) < 7)
            draw_text(_xPos, yy + 150 + array_length(form_data) * 35, string_hash_to_newline(scr_84_get_lang_string(""obj_darkcontroller_slash_Draw_0_gml_96_0""))); // Back

        if (isSubmenu)
            draw_sprite(spr_heart, 0, _heartXPos, yy + 160 + (global.modssubmenuno * 35));
    }
");

// Add menu step code
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_darkcontroller_Step_0", "global.menucoord[0] = 4;", "global.menucoord[0] = array_length(global.modsmenu_data) <= 0 ? 4 : 5;");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_darkcontroller_Step_0", "if (global.menucoord[0] == 4)", "if (global.menucoord[0] == (array_length(global.modsmenu_data) <= 0 ? 4 : 5))");
importGroup.QueueAppend("gml_Object_obj_darkcontroller_Step_0", @"
    if (global.menuno == 6)
    {
        var isSubmenu = (global.modssubmenuno >= 0);

        if (!isSubmenu) {
            if (array_length(global.modsmenu_data) == 1)
                global.modssubmenuno = 0;

            if (left_p())
            {
                movenoise = 1;

                global.modsmenuno--;
                if (global.modsmenuno < 0)
                    global.modsmenuno = array_length(global.modsmenu_data) - 1;
            }
            if (right_p())
            {
                movenoise = 1;

                global.modsmenuno++;
                if (global.modsmenuno >= array_length(global.modsmenu_data))
                    global.modsmenuno = 0;
            }
            if (button1_p() && onebuffer < 0 && twobuffer < 0)
            {
                onebuffer = 2;
                selectnoise = 1;
                global.modssubmenuno = 0;
            }
            if (button2_p() && onebuffer < 0 && twobuffer < 0)
            {
                cancelnoise = 1;
                twobuffer = 2;
                global.menuno = 0;
                global.submenu = 0;
            }
        } else if (!global.modssubmenuselected) {
            var form_data = ds_map_find_value(global.modsmenu_data[global.modsmenuno], ""form"");
            var form_length = ds_map_exists(global.modsmenu_data[global.modsmenuno], ""form"") ? array_length(form_data) : 0;
            // back button
            if (form_length > 0 && form_length < 7)
                form_length++;

            if (form_length <= 0) {
                global.modssubmenuno = -1;
            }

            if (up_p())
            {
                movenoise = 1;

                global.modssubmenuno--;
                if (global.modssubmenuno < 0)
                    global.modssubmenuno = form_length - 1;
            }
            if (down_p())
            {
                movenoise = 1;

                global.modssubmenuno++;
                if (global.modssubmenuno >= form_length)
                    global.modssubmenuno = 0;
            }
            if (button1_p() && onebuffer < 0 && twobuffer < 0)
            {
                onebuffer = 2;
                selectnoise = 1;

                if (global.modssubmenuno >= array_length(form_data)) {
                    global.modssubmenuno = -1;
                    
                    if (array_length(global.modsmenu_data) == 1)
                    {
                        global.menuno = 0;
                        global.submenu = 0;
                    }
                }
                else
                {
                    global.modssubmenuselected = true;

                    // if range is only labels just cycle through them
                    var row_data = form_data[global.modssubmenuno];
                    var value_range = ds_map_find_value(row_data, ""value_range"");
                    var ranges = string_split(ds_map_find_value(row_data, ""value_range""), "";"");
                    var isAllLabels = true;

                    for (var i = 0; i < array_length(ranges); i++) {
                        var range = ranges[i];
                        if (string_ends_with(range, ""%"")) {
                            isAllLabels = false;
                            break;
                        }
                    }

                    if (isAllLabels) {
                        global.modssubmenuselected = false;

                        for (var i = 0; i < array_length(ranges); i++) {
                            var range = ranges[i];
                            if (string_pos(""="", range)) {
                                var labelValue = string_split(range, ""="");
                                if (value < real(labelValue[1])) {
                                    value = real(labelValue[1]);
                                    break;
                                } else if (i+1 == array_length(ranges)) {
                                    range = ranges[0];
                                    labelValue = string_split(range, ""="");
                                    value = real(labelValue[1]);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (button2_p() && onebuffer < 0 && twobuffer < 0)
            {
                cancelnoise = 1;
                twobuffer = 2;
                global.modssubmenuno = -1;

                if (array_length(global.modsmenu_data) == 1)
                {
                    global.menuno = 0;
                    global.submenu = 0;
                }
            }
        } else {
            var form_data = ds_map_find_value(global.modsmenu_data[global.modsmenuno], ""form"");
            var row_data = form_data[global.modssubmenuno];
            var value_range = ds_map_find_value(row_data, ""value_range"");
            var value_name = ds_map_find_value(row_data, ""value_name"");
            var value = variable_instance_get(global, ds_map_find_value(row_data, ""value_name""));
            
            if (right_h())
            {
                if (value < 0.2)
                    value += 0.005;
                else if (value < 0.5)
                    value += 0.01;
                else if (value < 1)
                    value += 0.02;
                else if (value < 2)
                    value += 0.05;
                else
                    value += 0.1;

                var ranges = string_split(ds_map_find_value(row_data, ""value_range""), "";"");

                for (var i = 0; i < array_length(ranges); i++) {
                    var range = ranges[i];
                    if (string_ends_with(range, ""%"")) {
                        var minMax = string_split(string_replace(range, ""%"", """"), ""-"");
                        if (value * 100 <= minMax[1] || i+1 == array_length(ranges)) {
                            value = clamp(value, minMax[0] / 100, minMax[1] / 100);
                            break;
                        }
                    } else if (string_pos(""="", range)) {
                        var labelValue = string_split(range, ""="");
                        if (value <= real(labelValue[1]) || i+1 == array_length(ranges)) {
                            value = real(labelValue[1]);
                            break;
                        }
                    }
                }

                variable_instance_set(global, ds_map_find_value(row_data, ""value_name""), value);
            }
            
            if (left_h())
            {
                if (value <= 0.2)
                    value -= 0.005;
                else if (value <= 0.5)
                    value -= 0.01;
                else if (value <= 1)
                    value -= 0.02;
                else if (value <= 2)
                    value -= 0.05;
                else
                    value -= 0.1;

                var ranges = string_split(ds_map_find_value(row_data, ""value_range""), "";"");

                for (var i = array_length(ranges) - 1; i >= 0; i--) {
                    var range = ranges[i];
                    if (string_ends_with(range, ""%"")) {
                        var minMax = string_split(string_replace(range, ""%"", """"), ""-"");
                        if (value * 100 >= minMax[0] || i == 0) {
                            value = clamp(value, minMax[0] / 100, minMax[1] / 100);
                            break;
                        }
                    } else if (string_pos(""="", range)) {
                        var labelValue = string_split(range, ""="");
                        if (value >= real(labelValue[1]) || i == 0) {
                            value = real(labelValue[1]);
                            break;
                        }
                    }
                }

                variable_instance_set(global, ds_map_find_value(row_data, ""value_name""), value);
            }
            
            se_select = 0;

            if (button1_p() && onebuffer < 0)
                se_select = 1;
            
            if (button2_p() && twobuffer < 0)
                se_select = 1;
            
            if (se_select == 1)
            {
                selectnoise = 1;
                onebuffer = 2;
                twobuffer = 2;
                global.modssubmenuselected = false;
            }
        }
        
    }
");

// Finish edit
importGroup.Import();
ScriptMessage($"Success: Mod menu added to '{displayName}'!");