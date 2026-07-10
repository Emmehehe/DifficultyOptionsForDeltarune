using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;

EnsureDataLoaded();
var displayName = Data?.GeneralInfo?.DisplayName?.Content;

// check version
UndertaleVariable alreadyInstalled = Data.Variables.ByName("installed_customdifficulty");
if (alreadyInstalled != null) {
    ScriptMessage($"Skiping custom difficulty install for '{displayName}' as it is already installed.");
    return;
}

// Prefire checks
const string expectedDisplayName = "DELTARUNE \\S+ ([1-5](?:&2)?)";
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
ScriptMessage($"Adding custom difficulty to '{displayName}'...");

// Code edits
UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data){
    ThrowOnNoOpFindReplace = true
};

// Hide Reward Ranking from the menu to reduce clutter as it's not that useful. Users can still adjust Reward Ranking from the .ini file if needed.
readonly bool hide_rewardrank = true;

// Define presets
readonly struct Preset {
    public Preset () {}
    // default to values from vanilla Deltarune
    public readonly float damagemulti { get; init; }   = 1;
    public readonly float gameboarddmgx { get; init; } = -1;
    public readonly bool hitall { get; init; }         = false;
    public readonly float iframes { get; init; }       = 1;
    public readonly float enemycd { get; init; }       = 1;
    public readonly float gmbrdenemycd { get; init; }  = -1;
    public readonly float tpgain { get; init; }        = 1;
    public readonly float battlerewards { get; init; } = 1;
    public readonly bool rewardranking { get; init; }  = false;
    public readonly float downdeficit { get; init; }   = 1 / 2f;
    public readonly float downedregen { get; init; }   = 1 / 8f;
    public readonly float victoryres { get; init; }    = 1 / 8f;
    public readonly float plrdmg { get; init; }        = 1;
    public readonly float gmbrdplrdmg { get; init; }   = -1;
    public readonly float plrheal { get; init; }       = 1;
    public readonly float gmbrdplrheal { get; init; }  = -1;
    public readonly float mercy { get; init; }         = 1;
    public readonly bool saveheal { get; init; }       = true;

}
const string preset_default = "Normal";
Dictionary<string, Preset> presets = new Dictionary<string, Preset>();
presets.Add(
    "Easy", new Preset {
        damagemulti   = 0.5f,
        iframes       = 1.5f
    });
presets.Add(
    preset_default, new Preset { /* Use defaults. */ });
presets.Add(
    "Hard", new Preset {
        damagemulti   = 1.5f,
        gameboarddmgx = 1.25f,
        iframes       = 0.8f,
        enemycd       = 0.9f,
        gmbrdenemycd  = 0.95f
    });
presets.Add(
    "Nightmare", new Preset {
        damagemulti   = 2,
        gameboarddmgx = 1.5f,
        iframes       = 0.65f,
        enemycd       = 0.8f,
        gmbrdenemycd  = 0.9f
    });
presets.Add(
    "Nightmare-EX", new Preset {
        damagemulti   = 2.5f,
        gameboarddmgx = 1.75f,
        iframes       = 0.5f,
        enemycd       = 0.7f,
        gmbrdenemycd  = 0.85f
    });
presets.Add(
    "Nightmare-Neo", new Preset {
        damagemulti   = 2.5f,
        gameboarddmgx = 1.75f,
        iframes       = 0.5f,
        enemycd       = 0.7f,
        gmbrdenemycd  = 0.85f,
        battlerewards = 0.5f,
        downedregen   = 0,
        victoryres    = -1
    });
presets.Add(
    "No-Hit", new Preset {
        damagemulti   = 2147483647,
        hitall        = true
    });

