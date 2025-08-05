using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

// Prefire checks
EnsureDataLoaded();
const string expectedDisplayName = "DELTARUNE Chapter 2";
var displayName = Data?.GeneralInfo?.DisplayName?.Content;
if (displayName.ToLower() != expectedDisplayName.ToLower())
{
    ScriptError($"Error 0: data file display name does not match expected: '{expectedDisplayName}', actual display name: '{displayName}'.");
    return;
}

// Begin edit
ScriptMessage($"Adding difficulty options to '{displayName}'...");
UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data){
    ThrowOnNoOpFindReplace = true
};

// Add globals
importGroup.QueueRegexFindReplace("gml_GlobalScript_scr_gamestart", "function scr_gamestart\\(\\)\\s*{", @"
    function scr_gamestart()
    {
        global.diffdmgmulti = 1;
        global.diffdwnpenalty = 1 / 2;
        global.diffvictoryres = 1 / 8;

    ");
// Load globals from config
importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_load", "ossafe_file_text_close(myfileid);", @"
    ossafe_file_text_close(myfileid);

    ossafe_ini_open(""difficulty_"" + string(global.filechoice) + "".ini"");
    global.diffdmgmulti = ini_read_real(""DIFFICULTY"", ""DAMAGE_MULTIPLIER"", 1);
    global.diffdwnpenalty = ini_read_real(""DIFFICULTY"", ""DOWN_PENALTY"", 1 / 2);
    global.diffvictoryres = ini_read_real(""DIFFICULTY"", ""VICTORY_RES"", 1 / 8);
    ossafe_ini_close();

    ");
// Save globals to config
importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_saveprocess", "ossafe_file_text_close(myfileid);", @"
    ossafe_file_text_close(myfileid);

    ossafe_ini_open(""difficulty_"" + string(global.filechoice) + "".ini"");
    ini_write_real(""DIFFICULTY"", ""DAMAGE_MULTIPLIER"", global.diffdmgmulti);
    ini_write_real(""DIFFICULTY"", ""DOWN_PENALTY"", global.diffdwnpenalty);
    ini_write_real(""DIFFICULTY"", ""VICTORY_RES"", global.diffvictoryres);
    ossafe_ini_close();
    ");

// Add mod menu
importGroup.QueueAppend("gml_Object_obj_darkcontroller_Create_0", @"
    
    if (!variable_instance_exists(global, ""modsmenu_data""))
        global.modsmenu_data = array_create(0);

    var menudata = ds_map_create();
    ds_map_add(menudata, ""title_en"", ""Difficulty"");
    ds_map_add(menudata, ""title_size_en"", 138);

    var formdata = array_create(0);

    var rowdata = ds_map_create();
    ds_map_add(rowdata, ""title_en"", ""Damage Multi"");
    ds_map_add(rowdata, ""value_range"", ""0-1000%;INF=2147483647"");
    ds_map_add(rowdata, ""value_name"", ""diffdmgmulti"");
    array_push(formdata, rowdata);

    var rowdata = ds_map_create();
    ds_map_add(rowdata, ""title_en"", ""Down Deficit"");
    ds_map_add(rowdata, ""value_range"", ""0-500%"");
    ds_map_add(rowdata, ""value_name"", ""diffdwnpenalty"");
    array_push(formdata, rowdata);

    var rowdata = ds_map_create();
    ds_map_add(rowdata, ""title_en"", ""Victory Res"");
    ds_map_add(rowdata, ""value_range"", ""OFF=-1;0-100%"");
    ds_map_add(rowdata, ""value_name"", ""diffvictoryres"");
    array_push(formdata, rowdata);

    ds_map_add(menudata, ""form"", formdata);

    array_push(global.modsmenu_data, menudata);
");

string[] damageLikes = {"gml_GlobalScript_scr_damage", "gml_GlobalScript_scr_damage_proportional", "gml_GlobalScript_scr_damage_sneo_final_attack"};

// Apply damage multiplier
foreach (string scrName in damageLikes)
{   
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", "hpdiff = tdamage;", @"
        origdamage = tdamage;
        tdamage = ceil(tdamage * global.diffdmgmulti);
        hpdiff = tdamage;
        ");
    importGroup.QueueRegexFindReplace("gml_GlobalScript_scr_damage", "if \\(target == 3\\)\\s*{", @"
        if (target == 3)
        {
            tdamage = origdamage;
        ");
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", "if (global.charaction[hpi] == 10)", @"
        tdamage = ceil(tdamage * global.diffdmgmulti);

        if (global.charaction[hpi] == 10)
        ");
}
importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage_all_overworld", "hpdiff = tdamage;", @"
    tdamage = ceil(tdamage * global.diffdmgmulti);
    hpdiff = tdamage;
    ");
importGroup.QueueFindReplace("gml_Object_o_boxingcontroller_Collision_o_boxing_hitbox", "if (global.hp[1] <= 10)", "if (global.hp[1] <= round(10 * global.diffdmgmulti))");
importGroup.QueueFindReplace("gml_Object_o_boxingcontroller_Collision_o_boxing_hitbox", "global.hp[1] -= 10;", "global.hp[1] -= round(10 * global.diffdmgmulti);");
importGroup.QueueFindReplace("gml_Object_o_boxingcontroller_Collision_o_boxing_hitbox", "global.hp[1] / 2", "global.hp[1] / power(2, 1 / global.diffdmgmulti)");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_o_boxingcontroller_Collision_o_boxing_hitbox", "global.hp[1] -= final_damage_amount;", 
    "global.hp[1] -= final_damage_amount * global.diffdmgmulti;");
// Apply damage multiplier (Damage Over Time)
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_battlecontroller_Step_0", "t_siner++;", "");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_battlecontroller_Step_0", "if (global.charweapon[4] == 13)", @"
    if (global.charweapon[4] == 13)
    {
        if (global.hp[4] > round(global.maxhp[4] / 3))
            global.hp[4] -= floor(t_siner / 6);
        
        t_siner = t_siner % 6;
        t_siner += global.diffdmgmulti;
    }

    if (false)
");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_heroparent_Draw_0", "poisontimer++;", @"
    poisontimer++;
    poisondmgtimer += global.diffdmgmulti;
");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_heroparent_Draw_0", "global.hp[global.char[myself]]--;", 
    "global.hp[global.char[myself]] = max(1, global.hp[global.char[myself]] - floor(poisondmgtimer / 10));");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_heroparent_Draw_0", "poisonamount = 0;", @"
    poisonamount = 0;
    poisondmgtimer = 0;
");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_heroparent_Draw_0", "poisontimer = 0;", @"
    poisontimer = 0;
    poisondmgtimer = poisondmgtimer % 10;
");

// Apply down penalty
foreach (string scrName in damageLikes)
{   
    importGroup.QueueFindReplace(scrName, "global.maxhp[chartarget] / 2", "global.maxhp[chartarget] * global.diffdwnpenalty");
    importGroup.QueueFindReplace(scrName, "global.maxhp[0] / 2", "global.maxhp[0] * global.diffdwnpenalty");
}

// Apply victory res - if VictoryRes is 0 then don't heal; additionally ensure the heal brings the character to at least 1 hp for low values of VictoryRes
importGroup.QueueFindReplace("gml_Object_obj_battlecontroller_Step_0", "global.maxhp[i] / 8", "global.diffvictoryres >= 0 ? max(1, global.maxhp[i] * global.diffvictoryres) : global.hp[i]");

// Finish edit
importGroup.Import();
ScriptMessage($"Success: Difficulty options added to '{displayName}'!");