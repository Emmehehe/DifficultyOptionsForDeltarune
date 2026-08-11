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
UndertaleVariable alreadyInstalled = Data.Variables.ByName($"modmenu_{configName}");
if (alreadyInstalled != null) {
    ScriptError($"Can't add mod menu config '{configName}' to  '{displayName}' as it already exists.");
    return;
}

// Begin edit
ScriptMessage($"Adding new mod menu config '{configName}' to '{displayName}'...");

// Code edits
UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data){
    ThrowOnNoOpFindReplace = true
};

string example_config = @$"
    if (variable_instance_exists(global, ""modmenu"")) {{
        global.modmenu_{configName} = modmenu.create({{
            title: ""My Mod's Menu"",
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
                    value_range: ""OFF=-1;0-1000%;INF=2147483647""
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
";

const useModularScripts = false;
if (useModularScripts) {
    importGroup.QueueAppend($"gml_GlobalScript_scr_modmenu_{configName}", example_config);
} else {
    string[] gamestarts = {"gml_GlobalScript_scr_gamestart"};
    if (ch_no == 0)
    {
        string[] demoGamestarts = {"gml_GlobalScript_scr_gamestart_ch1"};
        gamestarts = gamestarts.Concat(demoGamestarts).ToArray();
    }
    foreach (string gamestart in gamestarts)
    {
        importGroup.QueueFindReplace(gamestart, "global.litem[0] = 0;", @$"
            {example_config}

            global.litem[0] = 0;
        ");
    }
}

// Finish edit
importGroup.Import();
ScriptMessage($"Success: Mod menu config '{configName}' added to '{displayName}'!");