string loadsettingsblock(string filechoice)
{
    return @$"
        ossafe_ini_open(""difficulty_"" + string({filechoice}) + "".ini"");
        global.diff_damagemulti = ini_read_real(""DIFFICULTY"", ""DAMAGE_MULTI"", {presets[preset_default].damagemulti.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_gameboarddmgx = ini_read_real(""DIFFICULTY"", ""GAMEBOARD_DMG_X"", {presets[preset_default].gameboarddmgx.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_hitall = ini_read_real(""DIFFICULTY"", ""HIT_ALL"", {presets[preset_default].hitall.ToString().ToLower()});
        global.diff_iframes = ini_read_real(""DIFFICULTY"", ""I_FRAMES"", {presets[preset_default].iframes.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_enemycd = ini_read_real(""DIFFICULTY"", ""ENEMY_COOLDOWNS"", {presets[preset_default].enemycd.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_gmbrdenemycd = ini_read_real(""DIFFICULTY"", ""GMBRD_ENEMY_CDS"", {presets[preset_default].gmbrdenemycd.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_tpgain = ini_read_real(""DIFFICULTY"", ""TP_GAIN"", {presets[preset_default].tpgain.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_battlerewards = ini_read_real(""DIFFICULTY"", ""BATTLE_REWARDS"", {presets[preset_default].battlerewards.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_rewardranking = ini_read_real(""DIFFICULTY"", ""REWARD_RANKING"", {presets[preset_default].rewardranking.ToString().ToLower()});
        global.diff_downdeficit = ini_read_real(""DIFFICULTY"", ""DOWN_DEFICIT"", {presets[preset_default].downdeficit.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_downedregen = ini_read_real(""DIFFICULTY"", ""DOWNED_REGEN"", {presets[preset_default].downedregen.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_victoryres = ini_read_real(""DIFFICULTY"", ""VICTORY_RES"", {presets[preset_default].victoryres.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_plrdmg = ini_read_real(""DIFFICULTY"", ""PLR_DMG"", {presets[preset_default].plrdmg.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_gmbrdplrdmg = ini_read_real(""DIFFICULTY"", ""GMBRD_PLR_DMG"", {presets[preset_default].gmbrdplrdmg.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_plrheal = ini_read_real(""DIFFICULTY"", ""PLR_HEAL"", {presets[preset_default].plrheal.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_gmbrdplrheal = ini_read_real(""DIFFICULTY"", ""GMBRD_PLR_HEAL"", {presets[preset_default].gmbrdplrheal.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_mercy = ini_read_real(""DIFFICULTY"", ""MERCY"", {presets[preset_default].mercy.ToString("F10", CultureInfo.InvariantCulture)});
        global.diff_saveheal = ini_read_real(""DIFFICULTY"", ""SAVE_POINT_HEAL"", {presets[preset_default].saveheal.ToString().ToLower()});
        ossafe_ini_close();

        // Determine preset
        {string.Join(" else ", presets.Select(pair => @$"
            if (global.diff_damagemulti == {pair.Value.damagemulti.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_gameboarddmgx == {pair.Value.gameboarddmgx.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_hitall == {pair.Value.hitall.ToString().ToLower()}
                && global.diff_iframes == {pair.Value.iframes.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_enemycd == {pair.Value.enemycd.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_gmbrdenemycd == {pair.Value.gmbrdenemycd.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_tpgain == {pair.Value.tpgain.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_battlerewards == {pair.Value.battlerewards.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_rewardranking == {pair.Value.rewardranking.ToString().ToLower()}
                && global.diff_downdeficit == {pair.Value.downdeficit.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_downedregen == {pair.Value.downedregen.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_victoryres == {pair.Value.victoryres.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_plrdmg == {pair.Value.plrdmg.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_gmbrdplrdmg == {pair.Value.gmbrdplrdmg.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_plrheal == {pair.Value.plrheal.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_gmbrdplrheal == {pair.Value.gmbrdplrheal.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_mercy == {pair.Value.mercy.ToString("F10", CultureInfo.InvariantCulture)}
                && global.diff_saveheal == {pair.Value.saveheal.ToString().ToLower()}) {{
                global.diff_preset = ""{pair.Key}"";
            }}
        "))}
        else {{
            global.diff_preset = ""Custom"";
        }}
    ";
}

// Add globals
string[] gamestartLikes = {"gml_GlobalScript_scr_gamestart"};
if (ch_no == 0)
{
    string[] demoGamestartLikes = {"gml_GlobalScript_scr_gamestart_ch1"};
    gamestartLikes = gamestartLikes.Concat(demoGamestartLikes).ToArray();
}
foreach (string scrName in gamestartLikes)
{
    importGroup.QueueRegexFindReplace(scrName, "function scr_gamestart(?:_ch1)?\\(\\)\\s*{", @$"
        function scr_gamestart{(scrName.EndsWith("_ch1") ? "_ch1" : "")}()
        {{
            var installed_customdifficulty = true;

            global.diff_usepreset = function()
            {{
                switch (global.diff_preset) {{
                    {string.Join("\n", presets.Select(pair => @$"
                        case ""{pair.Key}"":
                            global.diff_damagemulti = {pair.Value.damagemulti.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_gameboarddmgx = {pair.Value.gameboarddmgx.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_hitall = {pair.Value.hitall.ToString().ToLower()};
                            global.diff_iframes = {pair.Value.iframes.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_enemycd = {pair.Value.enemycd.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_gmbrdenemycd = {pair.Value.gmbrdenemycd.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_tpgain = {pair.Value.tpgain.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_battlerewards = {pair.Value.battlerewards.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_rewardranking = {pair.Value.rewardranking.ToString().ToLower()};
                            global.diff_downdeficit = {pair.Value.downdeficit.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_downedregen = {pair.Value.downedregen.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_victoryres = {pair.Value.victoryres.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_plrdmg = {pair.Value.plrdmg.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_gmbrdplrdmg = {pair.Value.gmbrdplrdmg.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_plrheal = {pair.Value.plrheal.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_gmbrdplrheal = {pair.Value.gmbrdplrheal.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_mercy = {pair.Value.mercy.ToString("F10", CultureInfo.InvariantCulture)};
                            global.diff_saveheal = {pair.Value.saveheal.ToString().ToLower()};
                            break;
                    "))}
                    case ""Custom"":
                    default:
                        // Nothing to do
                        break;
                }}
            }}

            global.diff_usepreset_custom = function()
            {{
                global.diff_preset = ""Custom"";
                global.diff_usepreset();
            }}

            global.diff_usepreset_default = function()
            {{
                global.diff_preset = ""{preset_default}"";
                global.diff_usepreset();
            }}

            global.diff_usepreset_default();
            global.diff_importslot = -1;
            global.diff_importvaluerange = """";

            // Provide support for mod devs to add compatibility
            global.diff_apply = function(arg0, arg1, arg2) {{
                switch (arg0) {{
                    case ""DIFFOP_DAMAGE"":
                    return ceil(global.diff_damagemulti * arg1);
                    case ""DIFFOP_DAMAGE_GB"":
                    return (global.diff_gameboarddmgx < 0 ? global.diff_damagemulti : global.diff_gameboarddmgx) * arg1;
                    case ""DIFFOP_HITALL"":
                    return global.diff_hitall || arg1;
                    case ""DIFFOP_IFRAMES"":
                    return ceil(global.diff_iframes * arg1);
                    case ""DIFFOP_ENEMYCD"":
                    return round(global.diff_enemycd * arg1);
                    case ""DIFFOP_ENEMYCD_GB"":
                    return ceil((global.diff_gmbrdenemycd < 0 ? global.diff_enemycd : global.diff_gmbrdenemycd) * arg1);
                    case ""DIFFOP_TPGAIN"":
                    return ceil(global.diff_tpgain * arg1);
                    case ""DIFFOP_REWARDS"":
                    return ceil(global.diff_battlerewards * arg1);
                    case ""DIFFOP_REWARDRANK_GB"":
                    return global.diff_rewardranking || arg1;
                    case ""DIFFOP_DOWNDEF"":
                    return max(-999, floor(global.diff_downdeficit * arg1));
                    case ""DIFFOP_DOWNREGEN"":
                    return ceil(global.diff_downedregen * arg1);
                    case ""DIFFOP_VICRES"":
                    return ceil(global.diff_victoryres >= 0 ? max(1, arg1 * global.diff_victoryres) : arg2);
                    case ""DIFFOP_PLRDMG"":
                    return ceil(global.diff_plrdmg * arg1);
                    case ""DIFFOP_PLRDMG_GB"":
                    return ceil(global.diff_gmbrdplrdmg * arg1);
                    case ""DIFFOP_PLRHEAL"":
                    return ceil(global.diff_plrheal * arg1);
                    case ""DIFFOP_PLRHEAL_GB"":
                    return ceil(global.diff_gmbrdplrheal * arg1);
                    case ""DIFFOP_MERCY"":
                    return ceil(global.diff_mercy * arg1);
                    case ""DIFFOP_SAVEHEAL"":
                    return ceil(global.diff_saveheal * arg1);
                }}
            }}

            global.diff_importfromslot = function() {{
                if (global.diff_importslot == -1)
                    return;

                {loadsettingsblock("global.diff_importslot")}

                global.diff_importslot = -1;
            }}

        ");
}

// Load globals from config
string[] loadLikes = {"gml_GlobalScript_scr_load"};
if (ch_no > 1 || ch_no == 0)
{
    string[] loadCh1 = {"gml_GlobalScript_scr_load_chapter1"};
    loadLikes = loadLikes.Concat(loadCh1).ToArray();
}
if (ch_no == 0)
{
    string[] loadCh1 = {"gml_GlobalScript_scr_load_ch1"};
    loadLikes = loadLikes.Concat(loadCh1).ToArray();
}
if (ch_no > 2)
{
    string[] loadCh2 = {"gml_GlobalScript_scr_load_chapter2"};
    loadLikes = loadLikes.Concat(loadCh2).ToArray();
}
if (ch_no > 3)
{
    string[] loadCh3 = {"gml_GlobalScript_scr_load_chapter3"};
    loadLikes = loadLikes.Concat(loadCh3).ToArray();
}
if (ch_no > 4)
{
    string[] loadCh4 = {"gml_GlobalScript_scr_load_chapter4"};
    loadLikes = loadLikes.Concat(loadCh4).ToArray();
}
// if (ch_no > 5)
// {
//     string[] loadCh5 = {"gml_GlobalScript_scr_load_chapter5"};
//     loadLikes = loadLikes.Concat(loadCh5).ToArray();
// }
// if (ch_no > 6)
// {
//     string[] loadCh6 = {"gml_GlobalScript_scr_load_chapter6"};
//     loadLikes = loadLikes.Concat(loadCh6).ToArray();
// }

foreach (string scrName in loadLikes)
{
    importGroup.QueueTrimmedLinesFindReplace(scrName, $"ossafe_file_text_close{(scrName.EndsWith("_ch1") ? "_ch1" : "")}(myfileid);", @$"
        ossafe_file_text_close{(scrName.EndsWith("_ch1") ? "_ch1" : "")}(myfileid);

        {loadsettingsblock("global.filechoice")}

        global.diff_importvaluerange = """";
        for (var i = 0; i < 3; i++) {{
            if (i != global.filechoice && ossafe_file_exists(""difficulty_"" + string(i) + "".ini"")) {{
                global.diff_importvaluerange += (global.diff_importvaluerange != """" ? "";"" : """") + ""SLOT"" + string(i + 1) + ""="" + string(i);
            }}
        }}

        ");
}

// Save globals to config
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

        ossafe_ini_open(""difficulty_"" + string(global.filechoice) + "".ini"");
        ini_write_real(""DIFFICULTY"", ""DAMAGE_MULTI"", global.diff_damagemulti);
        ini_write_real(""DIFFICULTY"", ""GAMEBOARD_DMG_X"", global.diff_gameboarddmgx);
        ini_write_real(""DIFFICULTY"", ""HIT_ALL"", global.diff_hitall);
        ini_write_real(""DIFFICULTY"", ""I_FRAMES"", global.diff_iframes);
        ini_write_real(""DIFFICULTY"", ""ENEMY_COOLDOWNS"", global.diff_enemycd);
        ini_write_real(""DIFFICULTY"", ""GMBRD_ENEMY_CDS"", global.diff_gmbrdenemycd);
        ini_write_real(""DIFFICULTY"", ""TP_GAIN"", global.diff_tpgain);
        ini_write_real(""DIFFICULTY"", ""BATTLE_REWARDS"", global.diff_battlerewards);
        ini_write_real(""DIFFICULTY"", ""REWARD_RANKING"", global.diff_rewardranking);
        ini_write_real(""DIFFICULTY"", ""DOWN_DEFICIT"", global.diff_downdeficit);
        ini_write_real(""DIFFICULTY"", ""DOWNED_REGEN"", global.diff_downedregen);
        ini_write_real(""DIFFICULTY"", ""VICTORY_RES"", global.diff_victoryres);
        ini_write_real(""DIFFICULTY"", ""PLR_DMG"", global.diff_plrdmg);
        ini_write_real(""DIFFICULTY"", ""GMBRD_PLR_DMG"", global.diff_gmbrdplrdmg);
        ini_write_real(""DIFFICULTY"", ""PLR_HEAL"", global.diff_plrheal);
        ini_write_real(""DIFFICULTY"", ""GMBRD_PLR_HEAL"", global.diff_gmbrdplrheal);
        ini_write_real(""DIFFICULTY"", ""MERCY"", global.diff_mercy);
        ini_write_real(""DIFFICULTY"", ""SAVE_POINT_HEAL"", global.diff_saveheal);
        ossafe_ini_close();
        ");
}

// Add mod menu
string[] darkcons = {"gml_Object_obj_darkcontroller"};
if (ch_no == 0)
{
    string[] demoDarkcons = {"gml_Object_obj_darkcontroller_ch1"};
    darkcons = darkcons.Concat(demoDarkcons).ToArray();
}
foreach (string darkcon in darkcons)
{
    importGroup.QueueAppend(darkcon + "_Create_0", @$"

        if (!variable_instance_exists(global, ""modmenu_data""))
            global.modmenu_data = array_create(0);

        var menudata = ds_map_create();
        ds_map_add(menudata, ""title_en"", ""Difficulty"");
        ds_map_add(menudata, ""left_margin_en"", 0);
        ds_map_add(menudata, ""left_value_pos_en"", 240);

        var formdata = array_create(0);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Preset"");
        ds_map_add(rowdata, ""value_range_en"", ""{string.Join(";", presets.Select(pair => @$"{pair.Key.ToUpper()}={pair.Key}`"))};CUSTOM=Custom`"");
        ds_map_add(rowdata, ""value_name"", ""diff_preset"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset"");
        ds_map_add(rowdata, ""force_scroll"", true);
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""BATTLE"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Damage Multi"");
        ds_map_add(rowdata, ""value_range_en"", ""0~1000%;INF=2147483647"");
        ds_map_add(rowdata, ""value_name"", ""diff_damagemulti"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        {(ch_no != 3 ? "" : @"
        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Gameboard Dmg X"");
        ds_map_add(rowdata, ""value_range_en"", ""INHERIT=-1;0-1000%;INF=2147483647"");
        ds_map_add(rowdata, ""value_name"", ""diff_gameboarddmgx"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);
        ")}

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Hit.All"");
        ds_map_add(rowdata, ""value_range_en"", ""OFF=false;ON=true"");
        ds_map_add(rowdata, ""value_name"", ""diff_hitall"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""I-Frames"");
        ds_map_add(rowdata, ""value_range_en"", ""0~1000%"");
        ds_map_add(rowdata, ""value_name"", ""diff_iframes"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Enemy Cooldowns"");
        ds_map_add(rowdata, ""value_range_en"", ""0~200%"");
        ds_map_add(rowdata, ""value_name"", ""diff_enemycd"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        {(ch_no != 3 ? "" : @"
        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Gmbrd Enemy CDs"");
        ds_map_add(rowdata, ""value_range_en"", ""INHERIT=-1;0~200%"");
        ds_map_add(rowdata, ""value_name"", ""diff_gmbrdenemycd"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);
        ")}

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Battle Rewards"");
        ds_map_add(rowdata, ""value_range_en"", ""0~1000%"");
        ds_map_add(rowdata, ""value_name"", ""diff_battlerewards"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        {((hide_rewardrank || ch_no != 3) ? "" : @"
        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Reward Ranking"");
        ds_map_add(rowdata, ""value_range_en"", ""OFF=false;ON=true"");
        ds_map_add(rowdata, ""value_name"", ""diff_rewardranking"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);
        ")}

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""DOWN"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Down Deficit"");
        ds_map_add(rowdata, ""value_range_en"", ""0~1000%;[-999]=2147483647"");
        ds_map_add(rowdata, ""value_name"", ""diff_downdeficit"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Downed Regen"");
        ds_map_add(rowdata, ""value_range_en"", ""0~1000%;INSTANT=2147483647"");
        ds_map_add(rowdata, ""value_name"", ""diff_downedregen"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Victory Res"");
        ds_map_add(rowdata, ""value_range_en"", ""OFF=-1;0~100%"");
        ds_map_add(rowdata, ""value_name"", ""diff_victoryres"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""PLAYER"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Damage"");
        ds_map_add(rowdata, ""value_range_en"", ""0~1000%;INF=2147483647"");
        ds_map_add(rowdata, ""value_name"", ""diff_plrdmg"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        {(ch_no != 3 ? "" : @"
        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Gmbrd Damage"");
        ds_map_add(rowdata, ""value_range_en"", ""INHERIT=-1;0-1000%;INF=2147483647"");
        ds_map_add(rowdata, ""value_name"", ""diff_gmbrdplrdmg"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);
        ")}

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Healing"");
        ds_map_add(rowdata, ""value_range_en"", ""0~1000%;INF=2147483647"");
        ds_map_add(rowdata, ""value_name"", ""diff_plrheal"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        {(ch_no != 3 ? "" : @"
        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Gmbrd Healing"");
        ds_map_add(rowdata, ""value_range_en"", ""INHERIT=-1;0-1000%;INF=2147483647"");
        ds_map_add(rowdata, ""value_name"", ""diff_gmbrdplrheal"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);
        ")}

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""TP Gain"");
        ds_map_add(rowdata, ""value_range_en"", ""0~1000%;INF=2147483647"");
        ds_map_add(rowdata, ""value_name"", ""diff_tpgain"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Mercy"");
        ds_map_add(rowdata, ""value_range_en"", ""0~1000%;INF=2147483647"");
        ds_map_add(rowdata, ""value_name"", ""diff_mercy"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""SAVE Point Heal"");
        ds_map_add(rowdata, ""value_range_en"", ""OFF=false;ON=true"");
        ds_map_add(rowdata, ""value_name"", ""diff_saveheal"");
        ds_map_add(rowdata, ""on_change"", ""diff_usepreset_custom"");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", """");
        array_push(formdata, rowdata);

        var rowdata = ds_map_create();
        ds_map_add(rowdata, ""title_en"", ""Reset to Defaults"");
        ds_map_add(rowdata, ""func_name"", ""diff_usepreset_default"");
        array_push(formdata, rowdata);

        if (global.diff_importvaluerange != """") {{
            var rowdata = ds_map_create();
            ds_map_add(rowdata, ""title_en"", ""Import from:"");
            ds_map_add(rowdata, ""value_range_en"", global.diff_importvaluerange);
            ds_map_add(rowdata, ""value_name"", ""diff_importslot"");
            ds_map_add(rowdata, ""func_name"", ""diff_importfromslot"");
            ds_map_add(rowdata, ""force_scroll"", true);
            array_push(formdata, rowdata);
        }}

        ds_map_add(menudata, ""form"", formdata);

        array_push(global.modmenu_data, menudata);
    ");
}

string[] damageLikes = {"gml_GlobalScript_scr_damage"};
if (ch_no == 0)
{
    string[] demoDamageLikes = {"gml_GlobalScript_scr_damage_ch1"};
    damageLikes = damageLikes.Concat(demoDamageLikes).ToArray();
}
if (ch_no >= 2 || ch_no == 0)
{
    string[] ch2UpDamageLikes = {"gml_GlobalScript_scr_damage_proportional", "gml_GlobalScript_scr_damage_sneo_final_attack"};
    damageLikes = damageLikes.Concat(ch2UpDamageLikes).ToArray();
}
if (ch_no == 3)
{
    string[] ch3DamageLikes = {"gml_GlobalScript_scr_damage_fixed", "gml_GlobalScript_scr_damage_maxhp"};
    damageLikes = damageLikes.Concat(ch3DamageLikes).ToArray();
}

// Apply damage multiplier
foreach (string scrName in damageLikes)
{   
    importGroup.QueueTrimmedLinesFindReplace(scrName, "hpdiff = tdamage;", @"
        origdamage = tdamage;
        tdamage = ceil(tdamage * global.diff_damagemulti);
        hpdiff = tdamage;
        ");
    importGroup.QueueRegexFindReplace(scrName, "if \\(target == 3\\)\\s*{", @"
        if (target == 3)
        {
            tdamage = origdamage;
        ");
    importGroup.QueueTrimmedLinesFindReplace(scrName, "if (global.charaction[hpi] == 10)", @"
        tdamage = ceil(tdamage * global.diff_damagemulti);

        if (global.charaction[hpi] == 10)
        ");
}
// TODO build all overworld/plat script array instead of copy pasting maybe
if (ch_no == 0)
{
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage_all_overworld_ch1", "hpdiff = tdamage;", @"
        tdamage = ceil(tdamage * global.diff_damagemulti);
        hpdiff = tdamage;
        ");
}
importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage_all_overworld", "hpdiff = tdamage;", @"
    tdamage = ceil(tdamage * global.diff_damagemulti);
    hpdiff = tdamage;
    ");
if (ch_no >= 5)
{
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage_all_platmode", "hpdiff = tdamage;", @"
        tdamage = ceil(tdamage * global.diff_damagemulti);
        hpdiff = tdamage;
        ");
}
if (ch_no == 1 || ch_no == 0)
{
    importGroup.QueueTrimmedLinesFindReplace(ch_no == 0 ? "gml_Object_obj_laserscythe_ch1_Other_15" : "gml_Object_obj_laserscythe_Other_15",
        "global.hp[global.char[i]] = ceil(global.hp[global.char[i]] * 0.7);",
        "global.hp[global.char[i]] = ceil(global.hp[global.char[i]] * power(0.7, global.diff_damagemulti));");
}
if (ch_no == 2 || ch_no == 0)
{
    importGroup.QueueFindReplace("gml_Object_o_boxingcontroller_Collision_o_boxing_hitbox", "if (global.hp[1] <= 10)", "if (global.hp[1] <= round(10 * global.diff_damagemulti))");
    importGroup.QueueFindReplace("gml_Object_o_boxingcontroller_Collision_o_boxing_hitbox", "global.hp[1] -= 10;", "global.hp[1] -= round(10 * global.diff_damagemulti);");
    importGroup.QueueFindReplace("gml_Object_o_boxingcontroller_Collision_o_boxing_hitbox", "global.hp[1] / 2", "(global.diff_damagemulti != 0 ? (global.hp[1] / power(2, 1 / global.diff_damagemulti)) : 0)");
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_o_boxingcontroller_Collision_o_boxing_hitbox", "global.hp[1] -= final_damage_amount;", 
        "global.hp[1] -= final_damage_amount * global.diff_damagemulti;");
}
if (ch_no == 3)
{
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_quizsequence_Other_13", "var _damage = irandom_range(30, 38);",
        "var _damage = ceil(irandom_range(30, 38) * global.diff_damagemulti);");
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_ch3_b4_chef_kris_Create_0", "global.hp[1] = clamp(global.hp[1] - damage_amount, 1, global.maxhp[1]);", @"
        damage_amount = ceil(damage_amount * global.diff_damagemulti);

        global.hp[1] = clamp(global.hp[1] - damage_amount, 1, global.maxhp[1]);
    ");
}
if (ch_no == 4) {
    string[] sandbagLimits = {"tdamage", "hpdiff"};
    foreach (string limit in sandbagLimits)
    {
        importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", $"if (global.chapter == 4 && i_ex(obj_hammer_of_justice_enemy) && {limit} < 5)", 
            $"if (global.chapter == 4 && i_ex(obj_hammer_of_justice_enemy) && {limit} < (5 * global.diff_damagemulti))");
        importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", $"{limit} = 5;", $"{limit} = 5 * global.diff_damagemulti;");
    }
}
// Apply damage multiplier (Damage Over Time)
if (ch_no >= 2 || ch_no == 0)
{
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_battlecontroller_Step_0", "t_siner++;", "");
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_battlecontroller_Step_0", "if (global.charweapon[4] == 13)", @"
        if (global.charweapon[4] == 13)
        {
            if (global.hp[4] > round(global.maxhp[4] / 3))
                global.hp[4] = max(round(global.maxhp[4] / 3), global.hp[4] - floor(t_siner / 6));
            
            t_siner = t_siner % 6;
            t_siner += global.diff_damagemulti;
        }

        if (false)
    ");
    string poisonScrName = (ch_no == 2 || ch_no == 0) ? "gml_Object_obj_heroparent_Draw_0" : "gml_Object_obj_heroparent_Step_0";
    importGroup.QueueTrimmedLinesFindReplace(poisonScrName, "poisontimer++;", @"
        poisontimer++;
        poisondmgtimer += global.diff_damagemulti;
    ");
    importGroup.QueueTrimmedLinesFindReplace(poisonScrName, "global.hp[global.char[myself]]--;", 
        "global.hp[global.char[myself]] = max(1, global.hp[global.char[myself]] - floor(poisondmgtimer / 10));");
    importGroup.QueueTrimmedLinesFindReplace(poisonScrName, "poisonamount = 0;", @"
        poisonamount = 0;
        poisondmgtimer = 0;
    ");
    importGroup.QueueTrimmedLinesFindReplace(poisonScrName, "poisontimer = 0;", @"
        poisontimer = 0;
        poisondmgtimer = poisondmgtimer % 10;
    ");
}
if (ch_no == 4)
{
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_incense_cloud_Other_15", "repeat (_r)", @"
        _r = ceil(_r * global.diff_damagemulti);
        repeat (_r)
    ");
}

// Apply Game Board damage multiplier
if (ch_no == 3)
{
    const string gameboarddmgmulti = "(global.diff_gameboarddmgx < 0 ? global.diff_damagemulti : global.diff_gameboarddmgx)";
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_board_puzzlebombbullet_Step_0", "myhealth -= other.damage;",
        $"myhealth -= other.damage * {gameboarddmgmulti};");
    importGroup.QueueFindReplace("gml_Object_obj_quizsequence_Draw_0", "myhealth -= 2;",
        $"myhealth -= 2 * {gameboarddmgmulti};");
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_b1rocks2_Step_0", "ralsei.myhealth -= 1;",
        $"ralsei.myhealth = max(1, ralsei.myhealth - {gameboarddmgmulti});");
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_mainchara_board_Step_0", "myhealth -= hazard.damage;",
        $"myhealth -= hazard.damage * {gameboarddmgmulti};");
    string[] hangInThereRalsei = {"gml_Object_obj_b1rocks1_Step_0", "gml_Object_obj_b1lancer_Step_0", "gml_Object_obj_b3bridge_Step_0", "gml_Object_obj_b1power_Step_0"};
    foreach (string scrName in hangInThereRalsei)
    {   
        importGroup.QueueTrimmedLinesFindReplace(scrName, "myhealth--;", $"myhealth = max(1, myhealth - {gameboarddmgmulti});");
    }
    // fix health item not working between 0 and 1
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_board_heal_pickup_Step_0", "if (obj_board_controller.kris_object.myhealth < 1)",
        "if (obj_board_controller.kris_object.myhealth <= 0)");
}

// Apply down deficit
foreach (string scrName in damageLikes)
{   
    importGroup.QueueFindReplace(scrName, "global.maxhp[chartarget] / 2", "max(-999, global.maxhp[chartarget] * global.diff_downdeficit)");
    importGroup.QueueFindReplace(scrName, "global.maxhp[0] / 2", "max(-999, global.maxhp[0] * global.diff_downdeficit)");
}
if (ch_no >= 4) {
    importGroup.QueueFindReplace("gml_GlobalScript_scr_down_partymember", "global.maxhp[_chartarget] / 2", "max(-999, global.maxhp[_chartarget] * global.diff_downdeficit)");
}
if (ch_no == 4) {
    string[] heavySmokers = {"1", "2", "3"};
    foreach (string smoker in heavySmokers)
    {
        importGroup.QueueFindReplace("gml_Object_obj_incense_cloud_Other_15", $"global.maxhp[{smoker}] / 2", $"max(-999, global.maxhp[{smoker}] * global.diff_downdeficit)");
    }
}

// Apply victory res - if VictoryRes is 0 then don't heal; additionally ensure the heal brings the character to at least 1 hp for low values of VictoryRes
if (ch_no == 0)
{
    importGroup.QueueFindReplace("gml_Object_obj_battlecontroller_ch1_Step_0", "global.maxhp[i] / 8", "global.diff_victoryres >= 0 ? max(1, global.maxhp[i] * global.diff_victoryres) : global.hp[i]");
}
importGroup.QueueFindReplace("gml_Object_obj_battlecontroller_Step_0", "global.maxhp[i] / 8", "global.diff_victoryres >= 0 ? max(1, global.maxhp[i] * global.diff_victoryres) : global.hp[i]");
if (ch_no >= 5)
{
    importGroup.QueueFindReplace("gml_GlobalScript_scr_endcombat_instant", "global.maxhp[i] / 8", "global.diff_victoryres >= 0 ? max(1, global.maxhp[i] * global.diff_victoryres) : global.hp[i]");
}

// Downed Regen
if (ch_no == 0)
{
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_mnendturn_ch1", "healamt = ceil(global.maxhp[hptarget] / 8);", "healamt = ceil(global.maxhp[hptarget] * global.diff_downedregen);");
}
importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_mnendturn", "healamt = ceil(global.maxhp[hptarget] / 8);", "healamt = ceil(global.maxhp[hptarget] * global.diff_downedregen);");

// Hit.All
string[] singleHits = {"gml_Object_obj_overworldbulletparent_Other_15", "gml_Object_obj_collidebullet_Other_15", "gml_Object_obj_checkers_leap_Other_15"};
if (ch_no == 0) {
    string[] demoSingleHits = {"gml_Object_obj_regularbullet_permanent_ch1_Other_15", "gml_Object_obj_lancerbike_ch1_Other_15", "gml_Object_obj_lancerbike_neo_ch1_Other_15"};
    foreach (string scrName in demoSingleHits)
    {
        importGroup.QueueTrimmedLinesFindReplace(scrName, "scr_damage_ch1();", @"
            {
                if (global.diff_hitall <= 0)
                {
                    scr_damage_ch1();
                }
                else
                {
                    scr_damage_all_ch1();
                }
            }
        ");
    }
}
if (ch_no == 1) {
    string[] ch1SingleHits = {"gml_Object_obj_regularbullet_permanent_Other_15", "gml_Object_obj_lancerbike_Other_15", "gml_Object_obj_lancerbike_neo_Other_15"};
    singleHits = singleHits.Concat(ch1SingleHits).ToArray();
}
if (ch_no == 2 || ch_no == 0) {
    string[] ch2SingleHits = {"gml_Object_obj_mettaton_bomb_hitbox_Other_15", "gml_Object_obj_lancerbike_Other_15", "gml_Object_obj_baseenemy_Step_0", "gml_Object_obj_omawaroid_vaccine_Other_15",
        "gml_Object_obj_viro_needle_Other_15", "gml_Object_obj_yarnbullet_Other_15", "gml_Object_obj_tasque_soundwave_Other_15", "gml_Object_obj_tm_quizzap_Other_15",
        "gml_Object_obj_queen_social_media_Other_15", "gml_Object_obj_queen_wine_attack_droplet_Other_15", "gml_Object_obj_queen_wine_attack_bottom_hurtbox_Other_15",
        "gml_Object_obj_queen_winebubble_Other_15", "gml_Object_obj_sneo_elevator_electric_ball_Other_15", "gml_Object_obj_shrinktangle_Step_0", "gml_Object_obj_sneo_phonehand_master_hurtbox_Other_15",
        "gml_Object_obj_thrash_duck_bullet_Other_15"};
    singleHits = singleHits.Concat(ch2SingleHits).ToArray();
}
if (ch_no == 3) {
    string[] ch3SingleHits = {"gml_GlobalScript_scr_damage_manual", "gml_Object_obj_bullet_dice_Other_15", "gml_Object_obj_knight_roaring_star_Other_15", "gml_Object_obj_susiezilla_statue_Alarm_0",
        "gml_Object_obj_knight_weird_circle_bullet_Other_15", "gml_Object_obj_tenna_enemy_Step_0", "gml_Object_obj_tenna_enemy_Step_0", "gml_Object_obj_regularbullet_elnina_Other_15",
        "gml_Object_obj_knight_diamondswordbullet_ext_Other_15", "gml_Object_obj_rouxls_yarnball_Other_15", "gml_Object_obj_elnina_snowring_Other_15", "gml_Object_obj_knight_enemy_Other_12",
        "gml_Object_obj_snowflake_ult_bullet_Other_15", "gml_Object_obj_rouxls_biplane_flag_Other_15", "gml_Object_obj_knight_pointing_starchild_Other_15",
        "gml_Object_obj_knight_pointing_starchild_Other_15", "gml_Object_obj_precipitation_bullet_parent_Other_15", "gml_Object_obj_bullet_rain_Other_15", "gml_Object_obj_elnina_raindrop_Other_15",
        "gml_Object_obj_bullet_sun_Other_15", "gml_Object_obj_rouxls_helicopter_hitbox_Other_15", "gml_Object_obj_tenna_allstars_bullet_Other_15", "gml_Object_obj_elnina_bouncingbullet_Other_15",
        "gml_Object_obj_tm_quizzap_Other_15", "gml_Object_obj_bullet_snow_Other_15", "gml_Object_obj_rainwater_Other_15", "gml_Object_obj_bullet_homing_Other_15",
        "gml_Object_obj_knight_pointing_star_Other_15", "gml_Object_obj_lanino_solar_system_Other_15", "gml_Object_obj_yarnsnake_bullet_Other_15", "gml_Object_obj_knight_bullethell_bullet2_Other_15",
        "gml_Object_obj_bullet_moon_Other_15", "gml_Object_obj_watercooler_enemy_Other_12", "gml_Object_obj_roaringknight_slash_Other_15", "gml_Object_obj_bullet_submoon_Other_15"};
    singleHits = singleHits.Concat(ch3SingleHits).ToArray();
}
if (ch_no == 4) {
    string[] ch4SingleHits = {"gml_Object_obj_incense_bullet_Other_15", "gml_Object_obj_climb_kris_Step_0", "gml_Object_obj_rotating_object_parent_new_Other_15",
        "gml_Object_obj_holywater_act_line_Other_15", "gml_Object_obj_yarnsnake_bullet_Other_15", "gml_Object_obj_gerson_shell_pinball_Other_15", "gml_Object_obj_regularbullet_elnina_Other_15",
        "gml_Object_obj_spearshot_Other_10", "gml_Object_obj_spearshot_Other_10", "gml_Object_obj_gerson_hammer_bro_hammer_Other_15", "gml_Object_obj_darkness_bullet_Other_15",
        "gml_Object_obj_organ_enemy_vertical_pillar_Other_15", "gml_Object_obj_mizzle_spotlight_eye_Other_15", "gml_Object_obj_holywatercooler_enemy_Other_12",
        "gml_Object_obj_ghosthouse_jackolantern_merciful_Other_15", "gml_Object_obj_gerson_hammer_bounce_left_Other_15", "gml_Object_obj_overworld_knight_sword2_Other_15",
        "gml_Object_obj_darkshape_bigblast_Other_15", "gml_Object_obj_overworld_knight_sword1_Other_15", "gml_Object_obj_mike_hairball_Other_15",
        "gml_Object_obj_ghosthouse_jackolantern_merciful_old_Other_15", "gml_Object_obj_vertical_dark_shockwave_hurtbox_Other_15", "gml_Object_obj_bullet_dice_Other_15",
        "gml_Object_obj_gerson_swing_down_Other_15", "gml_Object_obj_giant_hammer_Step_0", "gml_Object_obj_gerson_hammer_bounce_down_Other_15", "gml_Object_obj_gerson_growtangle_telegraph_new_Other_15",
        "gml_Object_obj_gh_fireball_bouncy_Other_15", "gml_Object_obj_incense_bullet_fire_Other_15", "gml_Object_obj_tm_quizzap_Other_15", "gml_Object_obj_ow_pathingenemy_Other_15",
        "gml_Object_obj_lightbullet_Other_15", "gml_Object_obj_elnina_bouncingbullet_Other_15", "gml_Object_obj_gh_fireball_linear_Other_15", "gml_Object_obj_mike_spike_Other_15"};
    singleHits = singleHits.Concat(ch4SingleHits).ToArray();
}
if (ch_no == 5) {
    string[] ch5SingleHits = {"gml_Object_obj_flowery_shockwave_Other_15", "gml_Object_obj_rabbitbullet_Other_15", "gml_Object_obj_flowery_crescent_effect_Other_15", "gml_Object_obj_orange_green_controller_Other_10",
        "gml_Object_obj_orange_green_controller_Other_11", "gml_Object_obj_flowery_bullet_parent_Other_15", "gml_Object_obj_flowery_bullet_Other_15", "gml_Object_obj_pink_enemy_Other_12",
        "gml_Object_obj_sheary_smashcutter_Step_2", "gml_Object_obj_tommygun_bullet_Other_15", "gml_Object_obj_mane_Other_15", "gml_Object_obj_bullet_scarecrow1_Other_15", "gml_Object_obj_aquabullet_Other_15",
        "gml_Object_obj_attack_knifefan_bullet_Other_15", "gml_Object_obj_overworld_foxbullet_Other_15", "gml_Object_obj_spike_bullet_Other_15", "gml_Object_obj_dancing_beetle_Other_15", "gml_Object_obj_bullet_dashbar_Other_15",
        "gml_Object_obj_rotating_object_parent_new_Other_15", "gml_Object_obj_purple_aim_attack_Create_0", "gml_Object_obj_bullet_knife_Other_15", "gml_Object_obj_omega_knife_Other_15",
        "gml_Object_obj_bullet_wheelspring_Other_15", "gml_Object_obj_flowery_crescent_Other_15", "gml_Object_obj_bullet_scarecrow2_Other_15", "gml_Object_obj_netskiebullet1_Other_15", "gml_Object_obj_flower_wall_Other_15",
        "gml_Object_obj_tm_quizzap_Other_15", "gml_Object_obj_orangeheart_Create_0", "gml_Object_obj_climb_kris_Step_0", "gml_Object_obj_dbulletcontroller_Step_0", "gml_Object_obj_flowery_star_Other_15"};
    singleHits = singleHits.Concat(ch5SingleHits).ToArray();
}
foreach (string scrName in singleHits)
{
    importGroup.QueueTrimmedLinesFindReplace(scrName, "scr_damage();", @"
        {
            if (global.diff_hitall <= 0)
            {
                scr_damage();
            }
            else
            {
                scr_damage_all();
            }
        }
    ");
}
if (ch_no == 2 || ch_no == 0) {
    // TODO might be cleaner to add a scr_damage_all_proportional function
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_basicbullet_sneo_finale_Other_15", "if (target != 3)", @"
        if (target != 3 && global.diff_hitall > 0)
        {
            if (global.inv < 0)
            {
                scr_damage_cache();
                remdamage = damage;
                _temptarget = target;

                for (ti = 0; ti < 3; ti += 1)
                {
                    global.inv = -1;
                    damage = remdamage;
                    target = ti;

                    if (global.hp[global.char[ti]] > 0 && global.char[ti] != 0)
                        scr_damage_proportional();
                }

                global.inv = global.invc * 40;
                target = _temptarget;
                scr_damage_check();
            }
        }

        if (target != 3 && global.diff_hitall <= 0)
    ");
}
if (ch_no == 3) {
    // TODO might be cleaner to add a scr_damage_all_maxhp function
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_roaringknight_splitslash_Step_0", "if (target != 3)", @"
        if (target != 3 && global.diff_hitall > 0)
        {
            if (global.inv < 0)
            {
                scr_damage_cache();
                remdamage = damage;
                _temptarget = target;

                for (ti = 0; ti < 3; ti += 1)
                {
                    global.inv = -1;
                    damage = remdamage;
                    target = ti;

                    if (global.hp[global.char[ti]] > 0 && global.char[ti] != 0)
                        scr_damage_maxhp(0.66, false, true);
                }

                global.inv = global.invc * 40;
                target = _temptarget;
                scr_damage_check();
            }
        }

        if (target != 3 && global.diff_hitall <= 0)
    ");
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_roaringknight_quickslash_big_Step_0", "if (target != 3)", @"
        if (target != 3 && global.diff_hitall > 0)
        {
            if (global.inv < 0)
            {
                scr_damage_cache();
                remdamage = damage;
                _temptarget = target;

                for (ti = 0; ti < 3; ti += 1)
                {
                    global.inv = -1;
                    damage = remdamage;
                    target = ti;

                    if (global.hp[global.char[ti]] > 0 && global.char[ti] != 0)
                        scr_damage_maxhp(1.25);
                }

                global.inv = global.invc * 40;
                target = _temptarget;
                scr_damage_check();
            }
        }

        if (target != 3 && global.diff_hitall <= 0)
    ");
}
// Disable these weird targeting exceptions in the damage scripts if hit.all=on, otherwise could hit the same target multiple times, instead of each target once.
if (ch_no == 3) {
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", "if (obj_knight_enemy.aoedamage == false)",
        "if (global.diff_hitall <= 0 && obj_knight_enemy.aoedamage == false)");
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", "if (global.chapter == 3 && i_ex(obj_rouxls_ch3_enemy))",
        "if (global.diff_hitall <= 0 && global.chapter == 3 && i_ex(obj_rouxls_ch3_enemy))");
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", "if (global.chapter == 3 && i_ex(obj_tenna_enemy) && obj_tenna_enemy.popularboy && global.hp[3] > 0)",
        "if (global.diff_hitall <= 0 && global.chapter == 3 && i_ex(obj_tenna_enemy) && obj_tenna_enemy.popularboy && global.hp[3] > 0)");
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage_fixed", "if (global.chapter == 3 && i_ex(obj_knight_enemy))",
        "if (global.diff_hitall <= 0 && global.chapter == 3 && i_ex(obj_knight_enemy))");
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage_maxhp", "if (target == 0)",
        "if (global.diff_hitall <= 0 && target == 0)");
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage_maxhp", "if (obj_knight_enemy.myattackchoice != 13)",
        "if (global.diff_hitall <= 0 && obj_knight_enemy.myattackchoice != 13)");
}
if (ch_no == 4) {
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", "if (global.chapter == 4 && i_ex(obj_titan_enemy) && obj_titan_enemy.forcehitralsei)",
        "if (global.diff_hitall <= 0 && global.chapter == 4 && i_ex(obj_titan_enemy) && obj_titan_enemy.forcehitralsei)");
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", "if (global.chapter == 4 && i_ex(obj_sound_of_justice_enemy) && obj_sound_of_justice_enemy.phase == 2)",
        "if (global.diff_hitall <= 0 && global.chapter == 4 && i_ex(obj_sound_of_justice_enemy) && obj_sound_of_justice_enemy.phase == 2)");
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", "if (global.hp[1] < 1)", "if (global.diff_hitall <= 0 && global.hp[1] < 1)");
}
if (ch_no == 5) {
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage", "target = choose(0, 1);", @"
        if (global.diff_hitall > 0)
            return;
        target = choose(0, 1);
    ");
}
// Disable these weird down exceptions if hit.all=on
if (ch_no == 3) {
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage_maxhp", "if (global.hp[1] < 0)",
        "if (global.diff_hitall <= 0 && global.hp[1] < 0)");
    importGroup.QueueTrimmedLinesFindReplace("gml_GlobalScript_scr_damage_fixed", "if (global.hp[1] < 0)",
        "if (global.diff_hitall <= 0 && global.hp[1] < 0)");
}


// I-Frames
string[] iFramers = {"gml_GlobalScript_scr_damage", "gml_GlobalScript_scr_damage_all", "gml_GlobalScript_scr_damage_all_overworld"};
if (ch_no == 0) {
    string[] demoIFramers = {"gml_GlobalScript_scr_damage_ch1", "gml_GlobalScript_scr_damage_all_ch1", "gml_GlobalScript_scr_damage_all_overworld_ch1", "gml_Object_obj_laserscythe_ch1_Other_15"};
    iFramers = iFramers.Concat(demoIFramers).ToArray();
}
if (ch_no == 1) {
    string[] ch1IFramers = {"gml_Object_obj_laserscythe_Other_15"};
    iFramers = iFramers.Concat(ch1IFramers).ToArray();
}
if (ch_no == 2) {
    string[] ch2IFramers = {"gml_Object_obj_basicbullet_sneo_finale_Other_15", "gml_Object_obj_spamton_neo_enemy_Other_12"};
    iFramers = iFramers.Concat(ch2IFramers).ToArray();
    importGroup.QueueFindReplace("gml_Object_obj_sneo_fakeheart_Create_0", "global.inv = 300", "global.inv = global.diff_iframes * 300");
}
if (ch_no >= 2) {
    string[] ch2UpIFramers = {"gml_GlobalScript_scr_damage_sneo_final_attack", "gml_GlobalScript_scr_damage_proportional", "gml_GlobalScript_scr_weaken_party"};
    iFramers = iFramers.Concat(ch2UpIFramers).ToArray();
}
if (ch_no == 3) {
    string[] ch3IFramers = {"gml_Object_obj_roaringknight_splitslash_Step_0", "gml_Object_obj_roaringknight_quickslash_big_Step_0", "gml_GlobalScript_scr_damage_fixed",
    "gml_GlobalScript_scr_damage_maxhp", "gml_Object_obj_tenna_enemy_Step_0", "gml_Object_obj_knight_enemy_Other_12", "gml_Object_obj_watercooler_enemy_Other_12"};
    iFramers = iFramers.Concat(ch3IFramers).ToArray();
    importGroup.QueueFindReplace("gml_Object_obj_tenna_zoom_Step_0", "global.inv = 30", "global.inv = global.diff_iframes * 30");
}
if (ch_no == 4) {
    string[] ch4IFramers = {"gml_Object_obj_mike_raindrop_Other_15", "gml_Object_obj_holywatercooler_enemy_Other_12"};
    iFramers = iFramers.Concat(ch4IFramers).ToArray();
    importGroup.QueueFindReplace("gml_Object_obj_gerson_fakeheart_Create_0", "global.inv = 300", "global.inv = global.diff_iframes * 300");
    importGroup.QueueFindReplace("gml_Object_obj_sound_of_justice_enemy_Step_0", "global.inv = 29", "global.inv = global.diff_iframes * 29");
    importGroup.QueueFindReplace("gml_Object_obj_ow_pathingenemy_Other_15", "global.inv = 60", "global.inv = global.diff_iframes * 60");
    importGroup.QueueFindReplace("gml_Object_obj_hammer_of_justice_enemy_Step_0", "global.inv = 19", "global.inv = global.diff_iframes * 19");
    importGroup.QueueFindReplace("gml_Object_obj_small_jackolantern_Other_15", "global.inv = min(global.inv, 10)", "global.inv = min(global.inv, global.diff_iframes * 10)");
    importGroup.QueueFindReplace("gml_Object_obj_ghosthouse_jackolantern_Other_15", "global.inv = min(global.inv, 10 - floor(hits / 2))",
        "global.inv = min(global.inv, global.diff_iframes * (10 - floor(hits / 2)))");
}
if (ch_no >= 5) {
    string[] ch5UpIFramers = {"gml_GlobalScript_scr_damage_all_platmode"};
    iFramers = iFramers.Concat(ch5UpIFramers).ToArray();
}
if (ch_no == 5) {
    string[] ch5IFramers = {"gml_Object_obj_orange_green_controller_Other_10", "gml_Object_obj_orange_green_controller_Other_11", "gml_Object_obj_sheary_smashcutter_Step_2", "gml_Object_obj_pinknodeact_Step_0"};
    iFramers = iFramers.Concat(ch5IFramers).ToArray();
}
foreach (string scrName in iFramers)
{
    importGroup.QueueFindReplace(scrName, "global.inv = global.invc", "global.inv = global.diff_iframes * global.invc");
}
if (ch_no == 5) {
    // Orange soul i-frames
    importGroup.QueueFindReplace("gml_Object_obj_heart_Step_0", "global.inv = 60;", "global.inv = global.diff_iframes * 60;");
    importGroup.QueueFindReplace("gml_Object_obj_orangeheart_Create_0", "global.inv > 24", "global.inv > global.diff_iframes * 24");
    importGroup.QueueFindReplace("gml_Object_obj_orangeheart_Create_0", "global.inv = 24;", "global.inv = global.diff_iframes * 24;");
}

// Apply Battle Rewards
if (ch_no == 0)
{
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_battlecontroller_ch1_Step_0", "global.xp += global.monsterexp[3];", @"
        global.monsterexp[3] = round(global.diff_battlerewards * global.monsterexp[3]);
        global.xp += global.monsterexp[3];
    ");
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_battlecontroller_ch1_Step_0", "global.gold += global.monstergold[3];", @"
        global.monstergold[3] = round(global.diff_battlerewards * global.monstergold[3]);
        global.gold += global.monstergold[3];
    ");
}
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_battlecontroller_Step_0", "global.xp += global.monsterexp[3];", @"
    global.monsterexp[3] = round(global.diff_battlerewards * global.monsterexp[3]);
    global.xp += global.monsterexp[3];
");
importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_battlecontroller_Step_0", "global.gold += global.monstergold[3];", @"
    global.monstergold[3] = round(global.diff_battlerewards * global.monstergold[3]);
    global.gold += global.monstergold[3];
");
if (ch_no == 3) {
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_gameshow_battlemanager_Step_0", " var scoretoAdd = totalstring;", @"
        var scoretoAdd = totalstring;
        scoretoAdd = string(round(global.diff_battlerewards * real(scoretoAdd)));
    ");
}

// Apply Reward Ranking
if (ch_no == 3) {
    importGroup.QueueTrimmedLinesFindReplace("gml_Object_obj_gameshow_battlemanager_Draw_0", "global.flag[1116] += real(totalstring);", @"
        global.flag[1116] += global.diff_rewardranking > 0 ? round(global.diff_battlerewards * real(totalstring)) : real(totalstring);
    ");
}

// Apply TP Gain
string[] tensionHeals = {"gml_Object_obj_grazebox_Collision_obj_collidebullet"};
if (ch_no == 0) {
    importGroup.QueueFindReplace("gml_Object_obj_heroparent_ch1_Alarm_1", "scr_tensionheal_ch1(", "scr_tensionheal_ch1(global.diff_tpgain * ");
}
if (ch_no >= 0 && ch_no <= 2) {
    string[] ch1to2TensionHeals = {"gml_Object_obj_heroparent_Alarm_1"};
    tensionHeals = tensionHeals.Concat(ch1to2TensionHeals).ToArray();
}
if (ch_no == 2 || ch_no == 0) {
    string[] ch2TensionHeals = {"gml_Object_obj_sneo_lilguy_Collision_obj_yheart_shot", "gml_Object_obj_sneo_crusher_Collision_obj_yheart_shot",
        "gml_Object_obj_pipis_egg_bullet_Collision_obj_yheart_shot", "gml_Object_obj_pipis_egg_bullet_Collision_obj_mettaton_bomb_hitbox", "gml_Object_obj_rouxls_enemy_Create_0",
        "gml_Object_o_boxingcontroller_Step_0", "gml_Object_o_boxinggraze_Alarm_0"};
    tensionHeals = tensionHeals.Concat(ch2TensionHeals).ToArray();
}
if (ch_no >= 3) {
    string[] ch3UpTensionHeals = {"gml_Object_obj_heroparent_Step_0"};
    tensionHeals = tensionHeals.Concat(ch3UpTensionHeals).ToArray();
}
if (ch_no == 3) {
    string[] ch3TensionHeals = {"gml_Object_obj_tracking_sword_slash_extra_graze_Step_0", "gml_Object_obj_heroparent_Other_10"};
    tensionHeals = tensionHeals.Concat(ch3TensionHeals).ToArray();
}
if (ch_no == 4) {
    string[] ch4TensionHeals = {"gml_GlobalScript_scr_boltcheck", "gml_Object_obj_ghosthouse_key_Other_15", "gml_Object_obj_spearshot_Other_10", "gml_Object_obj_ghosthouse_dot_Other_15",
        "gml_Object_obj_darkshape_greenblob_Step_0", "gml_Object_obj_attackpress_Other_11", "gml_Object_obj_hammer_of_justice_enemy_Draw_0"};
    tensionHeals = tensionHeals.Concat(ch4TensionHeals).ToArray();
}
if (ch_no == 5) {
    string[] ch5TensionHeals = {"gml_Object_obj_grazebox_Collision_obj_orangeheart_bullet", "gml_Object_obj_green_enemy_Step_0", "gml_Object_obj_orangeheart_helpful_flower_Create_0", "gml_Object_obj_orange_enemy_Step_0", 
        "gml_Object_obj_orangeheart_wall_Create_0", "gml_Object_obj_orangeheart_wall_Step_0", "gml_Object_obj_flowery_enemy_Step_0", "gml_Object_obj_bullet_healing_Step_0", "gml_Object_obj_orangeheart_floweryjarona_Step_0", 
        "gml_Object_obj_bullet_green_food_Other_15", "gml_Object_obj_orangeheart_jumppad_Step_0", "gml_Object_obj_dokiheart_Step_0", "gml_Object_obj_blue_enemy_Step_0", };
    tensionHeals = tensionHeals.Concat(ch5TensionHeals).ToArray();
}
foreach (string scrName in tensionHeals)
{
    importGroup.QueueFindReplace(scrName, "scr_tensionheal(", "scr_tensionheal(global.diff_tpgain * ");
}
// avoid tp heal items / round to int
if (ch_no == 0)
{
    importGroup.QueueFindReplace("gml_Object_obj_battlecontroller_ch1_Step_0", "scr_tensionheal_ch1(40", "scr_tensionheal_ch1(global.diff_tpgain * 40");
}
importGroup.QueueFindReplace("gml_Object_obj_battlecontroller_Step_0", "scr_tensionheal(40", "scr_tensionheal(global.diff_tpgain * 40");
if (ch_no == 4) {
    importGroup.QueueFindReplace("gml_Object_obj_battlecontroller_Step_0", "scr_tensionheal(5", "scr_tensionheal(global.diff_tpgain * 5");
}
if (ch_no == 5) {
    importGroup.QueueFindReplace("gml_Object_obj_battlecontroller_Step_0", "scr_tensionheal(20", "scr_tensionheal(global.diff_tpgain * 20");
    importGroup.QueueFindReplace("gml_Object_obj_date_controller_Step_0", "scr_tensionheal(round(", "scr_tensionheal(round(global.diff_tpgain * ");
}

// Apply Mercy Build-up
{
    importGroup.QueueFindReplace("gml_GlobalScript_scr_mercyadd", "arg1;", "ceil(global.diff_mercy * arg1);");
    if (ch_no != 1) {
        importGroup.QueueFindReplace("gml_GlobalScript_scr_mercyadd", "arg1 <", "ceil(global.diff_mercy * arg1) <");
    }
}
if (ch_no == 0) {
    importGroup.QueueFindReplace("gml_GlobalScript_scr_mercyadd_ch1", "arg1;", "ceil(global.diff_mercy * arg1);");
}
// Fix crash when sparing lancer in castle town
if (ch_no == 1) {
    importGroup.QueueFindReplace("gml_GlobalScript_scr_spell", "if (global.monstertype[star] != 3)", @"
        if (global.monstertype[star] == 2) {
            // do nothing
        }
        else if (global.monstertype[star] != 3)
    ");
    importGroup.QueueFindReplace("gml_GlobalScript_scr_spelltext", "if (cancelattack == 1)", @"
        if (global.monstertype[star] == 2) {
            global.msg[0] = scr_84_get_subst_string(""* ~1 spared ~2^2!&* But Lancer's bike cannot be stopped.../%"", global.charname[global.char[caster]], global.monstername[star]);
        }
        if (cancelattack == 1)
    ");
}
if (ch_no == 0) {
    importGroup.QueueFindReplace("gml_GlobalScript_scr_spell_ch1", "if (global.monstertype[star] != 3)", @"
        if (global.monstertype[star] == 2) {
            // do nothing
        }
        else if (global.monstertype[star] != 3)
    ");
    importGroup.QueueFindReplace("gml_GlobalScript_scr_spelltext_ch1", "if (cancelattack == 1)", @"
        if (global.monstertype[star] == 2) {
            global.msg[0] = scr_84_get_subst_string(""* ~1 spared ~2^2!&* But Lancer's bike cannot be stopped.../%"", global.charname[global.char[caster]], global.monstername[star]);
        }
        if (cancelattack == 1)
    ");
}
if (ch_no == 2 || ch_no == 0) {
    importGroup.QueueFindReplace("gml_Object_obj_berdlyplug_enemy_Alarm_0", "var mercyset = ceil(bardlymercy);", "var mercyset = ceil(global.diff_mercy * bardlymercy);");
    importGroup.QueueRegexFindReplace("gml_Object_obj_queen_enemy_Step_0", "mercyset = ([0-9]+);", "mercyset =  ceil(global.diff_mercy * ($1));");

    string[] sneoMercies = {"gml_Object_obj_sneo_wireheart_old_Draw_0", "gml_Object_obj_sneo_wireheart_edit_Draw_0", "gml_Object_obj_sneo_hitdetector_Collision_obj_yheart_shot",
        "gml_Object_obj_sneo_bigshot_Destroy_0", "gml_Object_obj_sneo_wireheart_Draw_0", "gml_Object_obj_spamton_neo_enemy_Step_2"};
    foreach (string sneoMercy in sneoMercies)
    {
        importGroup.QueueRegexFindReplace(sneoMercy, "obj_sneo_bulletcontroller.mercyaccumulated \\+= ([0-9]+);", "obj_sneo_bulletcontroller.mercyaccumulated += ceil(global.diff_mercy * ($1));");
        importGroup.QueueRegexFindReplace(sneoMercy, "__mercydmgwriter.damage = ([0-9]+);", "__mercydmgwriter.damage = ceil(global.diff_mercy * ($1));");
    }
}
if (ch_no == 3) {
    importGroup.QueueRegexFindReplace("gml_Object_obj_shadowman_enemy_Step_0", "obj_dmgwriter_boogie\\.damage \\+= ([0-9]+);", "obj_dmgwriter_boogie.damage += ceil(global.diff_mercy * ($1));");
    importGroup.QueueRegexFindReplace("gml_Object_obj_shadowman_enemy_Step_0", "global\\.mercymod\\[myself\\] \\+= ([0-9]+);", "global.mercymod[myself] += ceil(global.diff_mercy * ($1));");
    importGroup.QueueRegexFindReplace("gml_Object_obj_shadowman_enemy_Step_0", "__mercydmgwriter\\.damage = ([0-9]+);", "__mercydmgwriter.damage = ceil(global.diff_mercy * ($1));");
}
if (ch_no == 4) {
    importGroup.QueueFindReplace("gml_Object_obj_organ_enemy_vertical_pillar_Other_15", "if ((global.mercymod[myself] + mercygiven) > 100)", @"
        mercygiven = ceil(global.diff_mercy * mercygiven)
        if ((global.mercymod[myself] + mercygiven) > 100)
    ");
    importGroup.QueueFindReplace("gml_Object_obj_organ_enemy_Step_0", "global.mercymod[myself] += mercygained;", "global.mercymod[myself] += ceil(global.diff_mercy * mercygained);");
    importGroup.QueueFindReplace("gml_Object_obj_balthizard_enemy_Step_0", "obj_dmgwriter.damage += mercygained;", "obj_dmgwriter.damage += ceil(global.diff_mercy * mercygained);");
    importGroup.QueueFindReplace("gml_Object_obj_balthizard_enemy_Step_0", "global.mercymod[myself] += mercygained;", "global.mercymod[myself] += ceil(global.diff_mercy * mercygained);");
    importGroup.QueueFindReplace("gml_Object_obj_balthizard_enemy_Step_0", "shakemaxmercyhp -= mercygained;", "shakemaxmercyhp -= ceil(global.diff_mercy * mercygained);");
}
if (ch_no == 5) {

    string one_over_mercy = "(global.diff_mercy <= 0 ? 1 : (1/global.diff_mercy))";
    importGroup.QueueFindReplace("gml_Object_obj_trashy_hoop_Create_0", "damage += arg0;", "damage += ceil(global.diff_mercy * arg0);");
    importGroup.QueueFindReplace("gml_Object_obj_trashy_hoop_Create_0", "99, arg0);", "99, ceil(global.diff_mercy * arg0));");
    importGroup.QueueFindReplace("gml_Object_obj_trashy_broom_Step_0", "damage++;", "damage += ceil(global.diff_mercy);");
    importGroup.QueueFindReplace("gml_Object_obj_trashy_broom_Step_0", "99, 1);", "99, ceil(global.diff_mercy));");
    importGroup.QueueFindReplace("gml_Object_obj_flowery_enemy_Step_0", "global.mercymod[myself] += 10;", "global.mercymod[myself] += ceil(global.diff_mercy * 10);");
    importGroup.QueueFindReplace("gml_Object_obj_yellow_trial_manager_Create_0", "global.mercymod[myself] += 10;", "global.mercymod[myself] += ceil(global.diff_mercy * 10);");
    importGroup.QueueFindReplace("gml_Object_obj_monster1_Step_0", "global.mercymod[myself] += 120;", "global.mercymod[myself] += ceil(global.diff_mercy * 120);");
    importGroup.QueueFindReplace("gml_Object_obj_placeholderenemy_Step_0", "global.mercymod[myself] += 200;", "global.mercymod[myself] += ceil(global.diff_mercy * 200);");
    // convert mercy to doki
    importGroup.QueueFindReplace("gml_GlobalScript_scr_dokiadd", "arg1;", "ceil(global.diff_mercy * arg1);");
    importGroup.QueueFindReplace("gml_GlobalScript_scr_dokiadd", "arg1 <", "ceil(global.diff_mercy * arg1) <");
    importGroup.QueueFindReplace("gml_Object_obj_pink_enemy_Step_0", "scr_mercyadd(myself, 25);", $"scr_mercyadd(myself, {one_over_mercy} * 25);");
    // prevent final flowery softlock
    importGroup.QueueFindReplace("gml_Object_obj_flowery_enemy_Step_0", " && global.mercymod[myself] < 20", "");
    importGroup.QueueFindReplace("gml_Object_obj_flowery_enemy_Step_0", " && global.mercymod[myself] < 30", "");
    importGroup.QueueFindReplace("gml_Object_obj_flowery_enemy_Step_0", " && global.mercymod[myself] < 40", "");
    importGroup.QueueFindReplace("gml_Object_obj_flowery_enemy_Step_0", " && global.mercymod[myself] < 50", "");
    importGroup.QueueFindReplace("gml_Object_obj_flowery_enemy_Step_0", " && global.mercymod[myself] < 60", "");
}

// Apply Player Damage
{
    importGroup.QueueRegexFindReplace("gml_GlobalScript_scr_damage_enemy", "arg1(?!\\))", "ceil(global.diff_plrdmg * arg1)");
}
if (ch_no == 0) {
    importGroup.QueueRegexFindReplace("gml_GlobalScript_scr_damage_enemy_ch1", "arg1(?!\\))", "ceil(global.diff_plrdmg * arg1)");
}
if (ch_no >= 0 && ch_no <= 3) {
    importGroup.QueueFindReplace($"gml_Object_obj_heroparent_{((ch_no >= 0 && ch_no <= 2) ? "Alarm_1" : "Other_10")}", "dm.damage = damage;", @"
        damage = ceil(global.diff_plrdmg * damage);
        dm.damage = damage;
    ");
}
if (ch_no == 0) {
    importGroup.QueueFindReplace($"gml_Object_obj_heroparent_ch1_Alarm_1", "dm.damage = damage;", @"
        damage = ceil(global.diff_plrdmg * damage);
        dm.damage = damage;
    ");
}
if (ch_no == 4) {
    importGroup.QueueFindReplace($"gml_Object_obj_heroparent_Step_0", "global.monsterinstance[global.chartarget[myself]].hurtamt = damage;",
        "global.monsterinstance[global.chartarget[myself]].hurtamt = ceil(global.diff_plrdmg * damage);");
}
if (ch_no >= 5) {
    importGroup.QueueFindReplace($"gml_Object_obj_heroparent_Step_0", "dm.damage = damage;", @"
        damage = ceil(global.diff_plrdmg * damage);
        dm.damage = damage;
    ");
}
if (ch_no == 2 || ch_no == 0) {
    importGroup.QueueFindReplace("gml_Object_obj_sneo_wireheart_old_Draw_0", "global.monsterhp[0] -= ceil(global.monstermaxhp[0] * 0.03);",
        "global.monsterhp[0] -= ceil(global.diff_plrdmg * global.monstermaxhp[0] * 0.03);");
    importGroup.QueueFindReplace("gml_Object_obj_sneo_wireheart_edit_Draw_0", "global.monsterhp[0] -= ceil(global.monstermaxhp[0] * 0.03);",
        "global.monsterhp[0] -= ceil(global.diff_plrdmg * global.monstermaxhp[0] * 0.03);");
    importGroup.QueueFindReplace("gml_Object_obj_sneo_hitdetector_Collision_obj_yheart_shot", "if ((global.monsterhp[obj_spamton_neo_enemy.myself] - dmg) < 1)", @"
        dmg = ceil(global.diff_plrdmg * dmg);
        if ((global.monsterhp[obj_spamton_neo_enemy.myself] - dmg) < 1)
    ");
    importGroup.QueueFindReplace("gml_Object_obj_sneo_bigshot_Destroy_0", "global.monsterhp[0] -= ceil(global.monstermaxhp[0] * 0.05);",
        "global.monsterhp[0] -= ceil(global.diff_plrdmg * global.monstermaxhp[0] * 0.05);");
    importGroup.QueueFindReplace("gml_Object_obj_sneo_wireheart_Draw_0", "global.monsterhp[0] -= ceil(global.monstermaxhp[0] * 0.03);",
        "global.monsterhp[0] -= ceil(global.diff_plrdmg * global.monstermaxhp[0] * 0.03);");
}
// fix susie vs. lancer softlock
if (ch_no == 1 || ch_no == 0) {
    importGroup.QueueFindReplace("gml_GlobalScript_scr_monstersetup", "global.monstermaxhp[myself] = 2400;", "global.monstermaxhp[myself] = ceil(global.diff_plrdmg * 2400);");
    importGroup.QueueFindReplace("gml_GlobalScript_scr_monstersetup", "global.monsterhp[myself] = 2400;", "global.monsterhp[myself] = ceil(global.diff_plrdmg * 2400);");
    if (ch_no == 0) {
        importGroup.QueueFindReplace("gml_GlobalScript_scr_monstersetup_ch1", "global.monstermaxhp[myself] = 2400;", "global.monstermaxhp[myself] = ceil(global.diff_plrdmg * 2400);");
        importGroup.QueueFindReplace("gml_GlobalScript_scr_monstersetup_ch1", "global.monsterhp[myself] = 2400;", "global.monsterhp[myself] = ceil(global.diff_plrdmg * 2400);");
    }
}

// Apply Game Board Player Damage
if (ch_no == 3)
{
    const string gmbrdplrdmg = "(global.diff_gmbrdplrdmg < 0 ? global.diff_plrdmg : global.diff_gmbrdplrdmg)";
    string[] damDam = {"gml_Object_obj_board_hazard_Step_0", "gml_Object_obj_board_cactus_test_Step_0", "gml_Object_obj_board_npc_Step_0", "gml_Object_obj_board_npc_nosolid_Step_0",
        "gml_Object_obj_board_cactus_Step_0", "gml_Object_obj_board_fern_Step_0"};
    foreach (string dam in damDam)
    {
        importGroup.QueueTrimmedLinesFindReplace(dam, "myhealth--;", $"myhealth -= {gmbrdplrdmg};");
    }
    string[] zeroHps = {"gml_Object_obj_board_hazard_Step_0", "gml_Object_obj_board_cactus_test_Step_0"};
    foreach (string zeroHp in zeroHps)
    {
        importGroup.QueueFindReplace(zeroHp, "(myhealth == 0)", "(myhealth <= 0)");
    }
}

// Apply Player Healing
{
    importGroup.QueueRegexFindReplace("gml_GlobalScript_scr_spell", "scr_heal\\((\\S+), healnum\\);", "scr_heal($1, ceil(global.diff_plrheal * healnum));");
}
if (ch_no == 0) {
    importGroup.QueueRegexFindReplace("gml_GlobalScript_scr_spell_ch1", "scr_heal_ch1\\((\\S+), healnum\\);", "scr_heal_ch1($1, ceil(global.diff_plrheal * healnum));");
}
if (ch_no >= 5) {
    importGroup.QueueFindReplace("gml_GlobalScript_scr_spell", "scr_heal(0, scr_heal_amount_modify_by_equipment(5));", "scr_heal(0, ceil(global.diff_plrheal * scr_heal_amount_modify_by_equipment(5)));");
}

// Apply Game Board Player Healing
if (ch_no == 3)
{
    const string gmbrdplrheal = "(global.diff_gmbrdplrheal < 0 ? global.diff_plrheal : global.diff_gmbrdplrheal)";
    importGroup.QueueFindReplace("gml_Object_obj_board_heal_pickup_Step_0", "myhealth += 2",
        $"myhealth += 2 * {gmbrdplrheal};");
}

// Apply SAVE Point Heal
{
    importGroup.QueueFindReplace("gml_Object_obj_savepoint_Other_10", "if (global.hp[i] < global.maxhp[i])", "if (global.diff_saveheal && global.hp[i] < global.maxhp[i])");
}
if (ch_no == 0) {
    importGroup.QueueFindReplace("gml_Object_obj_savepoint_ch1_Other_10", "if (global.hp[i] < global.maxhp[i])", "if (global.diff_saveheal && global.hp[i] < global.maxhp[i])");
}

// Finish edit
importGroup.Import();

// No throw code edits
importGroup = new(Data){
    ThrowOnNoOpFindReplace = false
};

// Enemy Cooldowns
string one_over_cd = "(global.diff_enemycd <= 0 ? 1 : (1/global.diff_enemycd))";
// some attacks rely on the heart existing so have it fly out onto the box sooner
importGroup.QueueFindReplace("gml_Object_obj_moveheart_Create_0", "flytime = 8", "flytime = min(8, floor(global.diff_enemycd * 8))");
if (ch_no == 1)
{
    importGroup.QueueFindReplace("gml_Object_obj_moveheart_Create_0", "alarm[0] = flytime;", @"
        if (flytime > 0)
        {
            alarm[0] = flytime;
        }
        else
        {
            x = distx;
            y = disty;
            instance_create(x, y, obj_heart);
            instance_destroy();
        }
    ");
}
if (ch_no == 2 || ch_no == 0)
{
    importGroup.QueueFindReplace("gml_Object_obj_moveheart_Create_0", "alarm[0] = flytime;", @"
        if (flytime > 0)
        {
            alarm[0] = flytime;
        }
        else
        {
            x = distx;
            y = disty;

            if (!i_ex(obj_heart))
                instance_create(x, y, obj_heart);

            instance_destroy();
        }
    ");
}
if (ch_no == 3)
{
    importGroup.QueueFindReplace("gml_Object_obj_moveheart_Create_0", "alarm[0] = flytime;", @"
        if (flytime > 0)
        {
            alarm[0] = flytime;
        }
        else
        {
            x = distx;
            y = disty;

            if (!i_ex(obj_heart))
            {
                heart = instance_create(x, y, obj_heart);
                heart.sprite_index = sprite_index;
                heart.mask_index = mask_index;
            }

            instance_destroy();
        }
    ");
}
if (ch_no == 4)
{
    importGroup.QueueFindReplace("gml_Object_obj_moveheart_Create_0", "alarm[0] = flytime;", @"
        if (flytime > 0)
        {
            alarm[0] = flytime;
        }
        else
        {
            x = distx;
            y = disty;

            if (!i_ex(obj_heart))
            {
                heart = instance_create(x, y, obj_heart);

                if (i_ex(obj_jackenstein_enemy))
                    sprite_index = spr_dodgeheart_small;
            }

            instance_destroy();
        }
    ");
}
if (ch_no == 5)
{
    importGroup.QueueFindReplace("gml_Object_obj_moveheart_Create_0", "alarm[0] = flytime;", @"
        if (flytime > 0)
        {
            alarm[0] = flytime;
        }
        else
        {
            x = distx;
            y = disty;

            if (orange == true)
                instance_create_depth(x, y, -99999999, obj_flowery_act_menu_heart);
            else if (!i_ex(obj_heart))
                instance_create(x, y, obj_heart);

            instance_destroy();
        }
    ");
}
importGroup.QueueFindReplace("gml_Object_obj_moveheart_Step_0", "image_alpha += 0.334;", $"image_alpha += max(0.334, {one_over_cd} * 0.334)");
if (ch_no == 0)
{
    importGroup.QueueFindReplace("gml_Object_obj_moveheart_ch1_Create_0", "flytime = 8", "flytime = min(8, floor(global.diff_enemycd * 8))");
    importGroup.QueueFindReplace("gml_Object_obj_moveheart_ch1_Create_0", "alarm[0] = flytime;", @"
        if (flytime > 0)
        {
            alarm[0] = flytime;
        }
        else
        {
            x = distx;
            y = disty;
            instance_create_ch1(x, y, obj_heart_ch1);
            instance_destroy();
        }
    ");
    importGroup.QueueFindReplace("gml_Object_obj_moveheart_ch1_Step_0", "image_alpha += 0.334;", $"image_alpha += max(0.334, {one_over_cd} * 0.334)");
}

string[] bulletCons = {"gml_Object_obj_dbulletcontroller"};
if (ch_no == 0) {
    string[] demoBulletCons = {"gml_Object_obj_lancerbike_ch1", "gml_Object_obj_dbulletcontroller_ch1", "gml_Object_obj_chain_of_hell_ch1",
    "gml_Object_obj_finalchain_ch1", "gml_Object_obj_king_boss_ch1"};
    bulletCons = bulletCons.Concat(demoBulletCons).ToArray();
}
if (ch_no == 1) {
    string[] ch1BulletCons = {"gml_Object_obj_chain_of_hell", "gml_Object_obj_finalchain", "gml_Object_obj_king_boss"};
    bulletCons = bulletCons.Concat(ch1BulletCons).ToArray();
}
if (ch_no >= 0 && ch_no <= 2) {
    string[] ch1to2BulletCons = {"gml_Object_obj_lancerbike"};
    bulletCons = bulletCons.Concat(ch1to2BulletCons).ToArray();
}
if (ch_no >= 2 || ch_no == 0) {
    string[] ch2upBulletCons = {"gml_Object_obj_dojograzeenemy"};
    bulletCons = bulletCons.Concat(ch2upBulletCons).ToArray();
}
if (ch_no == 2 || ch_no == 0) {
    string[] ch2BulletCons = {"gml_Object_obj_queen_bulletcontroller", "gml_Object_obj_sneo_bulletcontroller", "gml_Object_obj_thrash_swordattack",
    "gml_Object_obj_thrash_laserattack", "gml_Object_obj_thrash_duck_attack", "gml_Object_obj_swatchling_bulletcontroller", "gml_Object_obj_swatchling_shockwave_maker",
    "gml_Object_obj_spamton_attack_mode", "gml_Object_obj_sneo_phoneshooter", "gml_Object_obj_sneo_phonehand_master", "gml_Object_obj_sneo_phonehand",
    "gml_Object_obj_thrash_flameattack", "gml_Object_obj_maus_liddle", "gml_Object_obj_sneo_bulletcontroller_somn", "gml_Object_obj_pipis_enemy", "gml_Object_obj_spamton_neo_enemy"};
    bulletCons = bulletCons.Concat(ch2BulletCons).ToArray();
}
if (ch_no == 3) {
    string[] ch3BulletCons = {"gml_Object_obj_shutta_rotation_attack"};
    bulletCons = bulletCons.Concat(ch3BulletCons).ToArray();
}
if (ch_no == 4) {
    string[] ch4BulletCons = {"gml_Object_obj_encounter_guei_alt"};
    bulletCons = bulletCons.Concat(ch4BulletCons).ToArray();
}
if (ch_no == 5) {
    string[] ch5BulletCons = {"gml_Object_obj_attack_blue_ponddancing_Draw_0"};
    bulletCons = bulletCons.Concat(ch5BulletCons).ToArray();
}
string[] btimerEquals = {"99"};
if (ch_no == 1 || ch_no == 0) {
    string[] ch1BtimerEquals = {"20", "10", "-8"};
    btimerEquals = btimerEquals.Concat(ch1BtimerEquals).ToArray();
}
if (ch_no >= 2 || ch_no == 0) {
    string[] ch2upBtimerEquals = {"attacktimer - 10", "135", "-45", "-40", "-20"};
    btimerEquals = btimerEquals.Concat(ch2upBtimerEquals).ToArray();
}
if (ch_no == 2 || ch_no == 0) {
    string[] ch2BtimerEquals = {"35 - random(30)", "random_range(6, 18) * ratio * (1 + difficulty)", "12 * ratio", "30 * ratio * (1 - (sameattacker / sameattack))",
    "(pattern && difficulty == 0) ? 10 : 0", "irandom(20 * ratio)", "irandom(10)", "firingspeed - irandom(10)", "-120", "999", "120", "115", "100", "-35",
    "-30", "-10", "70", "60", "55", "45", "44", "40", "36", "30", "20", "15", "12", "10", "9", "5", "3", "2"};
    btimerEquals = btimerEquals.Concat(ch2BtimerEquals).ToArray();
}
if (ch_no == 3) {
    string[] ch3BtimerEquals = {"(40 * ratio) - (sameattacker * 5)", "-12", "88", "50", "25", "18", "10"};
    btimerEquals = btimerEquals.Concat(ch3BtimerEquals).ToArray();
}
if (ch_no == 4) {
    string[] ch4BtimerEquals = {"(5 + (35 * ratio) + (10 * (spell == 0 && ratio == 1.5))) - (24 * (spell == 0 && ratio == 2.3)) - (8 * sameattacker)", "0 - irandom(10)",
    "-40 - irandom(30)", "starttime - 1", "100", "88", "-50", "40"};
    btimerEquals = btimerEquals.Concat(ch4BtimerEquals).ToArray();
}
if (ch_no == 5) {
    string[] ch5BtimerEquals = {"ceil(24 * ratio) - 1", "irandom(1500)", "irandom(150)", "-10", "-180", "32", "-999", "40 - floor(0.5 + (40 * _binterval))", "brate - 1"};
    btimerEquals = btimerEquals.Concat(ch5BtimerEquals).ToArray();
}
string[] btimerEqualEquals = {};
if (ch_no == 1 || ch_no == 0) {
    string[] ch1BtimerEqualEquals = {"10"};
    btimerEqualEquals = btimerEqualEquals.Concat(ch1BtimerEqualEquals).ToArray();
}
if (ch_no == 2 || ch_no == 0) {
    string[] ch2BtimerEqualEquals = {"(difficulty ? 8 : 15)", "350", "310", "270", "260", "240", "200", "192", "190", "180", "170", "150", "140", "130", "120", "115", "110", "100", "99", "90", "70",
    "45", "40", "30", "28", "25", "20", "13", "2", "1"};
    btimerEqualEquals = btimerEqualEquals.Concat(ch2BtimerEqualEquals).ToArray();
}
if (ch_no == 3) {
    string[] ch3BtimerEqualEquals = {"1260", "1150", "1000", "900", "816", "810", "720", "700", "650", "630", "560", "540", "530", "500", "480", "450", "440", "420", "410", "400", "350", "346", "325",
    "320", "300", "290", "265", "260", "230", "210", "200", "180", "125", "117", "115", "110", "103", "90", "86", "60", "50", "40", "20", "17", "13", "10", "3"};
    btimerEqualEquals = btimerEqualEquals.Concat(ch3BtimerEqualEquals).ToArray();
}
if (ch_no == 4) {
    string[] ch4BtimerEqualEquals = {"(starttime - 1)", "59", "35"};
    btimerEqualEquals = btimerEqualEquals.Concat(ch4BtimerEqualEquals).ToArray();
}
if (ch_no == 5) {
    string[] ch5BtimerEqualEquals = {"-6", "-173", "109", "119"};
    btimerEqualEquals = btimerEqualEquals.Concat(ch5BtimerEqualEquals).ToArray();
}
(string A, string B) [] btimerModulos = {};
if (ch_no == 5) {
    (string A, string B) [] ch5BtimerModulos = {
        ("ceil(31 * power(ratio, 1.28))", "(25 * sameattacker)"), ("ceil(24 * ratio)", "(4 * sameattacker)"), ("(6 * ceil(ratio))", "0"), ("ceil(27 * ratio)", "(20 * sameattacker)"),
        ("((3 + ceil(18 * ratio)) - (5 * (ratio == 1)))", "(3 * sameattacker)"), ("ceil(59 * ratio)", "(30 * sameattacker)"), ("40", "20"), ("60", "0"), ("_rate", "0"),
        ("ceil(31 * power(ratio, 1.28))", "(25 * sameattacker)")};
    btimerModulos = btimerModulos.Concat(ch5BtimerModulos).ToArray();
}
foreach (string con in bulletCons)
{
    foreach (string term in btimerEquals)
    {
        importGroup.QueueFindReplace(con + "_Create_0", $"btimer = {term}", $"btimer = floor(global.diff_enemycd * ({term}))");
        importGroup.QueueFindReplace(con + "_Step_0", $"btimer = {term}", $"btimer = floor(global.diff_enemycd * ({term}))");
        importGroup.QueueFindReplace(con + "_Other_0", $"btimer = {term}", $"btimer = floor(global.diff_enemycd * ({term}))");
        importGroup.QueueFindReplace(con + "_Other_10", $"btimer = {term}", $"btimer = floor(global.diff_enemycd * ({term}))");
        importGroup.QueueFindReplace(con + "_Other_15", $"btimer = {term}", $"btimer = floor(global.diff_enemycd * ({term}))");
    }

    importGroup.QueueFindReplace(con + "_Step_0", "btimer < ", "btimer < global.diff_enemycd * ");
    importGroup.QueueFindReplace(con + "_Step_0", "btimer > ", "btimer > global.diff_enemycd * ");
    importGroup.QueueFindReplace(con + "_Step_0", "btimer <= ", "btimer <= global.diff_enemycd * ");
    importGroup.QueueFindReplace(con + "_Step_0", "btimer >= ", "btimer >= global.diff_enemycd * ");
    foreach (string term in btimerEqualEquals)
    {
        importGroup.QueueFindReplace(con + "_Step_0", $"btimer == {term}", $"btimer == ceil(global.diff_enemycd * ({term}))");
    }
    if (ch_no != 2)
    {
        // TODO -= swatchling & sneo in ch2
        // TODO % weather duo in ch3
        // TODO % mizzle/watercoolers in ch4
        importGroup.QueueRegexFindReplace(con + "_Step_0", "btimer -= ([^;]+)", "btimer -= ceil(global.diff_enemycd * $1)");
        foreach ((string A, string B) terms in btimerModulos)
        {
            importGroup.QueueFindReplace(con + "_Step_0", $"(btimer % {terms.A}) == {terms.B}", $"(btimer % ceil(global.diff_enemycd * ({terms.A}))) == floor(global.diff_enemycd * {terms.B})");
        }
    }
}
if (ch_no == 1 || ch_no == 0) {
    string[] ch_postfixes = {""};
    if (ch_no == 0) {
        string[] demo_postfixes = {"_ch1"};
        ch_postfixes = ch_postfixes.Concat(demo_postfixes).ToArray();
    }
    foreach (string postfix in ch_postfixes)
    {
        // Reduce randomness for lower cooldowns as heart shaper can trap you.
        importGroup.QueueFindReplace($"gml_Object_obj_dbulletcontroller{postfix}_Step_0", "(obj_battlesolid.x - 50) + random(100)",
            "(obj_battlesolid.x - 25 - 25 * min(1, global.diff_enemycd)) + random(50 + 50 * min(1, global.diff_enemycd))");
        importGroup.QueueFindReplace($"gml_Object_obj_dbulletcontroller{postfix}_Step_0", "(obj_battlesolid.y - 50) + random(100)",
            "(obj_battlesolid.y - 25 - 25 * min(1, global.diff_enemycd)) + random(50 + 50 * min(1, global.diff_enemycd))");
    }

    // King's wave chain spades become undodgeable below a certain value.
    importGroup.QueueFindReplace($"gml_Object_obj_wavechain{(ch_no == 0 ? "_ch1" : "")}_Step_0", "btimer >= 20", "btimer >= max(12, global.diff_enemycd * 20)");
    importGroup.QueueFindReplace($"gml_Object_obj_wavechain{(ch_no == 0 ? "_ch1" : "")}_Step_0", "btimer >= 18", "btimer >= max(12, global.diff_enemycd * 18)");
    importGroup.QueueFindReplace($"gml_Object_obj_wavechain{(ch_no == 0 ? "_ch1" : "")}_Step_0", "btimer >= 16", "btimer >= max(12, global.diff_enemycd * 16)");
    importGroup.QueueFindReplace($"gml_Object_obj_wavechain{(ch_no == 0 ? "_ch1" : "")}_Step_0", "btimer >= 14", "btimer >= max(12, global.diff_enemycd * 14)");
}
string[] dojoCons = {"gml_Object_obj_dbullet_maker"};
if (ch_no == 0) {
    string[] demoDojoCons = {"gml_Object_obj_dbullet_maker_ch1"};
    dojoCons = dojoCons.Concat(demoDojoCons).ToArray();
}
if (ch_no >= 2 || ch_no == 0) {
    string[] ch2upDojoCons = {"gml_Object_obj_ch2_dojo_puzzlebullet_maker"};
    dojoCons = dojoCons.Concat(ch2upDojoCons).ToArray();
}
if (ch_no == 3) {
    string[] ch3DojoCons = {"gml_Object_obj_shadow_mantle_fire_controller", "gml_Object_obj_shadow_mantle_fire3"};
    dojoCons = dojoCons.Concat(ch3DojoCons).ToArray();
}
if (ch_no == 4) {
    string[] ch4DojoCons = {"gml_Object_obj_gerson_growtangle_telegraph_new"};
    dojoCons = dojoCons.Concat(ch4DojoCons).ToArray();
}
foreach (string con in dojoCons)
{
    importGroup.QueueFindReplace(con + "_Draw_0", "activetimer = timetarg - 1", "activetimer = floor(global.diff_enemycd * (timetarg - 1))");
    importGroup.QueueFindReplace(con + "_Step_0", "activetimer = 18", "activetimer = floor(global.diff_enemycd * (18))");
    importGroup.QueueFindReplace(con + "_Step_0", "activetimer = 10", "activetimer = floor(global.diff_enemycd * (10))");
    importGroup.QueueFindReplace(con + "_Create_0", "activetimer = 20", "activetimer = floor(global.diff_enemycd * (20))");

    importGroup.QueueFindReplace(con + "_Draw_0", "activetimer >= ", "activetimer >= global.diff_enemycd * ");
    importGroup.QueueFindReplace(con + "_Draw_0", "activetimer == timetarg", "activetimer == ceil(global.diff_enemycd * timetarg)");
    importGroup.QueueFindReplace(con + "_Step_0", "activetimer == 4", "activetimer == ceil(global.diff_enemycd * 4)");
}

if (ch_no == 1 || ch_no == 0) {
    string[] ch_postfixes = {""};
    if (ch_no == 0) {
        string[] demo_postfixes = {"_ch1"};
        ch_postfixes = ch_postfixes.Concat(demo_postfixes).ToArray();
    }
    foreach (string postfix in ch_postfixes)
    {
        // include Lancer overworld attacks
        importGroup.QueueFindReplace($"gml_Object_obj_overworld_spademaker{postfix}_Create_0", "alarm[0] = 5", "alarm[0] = ceil(global.diff_enemycd * 5)");
        importGroup.QueueFindReplace($"gml_Object_obj_overworld_spademaker{postfix}_Alarm_0", "alarm[0] = 5", "alarm[0] = ceil(global.diff_enemycd * 5)");
        importGroup.QueueFindReplace($"gml_Object_obj_overworld_spademaker{postfix}_Alarm_0", "alarm[0] = 7", "alarm[0] = ceil(global.diff_enemycd * 7)");
        importGroup.QueueFindReplace($"gml_Object_obj_overworld_spademaker{postfix}_Alarm_0", "alarm[0] = 10", "alarm[0] = ceil(global.diff_enemycd * 10)");
        importGroup.QueueFindReplace($"gml_Object_obj_overworld_spademaker{postfix}_Alarm_0", "alarm[0] = alarmamt", "alarm[0] = ceil(global.diff_enemycd * alarmamt)");
        importGroup.QueueFindReplace($"gml_Object_obj_overworld_spademaker{postfix}_Alarm_0", "alarm[0] = 20 + (15 * slow_bonus)",
            "alarm[0] = ceil(global.diff_enemycd * (20 + (15 * slow_bonus)))");

        // include K.Round leap attacks
        importGroup.QueueFindReplace($"gml_Object_obj_checkers_leap{postfix}_Step_0", "jumptimer >= ", "jumptimer >= global.diff_enemycd * ");
        importGroup.QueueFindReplace($"gml_Object_obj_checkers_leap{postfix}_Step_0", "s_timer >= ", "s_timer >= global.diff_enemycd * ");
        importGroup.QueueFindReplace($"gml_Object_obj_checkers_leap{postfix}_Step_0", "s_timer == 20", "s_timer == ceil(global.diff_enemycd * 20)");
        importGroup.QueueFindReplace($"gml_Object_obj_checkers_leap{postfix}_Step_0", "image_xscale += ", $"image_xscale += {one_over_cd} * ");
        importGroup.QueueFindReplace($"gml_Object_obj_checkers_leap{postfix}_Step_0", "image_yscale += ", $"image_yscale += {one_over_cd} * ");
        importGroup.QueueFindReplace($"gml_Object_obj_checkers_leap{postfix}_Step_0", "jumptimer = 10", "jumptimer = floor(global.diff_enemycd * (10))");
        importGroup.QueueFindReplace($"gml_Object_obj_checkers_leap{postfix}_Step_0", "s_timer = 21", "s_timer = floor(global.diff_enemycd * (21))");

        // include star bird's overworld attacks
        importGroup.QueueFindReplace($"gml_Object_obj_starwalker_overworld{postfix}_Step_0", "attacktimer >= ", "attacktimer >= global.diff_enemycd * ");
        importGroup.QueueFindReplace($"gml_Object_obj_starwalker_overworld{postfix}_Step_0", "attacktimer = 36", "attacktimer = floor(global.diff_enemycd * (36))");
        importGroup.QueueFindReplace($"gml_Object_obj_starwalker_overworld{postfix}_Step_0", "attacktimer = 38", "attacktimer = floor(global.diff_enemycd * (38))");
        // fix star bullets dissappearing too soon
        importGroup.QueueFindReplace($"gml_Object_obj_starwalker_overworld{postfix}_Step_0", "if (shot == 1)", "if (false && shot == 1)");
        importGroup.QueueAppend("gml_Object_obj_starwalker_overworld_Create_0", "trackstarbullet = array_create(0);");
        importGroup.QueueFindReplace($"gml_Object_obj_starwalker_overworld{postfix}_Step_0", "starbullet[i].depth = 1000;", @"
            starbullet[i].depth = 1000;
            array_push(trackstarbullet, starbullet[i]);
        ");
        importGroup.QueueAppend($"gml_Object_obj_starwalker_overworld{postfix}_Step_0", @"
            var newtrackstarbullet = array_create(0);
            var cam = view_camera[0];
            var x1 = camera_get_view_x(cam);
            var y1 = camera_get_view_y(cam);
            var x2 = x1 + camera_get_view_width(cam);
            var y2 = y1 + camera_get_view_height(cam);
            for (var i = 0; i < array_length(trackstarbullet); i++) {
                with (trackstarbullet[i]) {
                    if(!point_in_rectangle( x, y, x1, y1, x2, y2))
                        instance_destroy();
                    else
                        array_push(newtrackstarbullet, self);
                }
            }
            trackstarbullet = newtrackstarbullet;
        ");
    }

    // King's chain of hell could trap you, shorten chain length sooner
    importGroup.QueueFindReplace($"gml_Object_obj_chain_of_hell{(ch_no == 0 ? "_ch1" : "")}_Step_0", "bullettimer >= 30", "bullettimer >= min(30, global.diff_enemycd * 30)");

    // include box drag chain's time to decide next path
    importGroup.QueueFindReplace($"gml_Object_obj_finalchain{(ch_no == 0 ? "_ch1" : "")}_Step_0", "gotimer >= ", "gotimer >= global.diff_enemycd * ");
}
if (ch_no == 2 || ch_no == 0) {
    // include hangplugs
    importGroup.QueueFindReplace("gml_Object_obj_hangplug_Create_0", "timer = 130 + random(20)", "timer = floor(global.diff_enemycd * (130 + random(20)))");
    importGroup.QueueFindReplace("gml_Object_obj_hangplug_Create_0", "timer -= ", "timer -= global.diff_enemycd * ");
    importGroup.QueueFindReplace("gml_Object_obj_hangplug_Step_0", "timerb == timerbtarget", "timerb == ceil(global.diff_enemycd * timerbtarget)");
    importGroup.QueueFindReplace("gml_Object_obj_hangplug_Step_0", "timer >= ", "timer >= global.diff_enemycd * ");
    importGroup.QueueFindReplace("gml_Object_obj_hangplug_Step_0", "timer = ", "timer = global.diff_enemycd * ");

    // include shoottimer guys: Spamton, Queen, Were(were)wire, Sweet Cap'n Cakes
    string[] shooterGuys = {"gml_Object_obj_sneo_crusher_chase_Create_0", "gml_Object_obj_sneo_crusher_chase_Step_0", "gml_Object_obj_werewire_enemy_Create_0", "gml_Object_obj_werewire_enemy_Step_0",
        "gml_Object_obj_werewerewire_enemy_Create_0", "gml_Object_obj_werewerewire_enemy_Step_0", "gml_Object_obj_queen_search_gun_Create_0", "gml_Object_obj_queen_search_gun_Step_0",
        "gml_Object_obj_sneo_heartattack_Create_0", "gml_Object_obj_sneo_heartattack_Alarm_0", "gml_Object_obj_sneo_heartattack_Step_0", "gml_Object_obj_sneo_heartattack_old_Create_0",
        "gml_Object_obj_sneo_heartattack_old_Step_0", "gml_Object_obj_musicenemy_dancer_Create_0", "gml_Object_obj_musicenemy_dancer_Draw_0", "gml_Object_obj_musicenemy_dancer_end_Create_0",
        "gml_Object_obj_musicenemy_dancer_end_Draw_0"};
    string[] shoottimerEquals = {"29", "10 / m", "20", "-10", "-headimage * 4"};
    string[] shoottimerEqualEquals = {"obj_spamton_neo_enemy.heart_1st_wave_timer", "obj_spamton_neo_enemy.heart_2nd_wave_timer", "obj_spamton_neo_enemy.heart_3rd_wave_timer", "2", "4", "6"};
    foreach (string scr in shooterGuys)
    {
        foreach (string term in shoottimerEquals)
        {
            importGroup.QueueFindReplace(scr, $"shoottimer = {term}", $"shoottimer = floor(global.diff_enemycd * ({term}))");
        }

        importGroup.QueueFindReplace(scr, "shoottimer < ", "shoottimer < global.diff_enemycd * ");
        importGroup.QueueFindReplace(scr, "shoottimer > ", "shoottimer > global.diff_enemycd * ");
        importGroup.QueueFindReplace(scr, "shoottimer <= ", "shoottimer <= global.diff_enemycd * ");
        importGroup.QueueFindReplace(scr, "shoottimer >= ", "shoottimer >= global.diff_enemycd * ");
        foreach (string term in shoottimerEqualEquals)
        {
            importGroup.QueueFindReplace(scr, $"shoottimer == {term}", $"shoottimer == ceil(global.diff_enemycd * ({term}))");
        }
    }

    // include hanging spark shooters: Were(were)wire and Berdly (Queen fight)
    string[] hangSparkers = {"gml_Object_obj_werewire_enemy_Step_0", "gml_Object_obj_werewerewire_enemy_Step_0", "gml_Object_obj_queen_berdlywireattack_Step_0"};
    foreach (string scr in hangSparkers)
    {
        importGroup.QueueFindReplace(scr, "shoottimer >= ", "shoottimer >= global.diff_enemycd * ");
    }

    // include Sweet Cap'n Cakes' Boombox
    importGroup.QueueFindReplace("gml_Object_obj_musicenemy_boombox_Create_0", "makelongtimer = 9", "makelongtimer = floor(global.diff_enemycd * 9)");
    importGroup.QueueFindReplace("gml_Object_obj_musicenemy_boombox_Step_0", "makelongtimer >= ", "makelongtimer >= global.diff_enemycd * ");

    // include Berdly's tornados (Queen fight)
    importGroup.QueueFindReplace("gml_Object_obj_berdly_tornadomaker_Step_0", "timer >= ", "timer >= global.diff_enemycd * ");

    // these legs gotta wait their turn
    importGroup.QueueFindReplace("gml_Object_obj_queen_bulletcontroller_Step_0", "if (stomplocation[0] == 1 && stomplocation[1] == 1 && stomplocation[2] == 1)", @"
        var waitDont = false;

        if (instance_number(obj_queen_leg) >= 3)
            waitDont = true;

        if (stomplocation[0] == 1 && stomplocation[1] == 1 && stomplocation[2] == 1)
    ");
    importGroup.QueueFindReplace("gml_Object_obj_queen_bulletcontroller_Step_0", "if (chooselocation == 0)", "if (!waitDont && chooselocation == 0)");
    importGroup.QueueFindReplace("gml_Object_obj_queen_bulletcontroller_Step_0", "if (chooselocation == 1)", "if (!waitDont && chooselocation == 1)");
    importGroup.QueueFindReplace("gml_Object_obj_queen_bulletcontroller_Step_0", "if (chooselocation == 2)", "if (!waitDont && chooselocation == 2)");
    importGroup.QueueFindReplace("gml_Object_obj_queen_bulletcontroller_Step_0", "d.pos = chooselocation;", @"
        if (!waitDont)
            d.pos = chooselocation;
    ");
    importGroup.QueueFindReplace("gml_Object_obj_queen_bulletcontroller_Step_0", "d.shootbullets = 1;", @"
        if (!waitDont)
            d.shootbullets = 1;
    ");
    importGroup.QueueFindReplace("gml_Object_obj_queen_bulletcontroller_Step_0", "btimer = floor(global.diff_enemycd * 9);", @"
        if (!waitDont)
            btimer = floor(global.diff_enemycd * 9);
    ");

    // include Spamton Neo blue heads
    importGroup.QueueFindReplace("gml_Object_obj_sneo_guymaker_Step_0", "timer >= ", "timer >= global.diff_enemycd * ");
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_guymaker_Step_0", "timer == ([0-9]+)", "timer == ceil(global.diff_enemycd * ($1))");

    // include Spamton Neo walls of mail
    string[] wallCons = {"gml_Object_obj_sneo_wall_controller_new_Create_0", "gml_Object_obj_sneo_wall_controller_new_Step_0", "gml_Object_obj_sneo_wall_controller_Create_0",
        "gml_Object_obj_sneo_wall_controller_Alarm_0", "gml_Object_obj_sneo_wall_controller_Step_0", "gml_Object_obj_sneo_wall_controller_Other_10"};
    foreach (string scrName in wallCons) {
        importGroup.QueueFindReplace(scrName, "timer >= ", "timer >= global.diff_enemycd * ");
        importGroup.QueueFindReplace(scrName, "timer -= ", "timer -= global.diff_enemycd * ");
        importGroup.QueueRegexFindReplace(scrName, "timer = ([^;]+)", "timer = floor(global.diff_enemycd * ($1))");
        importGroup.QueueRegexFindReplace(scrName, "timer == ([0-9|\\.]+)", "timer == ceil(global.diff_enemycd * ($1))");
        importGroup.QueueFindReplace(scrName, "timer == wallcreatetimer[wallcount]", "timer == ceil(global.diff_enemycd * (wallcreatetimer[wallcount]))");
        // TODO why won't this $@#! attack loop? (not too noticeable at 70%)
        // importGroup.QueueFindReplace(scrName, "wallcount < wallcountmax", "wallcount < ({one_over_cd} * wallcountmax)");
        // importGroup.QueueFindReplace(scrName, "made < spawncount", "made < ({one_over_cd} * spawncount)");
    }

    // include Spamton Neo wire heart
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_wireheart_Draw_0", "(?<!_|damage|turn)timer >(=?) ([0-9|\\.]+)", "timer >$1 global.diff_enemycd * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_wireheart_Step_0", "(?<!_|damage|turn)timer >(=?) ([0-9|\\.]+)", "timer >$1 global.diff_enemycd * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_wireheart_Step_0", "(?<!_|damage|turn)timer <(=?) ([0-9|\\.]+)", "timer <$1 global.diff_enemycd * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_wireheart_Step_0", "(?<!_|damage|turn)timer = ([^;]+)", "timer = floor(global.diff_enemycd * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_wireheart_Step_0", "(?<!_|damage|turn)timer == ([0-9|\\.]+)", "timer == ceil(global.diff_enemycd * ($1))");
    importGroup.QueueFindReplace("gml_Object_obj_sneo_wireheart_Step_0", "movetimer / 21", "movetimer / (global.diff_enemycd * 21)");

    // include Spamton Neo face attack
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_faceattack_Step_0", "(?<!_|explode|turn|shootflash)timer >(=?) ([0-9|\\.]+)", "timer >$1 global.diff_enemycd * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_faceattack_Step_0", "(?<!_|explode|turn|shootflash)timer == ([0-9|\\.]+)", "timer == ceil(global.diff_enemycd * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_faceattack_Step_0", "(?<!_|explode|turn|shootflash)timer = ([^;]+)", "timer = floor(global.diff_enemycd * ($1))");
    // fix attack patterns getting cutoff
    importGroup.QueueFindReplace("gml_Object_obj_sneo_faceattack_Step_0", "timer == ceil(global.diff_enemycd * 50)", "timer == (ceil(global.diff_enemycd * 30) + 20)");
    importGroup.QueueFindReplace("gml_Object_obj_sneo_faceattack_Step_0", "timer == ceil(global.diff_enemycd * 90)", "timer == (ceil(global.diff_enemycd * 80) + 10)");
    importGroup.QueueFindReplace("gml_Object_obj_sneo_faceattack_Step_0", "timer == ceil(global.diff_enemycd * 42)", "timer == (ceil(global.diff_enemycd * 10) + 32)");

    // include Spamton Neo phonecall attack
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_bulletcontroller_Step_0", "atimer == ([0-9|\\.]+)", "atimer == ceil(global.diff_enemycd * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_bulletcontroller_Step_0", "atimer >(=?) ([0-9|\\.]+)", "atimer >$1 global.diff_enemycd * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_sneo_bulletcontroller_Step_0", "atimer <(=?) ([0-9|\\.]+)", "atimer <$1 global.diff_enemycd * ($2)");
    importGroup.QueueFindReplace("gml_Object_obj_sneo_bulletcontroller_Step_0", "min(10, atimer) / 10", "min(10, atimer) / (global.diff_enemycd * 10)");
    importGroup.QueueFindReplace("gml_Object_obj_sneo_bulletcontroller_Step_0", "atimer / 20", "atimer / (global.diff_enemycd * 20)");
    importGroup.QueueFindReplace("gml_Object_obj_sneo_bulletcontroller_Step_0", "atimer >= threshold", "atimer >= global.diff_enemycd * threshold");
    importGroup.QueueRegexFindReplace("gml_Object_obj_pipis_controller_Draw_0", "(?<!_|move|turn)timer >(=?) ([0-9|\\.]+)", "timer >$1 global.diff_enemycd * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_pipis_controller_Draw_0", "(?<!_|move|turn)timer <(=?) ([0-9|\\.]+)", "timer <$1 global.diff_enemycd * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_pipis_controller_Draw_0", "(?<!_|move|turn)timer / 10", "timer / (global.diff_enemycd * 10)");
    importGroup.QueueFindReplace("gml_Object_obj_pipis_controller_Draw_0", "timer < (timervariance - 15)", "timer < (global.diff_enemycd * (timervariance - 15))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_pipis_controller_Draw_0", "(?<!_|move|turn)timer / (timervariance - 15)", "timer / (global.diff_enemycd * (timervariance - 15))");
    importGroup.QueueFindReplace("gml_Object_obj_pipis_controller_Draw_0", "timer == (timervariance - 15)", "timer == ceil(global.diff_enemycd * (timervariance - 15))");
    importGroup.QueueFindReplace("gml_Object_obj_pipis_controller_Draw_0", "timer >= timervariance", "timer >= global.diff_enemycd * timervariance");
    importGroup.QueueFindReplace("gml_Object_obj_pipis_controller_Draw_0", "timer = -40", "timer = floor(global.diff_enemycd * -40)");
    importGroup.QueueFindReplace("gml_Object_obj_pipis_controller_Draw_0", "timer2 == 40", "timer2 == ceil(global.diff_enemycd * 40)");
    importGroup.QueueFindReplace("gml_Object_obj_pipis_controller_Draw_0", "timer2 >= 50", "timer2 >= global.diff_enemycd * 50");
    importGroup.QueueFindReplace("gml_Object_obj_pipis_controller_Draw_0", "timer3 == 150", "timer3 == ceil(global.diff_enemycd * 150)");
    // TODO why won't this $@#! attack loop? (not too noticeable at 70%)
    // importGroup.QueueFindReplace("gml_Object_obj_pipis_controller_Draw_0", "pipiscount >= maxpipis", "pipiscount >= ({one_over_cd} * maxpipis)");

    // include Spamton Neo big shots
    importGroup.QueueFindReplace("gml_Object_obj_spamton_neo_enemy_Draw_0", "dance_timer == (17 - (fastshot * 10))", "dance_timer == ceil(global.diff_enemycd * (17 - (fastshot * 10)))");
    importGroup.QueueFindReplace("gml_Object_obj_spamton_neo_enemy_Draw_0", "dance_timer == (52 - (fastshot * 10))", "dance_timer == ceil(global.diff_enemycd * (52 - (fastshot * 10)))");
    importGroup.QueueFindReplace("gml_Object_obj_spamton_neo_enemy_Draw_0", "dance_timer == (42 - (fastshot * 10))", "dance_timer == ceil(global.diff_enemycd * (42 - (fastshot * 10)))");
    importGroup.QueueFindReplace("gml_Object_obj_spamton_neo_enemy_Draw_0", "dance_timer == (84 - (fastshot * 20))", "dance_timer == ceil(global.diff_enemycd * (84 - (fastshot * 20)))");
    importGroup.QueueFindReplace("gml_Object_obj_spamton_neo_enemy_Draw_0", "dance_timer == (85 - (fastshot * 20))", "dance_timer == ceil(global.diff_enemycd * (85 - (fastshot * 20)))");
    importGroup.QueueFindReplace("gml_Object_obj_spamton_neo_enemy_Draw_0", "dance_timer > 9", "dance_timer > global.diff_enemycd * 9");
    importGroup.QueueFindReplace("gml_Object_obj_spamton_neo_enemy_Draw_0", "dance_timer = 3", "dance_timer = floor(global.diff_enemycd * 3)");
}
if (ch_no == 3) {
    // include shadowman tommy gun
    importGroup.QueueRegexFindReplace("gml_Object_obj_shadowman_tommygun_Step_0", "(?<!turn)timer <(=?) ([0-9|\\.]+)", "timer <$1 global.diff_enemycd * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_shadowman_tommygun_Step_0", "(?<!_|turn)timer == ([0-9|\\.]+)", "timer == ceil(global.diff_enemycd * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_shadowman_tommygun_Step_0", "bullet_timer (\\+?-?)= ([^;]+)", "bullet_timer $1= floor(global.diff_enemycd * ($2))");

    // include Lanino & Elnina
    importGroup.QueueFindReplace("gml_Object_obj_elnina_mascotattack_Step_0", "shottimer[i] >= shotrate[i]", "shottimer[i] >= global.diff_enemycd * shotrate[i]");

    // include water cooler
    importGroup.QueueFindReplace("gml_Object_obj_watercooler_bullet_rainball_Step_0", "timer >= threshold", "timer >= (global.diff_enemycd * threshold)");

    // include zaper
    importGroup.QueueRegexFindReplace("gml_Object_obj_zapper_laser_manager_Alarm_0", "alarm\\[(0|1)\\] (\\+?-?)= ([^;]+)", "alarm[$1] $2= ceil(global.diff_enemycd * ($3))");

    // Fix shutta crash
    importGroup.QueueFindReplace("gml_Object_obj_shutta_rotation_attack_Other_10", "other.afterimage_count", "min(other.afterimage_count, array_length(afterimage))");
    importGroup.QueueFindReplace("gml_Object_obj_shutta_rotation_attack_Other_11", "other.afterimage_count", "min(other.afterimage_count, array_length(afterimage))");
    importGroup.QueueFindReplace("gml_Object_obj_shutta_rotation_attack_Other_12", "other.afterimage_count", "min(other.afterimage_count, array_length(afterimage))");

    // include Tenna
    importGroup.QueueRegexFindReplace("gml_Object_obj_tenna_smashcut_attack_Step_0", "timer == ([0-9|\\.]+)", "timer == ceil(global.diff_enemycd * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_tenna_smashcut_attack_Step_0", "timer (\\+?-?)= ([^;]+)", "timer $1= floor(global.diff_enemycd * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_tenna_allstars_manager_Create_0", "(?<!turn)timer (\\+?-?)= ([^;]+)", "timer $1= floor(global.diff_enemycd * ($2))");
    importGroup.QueueFindReplace("gml_Object_obj_tenna_allstars_manager_Step_0", "(timer % 13) == 0", "(timer % ceil(global.diff_enemycd * 13)) == 0");
    importGroup.QueueFindReplace("gml_Object_obj_tenna_allstars_manager_Step_0", "(timer % 32) == 16", "(timer % (2 * ceil(global.diff_enemycd * 16))) == ceil(global.diff_enemycd * 16)");
    importGroup.QueueFindReplace("gml_Object_obj_tenna_allstars_manager_Step_0", "(timer % 32) == 0", "(timer % (2 * ceil(global.diff_enemycd * 16))) == 0");
    importGroup.QueueRegexFindReplace("gml_Object_obj_tenna_rimshot_star_Step_0", "rimshot_timer == ([0-9|\\.]+)", "rimshot_timer == floor(global.diff_enemycd * ($1))");
    importGroup.QueueFindReplace("gml_Object_obj_tenna_rimshot_star_Step_0", "laugh_timer += 0.25;", $"laugh_timer += {one_over_cd} * 0.25;");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "rimshot_timer = 74;", "rimshot_timer = ceil(global.diff_enemycd * 74);");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(btimer % rate1) == rate2", "(btimer % ceil(global.diff_enemycd * rate1)) == floor(global.diff_enemycd * rate2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_actor_tenna_Create_0", "(?<=lightemup|bullet_)timer (\\+?-?)= ([^;]+)", "timer $1= floor(global.diff_enemycd * ($2))");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "bullet_timer > (_rate - _jumpspeed)", "bullet_timer > (global.diff_enemycd * (_rate - _jumpspeed))");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "(bullet_timer + _movespeed + _waitspeed) > (_rate - _jumpspeed)",
        $"(({one_over_cd} * bullet_timer) + _movespeed + _waitspeed) > (_rate - _jumpspeed)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer == ([0-9|\\.]+)", "lightemuptimer == ceil(global.diff_enemycd * ($1))");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer < _jumpspeed", "lightemuptimer < (global.diff_enemycd * _jumpspeed)");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer >= _jumpspeed", "lightemuptimer >= (global.diff_enemycd * _jumpspeed)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer <(=?) ([0-9|\\.]+)", "lightemuptimer <$1 global.diff_enemycd * ($2)");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer / 2", "lightemuptimer / (global.diff_enemycd * 2)");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer / _movespeed", "lightemuptimer / (global.diff_enemycd * _movespeed)");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer >= _movespeed", "lightemuptimer >= (global.diff_enemycd * _movespeed)");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer == _waitspeed", "lightemuptimer == ceil(global.diff_enemycd * _waitspeed)");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer <= _jumpspeed", "lightemuptimer <= (global.diff_enemycd * _jumpspeed)");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer / _jumpspeed", "lightemuptimer / (global.diff_enemycd * _jumpspeed)");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer == (_jumpspeed - 4)", "lightemuptimer == ceil(global.diff_enemycd * (_jumpspeed - 4))");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer == _jumpspeed", "lightemuptimer == ceil(global.diff_enemycd * _jumpspeed)");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer >= (_jumpspeed + 10)", "lightemuptimer >= (global.diff_enemycd * (_jumpspeed + 10))");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer >= (_jumpspeed + 14)", "lightemuptimer >= (global.diff_enemycd * (_jumpspeed + 14))");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer == round(_graspspeed / 2)", "lightemuptimer == ceil(global.diff_enemycd * round(_graspspeed / 2))");
    importGroup.QueueFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer >= _graspspeed", "lightemuptimer >= (global.diff_enemycd * _graspspeed)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer >(=?) ([0-9|\\.]+)", "lightemuptimer >$1 global.diff_enemycd * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_actor_tenna_Draw_0", "lightemuptimer (\\+?-?)= ([^;]+)", "lightemuptimer $1= floor(global.diff_enemycd * ($2))");

    // include da knight
    importGroup.QueueFindReplace("gml_Object_obj_knight_roaring2_Step_0", "(roaring_timer % 5)", "(roaring_timer % ceil(max(0.25, global.diff_enemycd) * 5))");
    importGroup.QueueFindReplace("gml_Object_obj_knight_roaring2_Step_0", "(attack_timer == 4)", "(attack_timer == ceil(max(0.25, global.diff_enemycd) * 4))");
    importGroup.QueueFindReplace("gml_Object_obj_knight_roaring2_Step_0", "attack_timer = floor(-1 + intensity);", "attack_timer = floor(max(0.25, global.diff_enemycd) * (-1 + intensity));");
    importGroup.QueueFindReplace("gml_Object_obj_knight_roaring2_Other_10", "(roaring_timer % 5)", "(roaring_timer % ceil(max(0.25, global.diff_enemycd) * 5))");
    importGroup.QueueFindReplace("gml_Object_obj_knight_roaring2_Other_10", "(attack_timer == 4)", "(attack_timer == ceil(max(0.25, global.diff_enemycd) * 4))");
    importGroup.QueueFindReplace("gml_Object_obj_knight_roaring2_Other_10", "attack_timer = floor(attack_timer_goal + attack_token);",
        "attack_timer = floor(max(0.25, global.diff_enemycd) * (attack_timer_goal + attack_token));");
    importGroup.QueueFindReplace("gml_Object_obj_roaringknight_boxsplitter_attack_Step_0", "(timer >= spawn_speed)", "(timer >= global.diff_enemycd * spawn_speed)");
    importGroup.QueueFindReplace("gml_Object_obj_roaringknight_boxsplitter_attack_Step_0", "timer = -4;", "timer = floor(global.diff_enemycd * -4);");
    importGroup.QueueFindReplace("gml_Object_obj_roaringknight_boxsplitter_attack_Draw_0", "(timer / 30)", "(timer / (global.diff_enemycd * 30))");
    importGroup.QueueFindReplace("gml_Object_obj_roaringknight_boxsplitter_attack_Draw_0", "timer, timer", $"({one_over_cd} * timer), ({one_over_cd} * timer)");
    importGroup.QueueFindReplace("gml_Object_obj_roaringknight_boxsplitter_attack_Draw_0", "-timer + 40, -timer + 40",
        $"({one_over_cd} * -timer) + 40, ({one_over_cd} * -timer) + 40");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_tunnel_slasher_Step_0", "timer == ([0-9|\\.]+)", "timer == ceil(global.diff_enemycd * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_tunnel_slasher_Step_0", "timer <(=?) ([0-9|\\.]+)", "timer <$1 global.diff_enemycd * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_tunnel_slasher_Step_0", "timer % ([0-9]+)", "timer % ceil(global.diff_enemycd * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_tunnel_slasher_Step_0", "timer >(=?) ([0-9|\\.]+)", "timer >$1 global.diff_enemycd * ($2)");
    importGroup.QueueFindReplace("gml_Object_obj_knight_tunnel_slasher_Draw_0", "fulltimer", $"({one_over_cd} * fulltimer)");
    importGroup.QueueFindReplace("gml_Object_obj_roaringknight_quickslash_attack_Create_0", "timer = 99;", "timer = floor(global.diff_enemycd * 99);");
    importGroup.QueueFindReplace("gml_Object_obj_roaringknight_quickslash_attack_Step_0", "timer = -2;", "timer = floor(global.diff_enemycd * -2);");
    importGroup.QueueFindReplace("gml_Object_obj_roaringknight_quickslash_attack_Step_0", "(timer >= spawn_speed)", "(timer >= global.diff_enemycd * spawn_speed)");
    importGroup.QueueFindReplace("gml_Object_obj_knight_rotating_slash_Alarm_2", "knight.timer = knight.spawn_speed;", "knight.timer = floor(global.diff_enemycd * knight.spawn_speed);");
    importGroup.QueueFindReplace("gml_Object_obj_knight_rotating_slash_Alarm_2", "timer = spawn_speed;", "timer = floor(global.diff_enemycd * spawn_speed);");
    importGroup.QueueFindReplace("gml_Object_obj_knight_rotating_slash_Step_0", "timer = spawn_speed", "timer = floor(global.diff_enemycd * spawn_speed)");
    importGroup.QueueFindReplace("gml_Object_obj_knight_rotating_slash_Step_0", "timer = -8;", "timer = floor(global.diff_enemycd * -8)");
    importGroup.QueueFindReplace("gml_Object_obj_knight_rotating_slash_Step_0", "timer = -12;", "timer = floor(global.diff_enemycd * -12)");
    importGroup.QueueFindReplace("gml_Object_obj_knight_rotating_slash_Step_0", "timer == cooldown_time", "timer == ceil(global.diff_enemycd * cooldown_time)");
    importGroup.QueueFindReplace("gml_Object_obj_dknight_slasher_Step_0", "(timer >= (throwernumber * 3))", "(timer >= global.diff_enemycd * (throwernumber * 3))");
    importGroup.QueueFindReplace("gml_Object_obj_knight_slasher_Step_0", "(timer == (6 - diff))", "(timer == ceil(global.diff_enemycd * (6 - diff)))");
    importGroup.QueueFindReplace("gml_Object_obj_knight_slasher_Step_0", "(timer == ((5 - diff) + movespeed))", "(timer == ceil(global.diff_enemycd * ((5 - diff) + movespeed)))");
    importGroup.QueueFindReplace("gml_Object_obj_knight_swordfall_Alarm_3", "timer = -8;", "timer = floor(global.diff_enemycd * -8);");
    importGroup.QueueFindReplace("gml_Object_obj_knight_swordfall_Alarm_2", "alarm[4] = 26;", "alarm[4] = ceil(global.diff_enemycd * 26);");
    importGroup.QueueFindReplace("gml_Object_obj_knight_swordfall_Alarm_5", "alarm[0] = 8;", "alarm[0] = ceil(global.diff_enemycd * 8);");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_swordfall_Step_0", "alarm\\[([0-9]+)\\] = ([0-9]+);", "alarm[$1] = ceil(global.diff_enemycd * $2);");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_swordfall_Other_10", "alarm\\[([0-9]+)\\] = ([0-9]+);", "alarm[$1] = ceil(global.diff_enemycd * $2);");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_swordfall_Create_0", "countdown = ([0-9]+);", "countdown = ceil(global.diff_enemycd * $1);");
    importGroup.QueueFindReplace("gml_Object_obj_knight_swordfall_Step_0", "countdown = countdowner - irandom(1);", "countdown = ceil(global.diff_enemycd * (countdowner - irandom(1)));");
    importGroup.QueueFindReplace("gml_Object_obj_knight_swordfall_Step_0", "countdown = countdowner;", "countdown = ceil(global.diff_enemycd * countdowner);");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_swordfall_Other_10", "countdown = ([0-9]+);", "countdown = ceil(global.diff_enemycd * $1);");
    importGroup.QueueFindReplace("gml_Object_obj_knight_tunnel_slasher_2_revised_Alarm_2", "knight.timer = knight.spawn_speed;",
        "knight.timer = floor(global.diff_enemycd * knight.spawn_speed);");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_tunnel_slasher_2_revised_Step_0", "(?<!turn|intro)timer == ([0-9|\\.]+)", "timer == ceil(global.diff_enemycd * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_tunnel_slasher_2_revised_Step_0", "(?<!turn|intro)timer <(=?) ([0-9|\\.]+)", "timer <$1 global.diff_enemycd * ($2)");
    importGroup.QueueFindReplace("gml_Object_obj_knight_tunnel_slasher_2_revised_Step_0", "((fake_timer + 8) % 4)", "(fake_timer % ceil(global.diff_enemycd * 4))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_tunnel_slasher_2_revised_Step_0", "(?<!turn|intro)timer (\\+?-?)= ([^;]+)", "timer $1= floor(global.diff_enemycd * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_tunnel_slasher_2_revised_Other_10", "(?<!turn|intro)timer (\\+?-?)= ([^;]+)", "timer $1= floor(global.diff_enemycd * ($2))");
    importGroup.QueueFindReplace("gml_Object_obj_sword_tunnel_manager_Create_0", "timer = -40 + irandom(10);", "timer = floor(global.diff_enemycd * -40 + irandom(10));");
    importGroup.QueueFindReplace("gml_Object_obj_sword_tunnel_manager_Step_0", "timer >= rate", "timer >= global.diff_enemycd * rate");
    importGroup.QueueFindReplace("gml_Object_obj_sword_tunnel_manager_Step_0", "timer = max(0, sin(tobytimer / 6) * 2);", "timer = floor(global.diff_enemycd * max(0, sin(tobytimer / 6) * 2));");
    importGroup.QueueRegexFindReplace("gml_Object_obj_knight_stream_Step_0", "timer == ([0-9|\\.]+)", "timer == ceil(global.diff_enemycd * ($1))");
}
if (ch_no == 4) {
    // include Lanino & Elnina
    importGroup.QueueFindReplace("gml_Object_obj_elnina_mascotattack_Step_0", "shottimer[i] >= shotrate[i]", "shottimer[i] >= global.diff_enemycd * shotrate[i]");

    // include guei
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "((btimer - 40) % ceil(24 * ratio)) == (10 * sameattacker)",
        "((btimer - 40) % ceil(global.diff_enemycd * 24 * ratio)) == floor(global.diff_enemycd * (10 * sameattacker))");

    // include balthizard
    importGroup.QueueFindReplace("gml_Object_obj_incense_censer_Step_2", "(timer % (13 + floor(15 * ratio)))", "(timer % ceil(global.diff_enemycd * (13 + floor(15 * ratio))))");
    importGroup.QueueFindReplace("gml_Object_obj_incense_censer_Step_2", "))) == 5", "))) == floor(global.diff_enemycd * 5)");
    importGroup.QueueFindReplace("gml_Object_obj_incense_censer_Step_2", "(timer % 7)", "(timer % ceil(global.diff_enemycd * 7))");
    importGroup.QueueFindReplace("gml_Object_obj_incense_censer_Step_2", "(timer % 3)", "(timer % ceil(global.diff_enemycd * 3))");
    importGroup.QueueFindReplace("gml_Object_obj_incense_censer_Step_2", "(timer % 10)", "(timer % ceil(global.diff_enemycd * 10))");

    // include holy watercooler
    importGroup.QueueFindReplace("gml_Object_obj_watercooler_bullet_rainball_Step_0", "timer >= threshold", "timer >= (global.diff_enemycd * threshold)");

    // include winglade
    importGroup.QueueFindReplace("gml_Object_obj_halo_bat_aim_Step_0", "timer / 12", "timer / (max(0.5, global.diff_enemycd) * 12)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_halo_bat_aim_Step_0", "\\(timer == ([0-9]+)\\)", "(timer == floor(max(0.5, global.diff_enemycd) * $1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_halo_bat_aim_Step_0", "(?<!turn)timer (>=|<|<=|>) ([0-9]+)", "timer $1 floor(max(0.5, global.diff_enemycd) * $2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_halo_bat_aim_Step_0", "\\(timer - ([0-9]+)\\) / ([0-9]+)", "(timer - $1) / (max(0.5, global.diff_enemycd) * $2)");
    importGroup.QueueFindReplace("gml_Object_obj_halo_bat_aim_Step_0", "(timer * 0.1)", "(timer * 0.1 / max(0.5, global.diff_enemycd))");
    importGroup.QueueFindReplace("gml_Object_obj_halo_bat_aim_Step_0", "timer2 / 4", "timer2 / (4 * max(0.5, global.diff_enemycd))");
    importGroup.QueueFindReplace("gml_Object_obj_halo_bat_aim_Step_0", "(timer >= timer_limit)", "(timer >= max(0.5, global.diff_enemycd) * timer_limit)");
    importGroup.QueueFindReplace("gml_Object_obj_halo_bat_aim_Other_25", "timer >= 34 && timer < 38", "timer >= (max(0.5, global.diff_enemycd) * 34) && timer < (max(0.5, global.diff_enemycd) * 38)");
    importGroup.QueueFindReplace("gml_Object_obj_halo_bat_aim_Other_25", "(timer == 38)", "(timer == ceil(max(0.5, global.diff_enemycd) * 38))");
    importGroup.QueueFindReplace("gml_Object_obj_halo_bat_aim_Other_25", "(timer > 38)", "(timer > ceil(max(0.5, global.diff_enemycd) * 38))");
    importGroup.QueueFindReplace("gml_Object_obj_halo_bat_circle_Step_0", "timer == (timer_shoot - 10)", "timer == floor(global.diff_enemycd * (timer_shoot - 10))");
    importGroup.QueueFindReplace("gml_Object_obj_halo_bat_circle_Step_0", "timer >= floor(timer_shoot / (1 + (scr_monsterpop() == 1)))",
        "timer >= (global.diff_enemycd * floor(timer_shoot / (1 + (scr_monsterpop() == 1))))");

    // include organik
    importGroup.QueueFindReplace("gml_Object_obj_organnote_manager_Create_0", "alarm[0] = 8;", "alarm[0] = ceil(global.diff_enemycd * 8);");
    importGroup.QueueFindReplace("gml_Object_obj_organnote_manager_Alarm_0", "alarm[1] = 1;", "alarm[1] = ceil(global.diff_enemycd * 1);");
    importGroup.QueueRegexFindReplace("gml_Object_obj_organnote_manager_Alarm_1", "alarm\\[([0-9]+)\\] = ([0-9]+);", "alarm[$1] = ceil(global.diff_enemycd * $2);");
    importGroup.QueueRegexFindReplace("gml_Object_obj_organnote_manager_Alarm_2", "alarm\\[([0-9]+)\\] = ([0-9]+);", "alarm[$1] = ceil(global.diff_enemycd * $2);");
    importGroup.QueueFindReplace("gml_Object_obj_organnote_manager_Alarm_2", "alarm[2] = interval;", "alarm[2] = ceil(global.diff_enemycd * interval);");

    importGroup.QueueFindReplace("gml_Object_obj_organ_vertical_pillar_manager_Alarm_0", "alarm[0] = other.interval + 4;", "alarm[0] = ceil(global.diff_enemycd * (other.interval + 4));");
    importGroup.QueueFindReplace("gml_Object_obj_organ_vertical_pillar_manager_Alarm_0", "alarm[0] = interval;", "alarm[0] = ceil(global.diff_enemycd * interval);");
    importGroup.QueueFindReplace("gml_Object_obj_organ_vertical_pillar_manager_Other_10", "alarm[0] = start_time;", "alarm[0] = ceil(global.diff_enemycd * start_time);");

    // include wicabel
    importGroup.QueueRegexFindReplace("gml_Object_obj_bell_enemy_Draw_0", "spinattacktimer == ([0-9]+)", "spinattacktimer == ceil(global.diff_enemycd * $1)");
    importGroup.QueueFindReplace("gml_Object_obj_bell_enemy_Draw_0", "(spinattacktimer >= 7 || spinattacktimer < 11)",
        "(spinattacktimer >= (global.diff_enemycd * 7) || spinattacktimer < (global.diff_enemycd * 11))");
}
if (ch_no == 5) {
    importGroup.QueueFindReplace("gml_Object_obj_attack_blue_ponddancing_Draw_0", "if ((btimer % brate) == 0)", "if ((btimer % ceil(global.diff_enemycd * brate)) == 0)");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_aqua_miniboss_Step_0", "attacktimer >= attacklimit", "attacktimer >= global.diff_enemycd * attacklimit");

    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_gun_Create_0", "shottimer = shotdelay_start;", "shottimer = ceil(global.diff_enemycd * shotdelay_start);");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_gun_Create_0", "shottimer = shotdelay;", "shottimer = ceil(global.diff_enemycd * shotdelay);");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_gun_Step_0", "shottimer = shotdelay;", "shottimer = ceil(global.diff_enemycd * shotdelay);");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_gun_Step_0", "shottimer == (shotdelay - 1)", "shottimer == floor(global.diff_enemycd * (shotdelay - 1))");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_gun_Step_0", "shottimer > rapiddelay", "shottimer > (global.diff_enemycd * rapiddelay)");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_gun_Step_0", "(shottimer - rapiddelay) / (shotdelay - 16)", "(shottimer - global.diff_enemycd * rapiddelay) / (global.diff_enemycd * (shotdelay - 16))");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_gun_Step_0", "shottimer < (shotdelay - 8)", "shottimer < global.diff_enemycd * (shotdelay - 8)");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_gun_Step_0", "shottimer == (rapiddelay - 1)", "shottimer == floor(global.diff_enemycd * (rapiddelay - 1))");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_gun_Step_0", "shottimer / ", $"{one_over_cd} * shottimer / ");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_gun_Draw_0", "shottimer / ", $"{one_over_cd} * shottimer / ");

    importGroup.QueueRegexFindReplace("gml_Object_obj_starwalker_overworld_Step_0", "attacktimer = ([0-9]+);", "attacktimer = floor(global.diff_enemycd * $1);");
    importGroup.QueueFindReplace("gml_Object_obj_starwalker_overworld_Step_0", "attacktimer >= ", "attacktimer >= global.diff_enemycd * ");
    importGroup.QueueFindReplace("gml_Object_obj_starwalker_overworld_Step_0", "attacktimer = 46 + min(16, attackvolley, difficulty_capped);",
        "attacktimer = floor(global.diff_enemycd * (46 + min(16, attackvolley, difficulty_capped)));");
    importGroup.QueueFindReplace("gml_Object_obj_starwalker_overworld_Step_0", "attacktimer = 41 + min(6, attackvolley * 2, difficulty_capped);",
        "attacktimer = floor(global.diff_enemycd * (41 + min(6, attackvolley * 2, difficulty_capped)));");
    importGroup.QueueFindReplace("gml_Object_obj_starwalker_overworld_Step_0", "attacktimer += 7;", "attacktimer += floor(global.diff_enemycd * 7);");
    // fix star bullets dissappearing too soon
    importGroup.QueueFindReplace("gml_Object_obj_starwalker_overworld_Step_0", "if (shot == 1)", "if (false && shot == 1)");
    importGroup.QueueAppend("gml_Object_obj_starwalker_overworld_Create_0", "trackstarbullet = array_create(0);");
    importGroup.QueueFindReplace("gml_Object_obj_starwalker_overworld_Step_0", "configure_bullet(starbullet[i], i, target, count);", @"
        configure_bullet(starbullet[i], i, target, count);
        array_push(trackstarbullet, starbullet[i]);
    ");
    importGroup.QueueAppend("gml_Object_obj_starwalker_overworld_Step_0", @"
        var newtrackstarbullet = array_create(0);
        var cam = view_camera[0];
        var x1 = camera_get_view_x(cam);
        var y1 = camera_get_view_y(cam);
        var x2 = x1 + camera_get_view_width(cam);
        var y2 = y1 + camera_get_view_height(cam);
        for (var i = 0; i < array_length(trackstarbullet); i++) {
            with (trackstarbullet[i]) {
                if(!point_in_rectangle( x, y, x1, y1, x2, y2))
                    instance_destroy();
                else
                    array_push(newtrackstarbullet, self);
            }
        }
        trackstarbullet = newtrackstarbullet;
    ");

    importGroup.QueueFindReplace("gml_Object_obj_starwalker_climb_Step_0", "attacktimer == 20", "attacktimer == ceil(global.diff_enemycd * 20)");
    importGroup.QueueFindReplace("gml_Object_obj_starwalker_climb_Step_0", "attacktimer >= ", "attacktimer >= global.diff_enemycd * ");
    importGroup.QueueFindReplace("gml_Object_obj_starwalker_climb_Step_0", "attacktimer = 15;", "attacktimer = floor(global.diff_enemycd * 15);");
    importGroup.QueueFindReplace("gml_Object_obj_starwalker_climb_Step_0", "attacktimer = -99;", "attacktimer = floor(global.diff_enemycd * -99);");

    // TODO can't figure out why trashy's bullet tragectories get messed up at giga low cds but seems work fine for all the presets
    importGroup.QueueRegexFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(?<!\\w|\\.)timer = (-?[0-9]+);", "timer = floor(global.diff_enemycd * $1);");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "timer = irandom(timer_goal - 1);", "timer = floor(global.diff_enemycd * irandom(timer_goal - 1));");
    importGroup.QueueRegexFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(?<!\\w|\\.)timer == (-?[0-9]+)", "timer == ceil(global.diff_enemycd * $1)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(?<!\\w|\\.)timer < (-?[0-9]+)", "timer < (global.diff_enemycd * $1)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(?<!\\w|\\.)timer >= (-?[0-9]+)", "timer >= (global.diff_enemycd * $1)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(?<!\\w|\\.)timer % (-?[0-9]+)", "timer % ceil(global.diff_enemycd * $1)");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "global.time", $"{one_over_cd} * global.time");

    importGroup.QueueFindReplace("gml_Object_obj_trashy_trio_Create_0", "attack_timer * 16", $"{one_over_cd} * attack_timer * 16");
    importGroup.QueueFindReplace("gml_Object_obj_trashy_trio_Create_0", "attack_timer >= 12", "attack_timer >= (global.diff_enemycd * 12)");
    importGroup.QueueFindReplace("gml_Object_obj_trashy_trio_Create_0", "attack_timer > 10", "attack_timer > (global.diff_enemycd * 10)");
    importGroup.QueueFindReplace("gml_Object_obj_trashy_trio_Create_0", "attack_timer % 2", "attack_timer % ceil(global.diff_enemycd * 2)");

    // fix delay at start of floradinn spike attack and maybe others
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "if ((btimer - 90) >= ((10 * ratio) + (sameattacker * sameattack)))",
        "if ((btimer - global.diff_enemycd * 90) >= (global.diff_enemycd * ((10 * ratio) + (sameattacker * sameattack))))");
    // floradinn mane attack
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "special = 10;", "special = ceil(max(2, global.diff_enemycd * 10));");
    // sheary attack
    importGroup.QueueFindReplace("gml_Object_obj_sheary_smashcut_attack_Create_0", "timer = 10;", "timer = floor(global.diff_enemycd * 10);");
    importGroup.QueueFindReplace("gml_Object_obj_sheary_smashcut_attack_Create_0", "timer2 = 12;", "timer2 = floor(global.diff_enemycd * 12);");
    importGroup.QueueFindReplace("gml_Object_obj_sheary_smashcut_attack_Step_0", "timer == ((24 + ((10 - difficulty) * type)) - (difficulty * 2))",
        "timer == ceil(global.diff_enemycd * ((24 + ((10 - difficulty) * type)) - (difficulty * 2)))");
    importGroup.QueueFindReplace("gml_Object_obj_sheary_smashcut_attack_Step_0", "timer2 == ((25 + (10 * type)) - ((3 + type) * difficulty))",
        "timer2 == ceil(global.diff_enemycd * ((25 + (10 * type)) - ((3 + type) * difficulty)))");

    // revert this change so that netskie fake rabbick bullets can work properly: preserve original timing and add new, variably timed attacks
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "btimer >= (global.diff_enemycd * bmax)", "btimer >= bmax");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "if (btimer == special && i_ex(obj_netskie_enemy))", @"
        if (btimer >= (global.diff_enemycd * bmax))
        {
            rab = instance_create(obj_growtangle.x + obj_growtangle.sprite_width + 10, obj_growtangle.y, obj_rabbitbullet);
            rab.hspeed = choose(3 + random(1), 3 + random(1), 6) * -1;

            if (rab.hspeed == -6)
                rab.x += 50 + random(30);

            scr_bullet_inherit(rab);
        }

        if (btimer == special && i_ex(obj_netskie_enemy))
    ");
    // netskie shadowman attack
    importGroup.QueueFindReplace("gml_Object_obj_shadowman_tommygun_Step_0", "bullet_timer = 4;", "bullet_timer = ceil(global.diff_enemycd * 4);");
    importGroup.QueueFindReplace("gml_Object_obj_shadowman_tommygun_Step_0", "bullet_timer += (10 + (irandom(1) * 5) + (4 * sameattack));", "bullet_timer += ceil(global.diff_enemycd * (10 + (irandom(1) * 5) + (4 * sameattack)));");
    importGroup.QueueRegexFindReplace("gml_Object_obj_shadowman_tommygun_Step_0", "netskie_count == (-?[0-9]+)", $"netskie_count == floor({one_over_cd} * $1)");
    // spread out floradinn fake bullets properly
    importGroup.QueueFindReplace("gml_Object_obj_netskie_enemy_Create_0", "choose(1, 2)", $"irandom_range(1, max(1, floor({one_over_cd} * 2)))");
    importGroup.QueueFindReplace("gml_Object_obj_netskie_enemy_Create_0", "choose(3, 4)", $"irandom_range(max(2, floor({one_over_cd} * 3)), max(2, floor({one_over_cd} * 4)))");
    importGroup.QueueFindReplace("gml_Object_obj_netskie_enemy_Create_0", "choose(5, 6)", $"irandom_range(max(3, floor({one_over_cd} * 5)), max(3, floor({one_over_cd} * 6)))");
    importGroup.QueueFindReplace("gml_Object_obj_netskie_enemy_Create_0", "choose(7, 8)", $"irandom_range(max(4, floor({one_over_cd} * 7)), max(4, floor({one_over_cd} * 8)))");
    importGroup.QueueFindReplace("gml_Object_obj_netskie_enemy_Create_0", "choose(9, 10)", $"irandom_range(max(5, floor({one_over_cd} * 9)), max(5, floor({one_over_cd} * 10)))");
    importGroup.QueueFindReplace("gml_Object_obj_netskie_enemy_Create_0", "choose(11, 12)", $"irandom_range(max(6, floor({one_over_cd} * 11)), max(6, floor({one_over_cd} * 12)))");
    // netskie foxtrot attack
    importGroup.QueueFindReplace("gml_Object_obj_bullet_foxtrot_Create_0", "timer = 30;", "timer = ceil(global.diff_enemycd * 30);");
    importGroup.QueueFindReplace("gml_Object_obj_bullet_foxtrot_Step_0", "timer = 30 + (10 * sameattack);", "timer = ceil(global.diff_enemycd * 30 + (10 * sameattack));");
    importGroup.QueueFindReplace("gml_Object_obj_bullet_foxtrot_Step_0", "timer < 21 && timer > 4", "timer < global.diff_enemycd * 21 && timer > global.diff_enemycd * 4");
    importGroup.QueueFindReplace("gml_Object_obj_bullet_foxtrot_Step_0", "(timer % 3) == 2", "(timer % ceil(global.diff_enemycd * 3)) == floor(global.diff_enemycd * 2)");
    importGroup.QueueFindReplace("gml_Object_obj_bullet_foxtrot_Step_0", "2 + (timer / 5)", $"({one_over_cd} * (2 + (timer / 5)))");

    // aqua yes!! silly little adorable murder gremlin!!! sdgsdfhsdfjgbsdigbsdjkfgnk *dies*
    // importGroup.QueueFindReplace("gml_Object_obj_attack_knifechain_manager2_Alarm_0", "alarm[0] = 35;", "alarm[0] = ceil(global.diff_enemycd * 35);");
    // importGroup.QueueFindReplace("gml_Object_obj_attack_knifechain_manager2_Alarm_0", "alarm[0] = 16;", "alarm[0] = ceil(global.diff_enemycd * 16);");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "_aqua_fanofknives.timer = 10;", "_aqua_fanofknives.timer = floor(global.diff_enemycd * 10);");
    importGroup.QueueFindReplace("gml_Object_obj_attack_knifefan_manager_Create_0", "timer = 30;", "timer = floor(global.diff_enemycd * 30);");
    importGroup.QueueFindReplace("gml_Object_obj_attack_knifefan_manager_Create_0", "timer = 50;", "timer = floor(global.diff_enemycd * 50);");
    importGroup.QueueFindReplace("gml_Object_obj_attack_knifefan_manager_Step_0", "timer == (cooldown - 6)", "timer == ceil(global.diff_enemycd * (cooldown - 6))");
    importGroup.QueueFindReplace("gml_Object_obj_attack_knifefan_manager_Step_0", "timer >= cooldown", "timer >= ceil(global.diff_enemycd * cooldown)");
    importGroup.QueueFindReplace("gml_Object_obj_attack_knife_leafling_Create_0", "alarm[0] = 10;", "alarm[0] = ceil(global.diff_enemycd * 10);");
    importGroup.QueueFindReplace("gml_Object_obj_attack_knife_leafling_Alarm_0", "alarm[0] = cooldown;", "alarm[0] = ceil(global.diff_enemycd * cooldown);");

    // platforming aqua knife
    importGroup.QueueFindReplace("gml_Object_obj_plat_dash_aqua_Step_0", "(timer == 8)", "(timer == ceil(global.diff_enemycd * 8))");
    importGroup.QueueFindReplace("gml_Object_obj_plat_dash_aqua_Step_0", "timer >= timerlimit", "timer >= global.diff_enemycd * timerlimit");
    importGroup.QueueFindReplace("gml_Object_obj_plat_dash_aqua_Step_0", "if (timer == 45)", "if (timer == ceil(global.diff_enemycd * 45))");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_sethglasses_miniboss_Step_0", "(timer % tt)", "(timer % ceil(global.diff_enemycd * tt))");
    importGroup.QueueFindReplace("gml_Object_obj_plat_enm_sethglasses_miniboss_Step_0", "(timer % 3)", "(timer % ceil(global.diff_enemycd * 3))");
    importGroup.QueueFindReplace("gml_Object_obj_knifewall_generator_Step_0", "(timer % 3)", "(timer % ceil(global.diff_enemycd * 3))");
    importGroup.QueueFindReplace("gml_Object_obj_knifewall_generator_Step_0", "(timer % interval)", "(timer % ceil(global.diff_enemycd * interval))");
    importGroup.QueueFindReplace("gml_Object_obj_knifewall_generator_Step_0", "timer >= 60", "timer >= global.diff_enemycd * 60");
    // TODO the platforming aqua pattern doesn't cooldown reduce, and I do not know why

    // Terakota
    importGroup.QueueFindReplace("gml_Object_obj_terracota_pots_controller_Draw_0", "timer == timermax", "timer == ceil(global.diff_enemycd * timermax)");

    // type == 145
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(my_timer % ceil(10 + (40 * ratio))) == (24 * sameattacker)",
        "(my_timer % ceil(global.diff_enemycd * (10 + (40 * ratio)))) == floor(global.diff_enemycd * 24 * sameattacker)");
    // type == 146
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "((btimer - 12) % ceil(25 * ratio)) == (17 * sameattacker)",
        "((btimer - floor(global.diff_enemycd * 12)) % ceil(global.diff_enemycd * 25 * ratio)) == floor(global.diff_enemycd * 17 * sameattacker)");

    // green & orange
    importGroup.QueueFindReplace("gml_Object_obj_attack_green_cookingtime_Draw_0", "timer == (timermax - 3)", "timer == ceil(global.diff_enemycd * (timermax - 3))");
    importGroup.QueueFindReplace("gml_Object_obj_attack_green_cookingtime_Draw_0", "(timer >= timermax)", "(timer >= global.diff_enemycd * timermax)");
    importGroup.QueueFindReplace("gml_Object_obj_attack_green_cookingtime_Draw_0", "timer = -2;", "timer = floor(global.diff_enemycd * -2);");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(mytimer % 56) == 1", "(mytimer % ceil(global.diff_enemycd * 56)) == 1");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(other.mytimer > 5)", "(other.mytimer > global.diff_enemycd * 5)");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(mytimer % 56) == 15", "(mytimer % ceil(global.diff_enemycd * 56)) == floor(global.diff_enemycd * 15)");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(mytimer % 56) == 22", "(mytimer % ceil(global.diff_enemycd * 56)) == floor(global.diff_enemycd * 22)");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(mytimer % 56) == 29", "(mytimer % ceil(global.diff_enemycd * 56)) == floor(global.diff_enemycd * 29)");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(mytimer % 56) == 25", "(mytimer % ceil(global.diff_enemycd * 56)) == floor(global.diff_enemycd * 25)");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "(mytimer % 56) == 30", "(mytimer % ceil(global.diff_enemycd * 56)) == floor(global.diff_enemycd * 30)");
    // TODO be nice to include the omega attack

    // yellow hat shooter
    importGroup.QueueFindReplace("gml_Object_obj_dw_fcastle_dangerous_platforming_Step_0", "hat_timer = -1;", "hat_timer = round(global.diff_enemycd * -1);");

    // blue + yellow boss
    // TODO this stuff is giga scripted and doesn't play ball well
    // importGroup.QueueFindReplace("gml_Object_obj_blue_guidelines_Step_0", "(timer % 3)", "(timer % ceil(global.diff_enemycd * 3))");
    // importGroup.QueueFindReplace("gml_Object_obj_blue_guidelines_Step_0", "(timer == 32)", "(timer == ceil(global.diff_enemycd * 32))");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Alarm_0", "alarm[0] = 64;", "alarm[0] = ceil(global.diff_enemycd * 64);");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Alarm_3", "alarm[3] = 40;", "alarm[3] = ceil(global.diff_enemycd * 40);");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "alarm[0] = 96;", "alarm[0] = ceil(global.diff_enemycd * 96);");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "alarm[3] = 40;", "alarm[3] = ceil(global.diff_enemycd * 40);");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "alarm[0] = 1 + (8 * (abs(x - (other.x + 50)) / 150));", "alarm[0] = ceil(global.diff_enemycd * (1 + (8 * (abs(x - (other.x + 50)) / 150))));");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "image_index = other.dancer_speed_counter + ((40 - alarm[1]) * 0.4);",
    //     "image_index = other.dancer_speed_counter + ((ceil(global.diff_enemycd * 40) - alarm[1]) * 0.4);");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "alarm[0] == 64", "alarm[0] == floor(global.diff_enemycd * 64);");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "alarm[1] = 24", "alarm[1] = ceil(global.diff_enemycd * 24);");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "alarm[2] = 13", "alarm[2] = ceil(global.diff_enemycd * 13);");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "24 - alarm[1]", "ceil(global.diff_enemycd * 24) - alarm[1]");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "24 - other.alarm[1]", "ceil(global.diff_enemycd * 24) - other.alarm[1]");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "timer == 48", "timer == (43 + ceil(global.diff_enemycd * 5))");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "timer == 72", "timer == (43 + ceil(global.diff_enemycd * 29))");
    // importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_boxspin_Step_0", "timer == 106", "timer == (43 + ceil(global.diff_enemycd * 63))");
    importGroup.QueueFindReplace("gml_Object_obj_enemy_blue_flower_aim_Other_10", "alarm[0] = attack_speed;", "alarm[0] = ceil(global.diff_enemycd * attack_speed);");
    importGroup.QueueFindReplace("gml_Object_obj_blue_singing2_Step_0", "timer % (20 + offset)", "timer % ceil(global.diff_enemycd * (20 + offset))");
    importGroup.QueueFindReplace("gml_Object_obj_blue_singing2_Step_0", "sin(timer / 19)", $"sin({one_over_cd} * timer / 19)");
    importGroup.QueueFindReplace("gml_Object_obj_blue_singing2_Draw_0", "timer * 7", $"{one_over_cd} * timer * 7");

    // Mad Mew Mew
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "if (btimer >= (global.diff_enemycd * (btimer_start + 15)))", "if (btimer >= (btimer_start + 15))");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "btimer -= ceil(global.diff_enemycd * round(0.5 + ((13 * ds_list_find_value(obj_purplecontrols.ds_bullet_list, 2)) / _bullet_interval_modifier)));",
        "btimer -= round(0.5 + ((13 * ds_list_find_value(obj_purplecontrols.ds_bullet_list, 2)) / _bullet_interval_modifier));");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "until (btimer < (global.diff_enemycd * (btimer_start + 15)));", "until (btimer < (btimer_start + 15));");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "if (btimer == ceil(global.diff_enemycd * -173))", "if (btimer == -173)");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "btimer = floor(global.diff_enemycd * 32);", "btimer = 32;");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "if (btimer >= (global.diff_enemycd * 40))", "if (btimer >= 40)");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "btimer = floor(global.diff_enemycd * (40 - floor(0.5 + (40 * _binterval))));", "btimer = (40 - floor(0.5 + (40 * _binterval)));");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "until (btimer < (global.diff_enemycd * 40));", "until (btimer < 40);");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "if (btimer >= (global.diff_enemycd * (btimer_start + _startdelay)))", "if (btimer >= (btimer_start + _startdelay))");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "btimer -= ceil(global.diff_enemycd * round(0.5 + ((45 * ds_list_find_value(obj_purplecontrols.ds_bullet_list, 1)) / _bullet_interval_modifier)));",
        "btimer -= round(0.5 + ((45 * ds_list_find_value(obj_purplecontrols.ds_bullet_list, 1)) / _bullet_interval_modifier));");
    importGroup.QueueFindReplace("gml_Object_obj_dbulletcontroller_Step_0", "until (btimer < (global.diff_enemycd * (btimer_start + _startdelay)));", "until (btimer < (btimer_start + _startdelay));");

    // Final Flowery
    // importGroup.QueueFindReplace("gml_Object_obj_debug_orangeheartcontroller_Step_0", "= abs((fakecamxspeedbase + fakecamxspeedadditional) / ", $"= abs({one_over_cd} * (fakecamxspeedbase + fakecamxspeedadditional) / ");
    importGroup.QueueFindReplace("gml_Object_obj_debug_orangeheartcontroller_Step_0", "= abs((fakecamxspeedbase + fakecamxspeedadditional) / 10", $"= abs({one_over_cd} * (fakecamxspeedbase + fakecamxspeedadditional) / 10");
    importGroup.QueueFindReplace("gml_Object_obj_debug_orangeheartcontroller_Step_0", "if ((timer % 8) == 0)", "if ((timer % ceil(global.diff_enemycd * 8)) == 0)");
    importGroup.QueueFindReplace("gml_Object_obj_debug_orangeheartcontroller_Step_0", "if (((timer + 3) % 90) == 0)", "if (((timer + ceil(global.diff_enemycd * 3)) % ceil(global.diff_enemycd * 90)) == 0)");
}

// Apply Game Board Enemy Cooldowns
if (ch_no == 3)
{
    const string gmbrdenemycd = "(global.diff_gmbrdenemycd < 0 ? global.diff_enemycd : global.diff_gmbrdenemycd)";
    // basic enemies
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_bluebird_Step_0", "bulletimer == (-?[0-9|\\.]+)", $"bulletimer == ceil({gmbrdenemycd} * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_bluebird_Step_0", "bulletimer (\\+?-?)= ([^;]+)", $"bullettimer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_yellowflower_Create_0", "bubbletimer (\\+?-?)= ([^;]+)", $"bubbletimer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_yellowflower_Step_0", "bubbletimer == (-?[0-9|\\.]+)", $"bubbletimer == ceil({gmbrdenemycd} * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_yellowflower_Step_0", "bubbletimer >(=?) (-?[0-9|\\.]+)", $"bubbletimer >$1 {gmbrdenemycd} * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_yellowflower_Step_0", "bubbletimer (\\+?-?)= ([^;]+)", $"bubbletimer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_yellowflower_Other_22", "bubbletimer (\\+?-?)= ([^;]+)", $"bubbletimer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_offscreenevent_Step_0", "timer == (-?[0-9|\\.]+)", $"timer == ceil({gmbrdenemycd} * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_offscreenevent_Step_0", "timer (\\+?-?)= ([^;]+)", $"timer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_lizard_Create_0", "bulletimer (\\+?-?)= ([^;]+)", $"bulletimer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_lizard_Step_0", "bulletimer >(=?) (-?[0-9|\\.]+)", $"bulletimer >$1 {gmbrdenemycd} * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_lizard_Step_0", "bulletimer (\\+?-?)= ([^;]+)", $"bulletimer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_lizard_Other_22", "bulletimer (\\+?-?)= ([^;]+)", $"bulletimer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_singingcat_Step_0", "bubbletimer == (-?[0-9|\\.]+)", $"bubbletimer == ceil({gmbrdenemycd} * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_singingcat_Step_0", "bubbletimer >(=?) (-?[0-9|\\.]+)", $"bubbletimer >$1 {gmbrdenemycd} * ($2)");
    importGroup.QueueFindReplace("gml_Object_obj_board_enemy_silentcat_Step_0", "waketimer == 7", $"waketimer == ceil({gmbrdenemycd} * 7)");
    importGroup.QueueFindReplace("gml_Object_obj_board_enemy_silentcat_Step_0", "waketimer == 8", $"waketimer == ceil({gmbrdenemycd} * 7) + 1");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_flower_Step_0", "timer == (-?[0-9|\\.]+)", $"timer == ceil({gmbrdenemycd} * ($1))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_flower_Step_0", "timer (\\+?-?)= ([^;]+)", $"timer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_monster_Create_0", "bulletimer (\\+?-?)= ([^;]+)", $"bulletimer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueFindReplace("gml_Object_obj_board_enemy_monster_Step_0", "bulletimer > shoot_wait_time", $"bulletimer > ({gmbrdenemycd} * shoot_wait_time)");
    importGroup.QueueFindReplace("gml_Object_obj_board_enemy_monster_Step_0", "bulletimer <= shoot_wait_time", $"bulletimer <= ({gmbrdenemycd} * shoot_wait_time)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_monster_Step_0", "bulletimer >(=?) (-?[0-9|\\.]+)", $"bulletimer >$1 {gmbrdenemycd} * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_monster_Step_0", "bulletimer (\\+?-?)= ([^;]+)", $"bulletimer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_board_enemy_monster_Other_22", "bulletimer (\\+?-?)= ([^;]+)", $"bulletimer $1= floor({gmbrdenemycd} * ($2))");
    // John Mantleholder / Nightmare
    importGroup.QueueRegexFindReplace("gml_Object_obj_shadow_mantle_enemy_Step_2", "(?<=burstwave|spawnenemies|flamewave|dash)timer (\\+?-?)= ([^;]+)",
        $"timer $1= floor({gmbrdenemycd} * ($2))");
    importGroup.QueueRegexFindReplace("gml_Object_obj_shadow_mantle_enemy_Step_2", "(?<=burstwave|spawnenemies|flamewave|dash)timer >(=?) (-?[0-9|\\.]+)",
        $"timer >$1 {gmbrdenemycd} * ($2)");
    importGroup.QueueRegexFindReplace("gml_Object_obj_shadow_mantle_enemy_Step_2", "(?<=burstwave|spawnenemies|flamewave|dash)timer == (-?[0-9|\\.]+)",
        $"timer == ceil({gmbrdenemycd} * ($1))");
}

// Finish edit
// utmt keeps throwing out exceptions for gml compile errors w\ swatchling(ch2&demo)&sneo(demo)&laserattack(ch1) but on inspection nothing looks wrong and utmt saves changes without issue
    // seems to be inconsistent issue with utmt - exceptions appeared and dissappeared after completely irrelevant changes
importGroup.Import(false);
ScriptMessage($"Success: Custom difficulty added to '{displayName}'!");
