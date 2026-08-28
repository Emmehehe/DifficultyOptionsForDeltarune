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

// // check ModMenu framework installed
// UndertaleVariable frameworkInstalled = Data.Variables.ByName("installed_modmenu");
// if (frameworkInstalled == null) {
//     ScriptError($"Can't add a mod menu to '{displayName}' as the ModMenu framework is not installed.");
//     return;
// }

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

// Get config name from user
string configName = ScriptInputDialog("Config identifier", "Input a unique identifier for your menu config (alphanumeric & underscores only, no trailing underscores)", "my_mods_menu", "Cancel", "Submit", false, false);

const string validConfigName = "^[a-zA-Z0-9][a-zA-Z0-9_]*[a-zA-Z0-9]$";
if (String.IsNullOrEmpty(configName))
    return;

if (!Regex.IsMatch(configName, validConfigName, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(500)))
{
    ScriptError($"Error 1: invalid mod menu config name: '{configName}', must be only alphanumeric & underscores with no trailing underscores.");
    return;
}

// check version
UndertaleVariable alreadyInstalled = Data.Variables.ByName($"menu_{configName}");
if (alreadyInstalled != null) {
    ScriptError($"Can't add mod menu config '{configName}' to  '{displayName}' as it already exists.");
    return;
}

bool addToChapter = true;
bool addToDemoChapter1 = false;
if (ch_no == 0) {
    addToDemoChapter1 = ScriptQuestion("Add to chapter 1?");
    addToChapter = ScriptQuestion("Add to chapter 2?");
}

if (!addToChapter && !addToDemoChapter1) {
    ScriptError($"Can't add mod menu config '{configName}' to  '{displayName}' as no chapter has been selected.");
    return;
}

// Begin edit
ScriptMessage($"Adding new mod menu config '{configName}' to '{displayName}'...");

// Code edits
UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data){
    ThrowOnNoOpFindReplace = true
};

string example_config(string modmenuPostfix) { return @$"
    if (variable_instance_exists(global, ""modmenu"")) {{
        global.menu{modmenuPostfix}_{configName} = global.modmenu{modmenuPostfix}.create({{
            title: ""My Mod's Menu"",
            ini_name: ""{configName}"",
            form: [
                {{
                    type: ""Toggle"",
                    title: ""Example Toggle"",
                    data_ref: {{ var_name: ""example_toggle"", default_value: false }},
                    value_range: ""OFF=false;ON=true""
                }},{{
                    type: ""Slider"",
                    title: ""Example Slider"",
                    data_ref: {{ var_name: ""example_slider"", default_value: -1 }},
                    value_range: ""OFF=-1;0~1000%;INF=2147483647""
                }},{{
                    type: ""UserInput"",
                    title: ""Example UserInput"",
                    data_ref: {{ var_name: ""example_userinput"", default_value: ""USERONE"" }},
                    max_length: 12
                }},{{
                    type: ""Header"",
                    title: ""Example Header""
                }},{{
                    type: ""Button"",
                    title: ""Example Button"",
                    trigger_func: function () {{}}
                }}
            ]
        }});
    }}
"; }

const bool useModularScripts = false;
if (useModularScripts) {
    // This feature is WIP, don't use it
    if (addToDemoChapter1)
        importGroup.QueueAppend($"gml_GlobalScript_scr_modmenu_{configName}", example_config("_ch1"));
    if (addToChapter)
        importGroup.QueueAppend($"gml_GlobalScript_scr_modmenu_{configName}", example_config(""));

} else {
    string[] gamestarts = {};
    if (addToChapter)
    {
        string[] chapterGamestarts = {"gml_GlobalScript_scr_gamestart"};
        gamestarts = gamestarts.Concat(chapterGamestarts).ToArray();
    }
    if (addToDemoChapter1)
    {
        string[] demoGamestarts = {"gml_GlobalScript_scr_gamestart_ch1"};
        gamestarts = gamestarts.Concat(demoGamestarts).ToArray();
    }
    foreach (string gamestart in gamestarts)
    {
        importGroup.QueueFindReplace(gamestart, "global.litem[0] = 0;", @$"
            {example_config("")}

            global.litem[0] = 0;
        ");
    }
}

// Finish edit
importGroup.Import();
ScriptMessage($"Success: Mod menu config '{configName}' added to '{displayName}'!");
