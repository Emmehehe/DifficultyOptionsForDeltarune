using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

// Prefire checks
EnsureDataLoaded();
const string expectedDisplayName = "DELTARUNE Chapter 4";
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
importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_load", "scr_tempsave();", @"
    scr_tempsave();

    ossafe_ini_open(""difficulty_"" + string(global.filechoice) + "".ini"");
    
    global.diffdmgmulti = ini_read_real(""DIFFICULTY"", ""DAMAGE_MULTIPLIER"", 1);
    if (!ini_key_exists(""DIFFICULTY"", ""DAMAGE_MULTIPLIER"")) ini_write_real(""DIFFICULTY"", ""DAMAGE_MULTIPLIER"", global.diffdmgmulti);
    global.diffdwnpenalty = ini_read_real(""DIFFICULTY"", ""DOWN_PENALTY"", 1 / 2);
    if (!ini_key_exists(""DIFFICULTY"", ""DOWN_PENALTY"")) ini_write_real(""DIFFICULTY"", ""DOWN_PENALTY"", global.diffdwnpenalty);
    global.diffvictoryres = ini_read_real(""DIFFICULTY"", ""VICTORY_RES"", 1 / 8);
    if (!ini_key_exists(""DIFFICULTY"", ""VICTORY_RES"")) ini_write_real(""DIFFICULTY"", ""VICTORY_RES"", global.diffvictoryres);

    ossafe_ini_close();
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
string[] sandbagLimits = {"tdamage", "hpdiff"};
foreach (string limit in sandbagLimits)
{
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", $"if (global.chapter == 4 && i_ex(obj_hammer_of_justice_enemy) && {limit} < 5)", 
        $"if (global.chapter == 4 && i_ex(obj_hammer_of_justice_enemy) && {limit} < (5 * global.diffdmgmulti))");
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", $"{limit} = 5;", $"{limit} = 5 * global.diffdmgmulti;");
}
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
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_incense_cloud_Other_15", "repeat (_r)", @"
    _r = ceil(_r * global.diffdmgmulti);
    repeat (_r)
");

// Apply down penalty
foreach (string scrName in damageLikes)
{   
    importGroup.QueueFindReplace(scrName, "global.maxhp[chartarget] / 2", "global.maxhp[chartarget] * global.diffdwnpenalty");
    importGroup.QueueFindReplace(scrName, "global.maxhp[0] / 2", "global.maxhp[0] * global.diffdwnpenalty");
}
importGroup.QueueFindReplace("gml_GlobalScript_scr_down_partymember", "global.maxhp[_chartarget] / 2", "global.maxhp[_chartarget] * global.diffdwnpenalty");
string[] heavySmokers = {"1", "2", "3"};
foreach (string smoker in heavySmokers)
{
    importGroup.QueueFindReplace("gml_Object_obj_incense_cloud_Other_15", $"global.maxhp[{smoker}] / 2", $"global.maxhp[{smoker}] * global.diffdwnpenalty");
}

// Apply victory res - if VictoryRes is 0 then don't heal; additionally ensure the heal brings the character to at least 1 hp for low values of VictoryRes
importGroup.QueueFindReplace("gml_Object_obj_battlecontroller_Step_0", "global.maxhp[i] / 8", "global.diffvictoryres >= 0 ? max(1, global.maxhp[i] * global.diffvictoryres) : global.hp[i]");

// Finish edit
importGroup.Import();
ScriptMessage($"Success: Difficulty options added to '{displayName}'!");