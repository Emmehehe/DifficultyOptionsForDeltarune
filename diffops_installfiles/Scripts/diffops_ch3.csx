using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

// Prefire checks
EnsureDataLoaded();
const string expectedDisplayName = "DELTARUNE Chapter 3";
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
        global.diffbatboarddmgmulti = -1;

    ");
// Load globals from config
importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_load", "ossafe_file_text_close(myfileid);", @"
    ossafe_file_text_close(myfileid);

    ossafe_ini_open(""difficulty_"" + string(global.filechoice) + "".ini"");
    global.diffdmgmulti = ini_read_real(""DIFFICULTY"", ""DAMAGE_MULTIPLIER"", 1);
    global.diffdwnpenalty = ini_read_real(""DIFFICULTY"", ""DOWN_PENALTY"", 1 / 2);
    global.diffvictoryres = ini_read_real(""DIFFICULTY"", ""VICTORY_RES"", 1 / 8);
    global.diffbatboarddmgmulti = ini_read_real(""DIFFICULTY"", ""BATTLEBOARD_DAMAGE_MULTIPLIER"", -1);
    ossafe_ini_close();

    ");
// Save globals to config
importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_saveprocess", "ossafe_file_text_close(myfileid);", @"
    ossafe_file_text_close(myfileid);

    ossafe_ini_open(""difficulty_"" + string(global.filechoice) + "".ini"");
    ini_write_real(""DIFFICULTY"", ""DAMAGE_MULTIPLIER"", global.diffdmgmulti);
    ini_write_real(""DIFFICULTY"", ""DOWN_PENALTY"", global.diffdwnpenalty);
    ini_write_real(""DIFFICULTY"", ""VICTORY_RES"", global.diffvictoryres);
    ini_write_real(""DIFFICULTY"", ""BATTLEBOARD_DAMAGE_MULTIPLIER"", global.diffbatboarddmgmulti);
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

    var rowdata = ds_map_create();
    ds_map_add(rowdata, ""title_en"", ""Gameboard Dmg Multi"");
    ds_map_add(rowdata, ""value_range"", ""INHERIT=-1;0-1000%;INF=2147483647"");
    ds_map_add(rowdata, ""value_name"", ""diffbatboarddmgmulti"");
    array_push(formdata, rowdata);

    ds_map_add(menudata, ""form"", formdata);

    array_push(global.modsmenu_data, menudata);
");

string[] damageLikes = {"gml_GlobalScript_scr_damage", "gml_GlobalScript_scr_damage_proportional", "gml_GlobalScript_scr_damage_fixed",
    "gml_GlobalScript_scr_damage_maxhp", "gml_GlobalScript_scr_damage_sneo_final_attack"};

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
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_quizsequence_Other_13", "var _damage = irandom_range(30, 38);",
    "var _damage = ceil(irandom_range(30, 38) * global.diffdmgmulti);");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_ch3_b4_chef_kris_Create_0", "global.hp[1] = clamp(global.hp[1] - damage_amount, 1, global.maxhp[1]);", @"
    damage_amount = ceil(damage_amount * global.diffdmgmulti);

    global.hp[1] = clamp(global.hp[1] - damage_amount, 1, global.maxhp[1]);
");
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
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_heroparent_Step_0", "poisontimer++;", @"
    poisontimer++;
    poisondmgtimer += global.diffdmgmulti;
");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_heroparent_Step_0", "global.hp[global.char[myself]]--;", 
    "global.hp[global.char[myself]] = max(1, global.hp[global.char[myself]] - floor(poisondmgtimer / 10));");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_heroparent_Step_0", "poisonamount = 0;", @"
    poisonamount = 0;
    poisondmgtimer = 0;
");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_heroparent_Step_0", "poisontimer = 0;", @"
    poisontimer = 0;
    poisondmgtimer = poisondmgtimer % 10;
");

// Apply BattleBoard damage multiplier
const string batboarddmgmulti = "(global.diffbatboarddmgmulti < 0 ? global.diffdmgmulti : global.diffbatboarddmgmulti)";
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_board_puzzlebombbullet_Step_0", "myhealth -= other.damage;",
    $"myhealth -= other.damage * {batboarddmgmulti};");
importGroup.QueueFindReplace("gml_Object_obj_quizsequence_Draw_0", "myhealth -= 2;",
    $"myhealth -= 2 * {batboarddmgmulti};");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_b1rocks2_Step_0", "ralsei.myhealth -= 1;",
    $"ralsei.myhealth = max(1, ralsei.myhealth - {batboarddmgmulti});");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_mainchara_board_Step_0", "myhealth -= hazard.damage;",
    $"myhealth -= hazard.damage * {batboarddmgmulti};");
string[] hangInThereRalsei = {"gml_Object_obj_b1rocks1_Step_0", "gml_Object_obj_b1lancer_Step_0", "gml_Object_obj_b3bridge_Step_0", "gml_Object_obj_b1power_Step_0"};
foreach (string scrName in hangInThereRalsei)
{   
    importGroup.QueueTrimmedLinesFindReplace(scrName, "myhealth--;", $"myhealth = max(1, myhealth - {batboarddmgmulti});");
}

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