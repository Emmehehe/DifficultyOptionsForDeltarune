@echo off
echo Installing difficulty options for all chapters...
"diffops_installfiles\UTMT_CLI_v0.8.3.0-Windows\UndertaleModCli.exe" load "chapter1_windows\data.win" --scripts "diffops_installfiles\Scripts\modmenu_ch1to4.csx,diffops_installfiles\Scripts\diffops_ch1.csx" --verbose false --output "chapter1_windows\data.win"
"diffops_installfiles\UTMT_CLI_v0.8.3.0-Windows\UndertaleModCli.exe" load "chapter2_windows\data.win" --scripts "diffops_installfiles\Scripts\modmenu_ch1to4.csx,diffops_installfiles\Scripts\diffops_ch2.csx" --verbose false --output "chapter2_windows\data.win"
"diffops_installfiles\UTMT_CLI_v0.8.3.0-Windows\UndertaleModCli.exe" load "chapter3_windows\data.win" --scripts "diffops_installfiles\Scripts\modmenu_ch1to4.csx,diffops_installfiles\Scripts\diffops_ch3.csx" --verbose false --output "chapter3_windows\data.win"
"diffops_installfiles\UTMT_CLI_v0.8.3.0-Windows\UndertaleModCli.exe" load "chapter4_windows\data.win" --scripts "diffops_installfiles\Scripts\modmenu_ch1to4.csx,diffops_installfiles\Scripts\diffops_ch4.csx" --verbose false --output "chapter4_windows\data.win"
echo Finished installation.
pause