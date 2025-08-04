using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

// Prefire checks
EnsureDataLoaded();
const string expectedDisplayName = "DELTARUNE Chapter 1";
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

string[] damageLikes = {"gml_GlobalScript_scr_damage"};

// Apply damage multiplier
foreach (string scrName in damageLikes)
{   
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", "hpdiff = tdamage;", @"
        tdamage = ceil(tdamage * global.diffdmgmulti);
        hpdiff = tdamage;
        ");
}
importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage_all_overworld", "hpdiff = tdamage;", @"
    tdamage = ceil(tdamage * global.diffdmgmulti);
    hpdiff = tdamage;
    ");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_laserscythe_Other_15", "global.hp[global.char[i]] = ceil(global.hp[global.char[i]] * 0.7);",
    "global.hp[global.char[i]] = ceil(global.hp[global.char[i]] * power(0.7, global.diffdmgmulti));");

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