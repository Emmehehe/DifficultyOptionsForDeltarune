@echo off
setlocal EnableExtensions DisableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "XDELTA_BIN=%SCRIPT_DIR%xdelta3.exe"
if not exist "%XDELTA_BIN%" set "XDELTA_BIN=%SCRIPT_DIR%xdelta.exe"
if not exist "%XDELTA_BIN%" set "XDELTA_BIN=xdelta3"

set "VANILLA_DEMO=%SCRIPT_DIR%references-files_vanilla_dr-demo\data.win"
set "VANILLA_CH1=%SCRIPT_DIR%references-files_vanilla_dr-fullgame\chapter1\data.win"
set "VANILLA_CH2=%SCRIPT_DIR%references-files_vanilla_dr-fullgame\chapter2\data.win"
set "VANILLA_CH3=%SCRIPT_DIR%references-files_vanilla_dr-fullgame\chapter3\data.win"
set "VANILLA_CH4=%SCRIPT_DIR%references-files_vanilla_dr-fullgame\chapter4\data.win"
set "VANILLA_CH5=%SCRIPT_DIR%references-files_vanilla_dr-fullgame\chapter5\data.win"

set "MODMENU_DEMO=%SCRIPT_DIR%datapack_modmenu_dr-demo\data.win"
set "MODMENU_CH1=%SCRIPT_DIR%datapack_modmenu_dr-fullgame\chapter1\data.win"
set "MODMENU_CH2=%SCRIPT_DIR%datapack_modmenu_dr-fullgame\chapter2\data.win"
set "MODMENU_CH3=%SCRIPT_DIR%datapack_modmenu_dr-fullgame\chapter3\data.win"
set "MODMENU_CH4=%SCRIPT_DIR%datapack_modmenu_dr-fullgame\chapter4\data.win"
set "MODMENU_CH5=%SCRIPT_DIR%datapack_modmenu_dr-fullgame\chapter5\data.win"

set "DIFF_DEMO=%SCRIPT_DIR%datapack_difficulty_dr-demo\data.win"
set "DIFF_CH1=%SCRIPT_DIR%datapack_difficulty_dr-fullgame\chapter1\data.win"
set "DIFF_CH2=%SCRIPT_DIR%datapack_difficulty_dr-fullgame\chapter2\data.win"
set "DIFF_CH3=%SCRIPT_DIR%datapack_difficulty_dr-fullgame\chapter3\data.win"
set "DIFF_CH4=%SCRIPT_DIR%datapack_difficulty_dr-fullgame\chapter4\data.win"
set "DIFF_CH5=%SCRIPT_DIR%datapack_difficulty_dr-fullgame\chapter5\data.win"

set "MODMENU_DEMO_OUT=%SCRIPT_DIR%datapack_modmenu_dr-demo\data.xdelta"
set "MODMENU_CH1_OUT=%SCRIPT_DIR%datapack_modmenu_dr-fullgame\chapter1.xdelta"
set "MODMENU_CH2_OUT=%SCRIPT_DIR%datapack_modmenu_dr-fullgame\chapter2.xdelta"
set "MODMENU_CH3_OUT=%SCRIPT_DIR%datapack_modmenu_dr-fullgame\chapter3.xdelta"
set "MODMENU_CH4_OUT=%SCRIPT_DIR%datapack_modmenu_dr-fullgame\chapter4.xdelta"
set "MODMENU_CH5_OUT=%SCRIPT_DIR%datapack_modmenu_dr-fullgame\chapter5.xdelta"

set "DIFF_DEMO_OUT=%SCRIPT_DIR%datapack_difficulty_dr-demo\data.xdelta"
set "DIFF_CH1_OUT=%SCRIPT_DIR%datapack_difficulty_dr-fullgame\chapter1.xdelta"
set "DIFF_CH2_OUT=%SCRIPT_DIR%datapack_difficulty_dr-fullgame\chapter2.xdelta"
set "DIFF_CH3_OUT=%SCRIPT_DIR%datapack_difficulty_dr-fullgame\chapter3.xdelta"
set "DIFF_CH4_OUT=%SCRIPT_DIR%datapack_difficulty_dr-fullgame\chapter4.xdelta"
set "DIFF_CH5_OUT=%SCRIPT_DIR%datapack_difficulty_dr-fullgame\chapter5.xdelta"

echo Generating deploy files...

call :GeneratePatch "ModMenu: Demo" "%VANILLA_DEMO%" "%MODMENU_DEMO%" "%MODMENU_DEMO_OUT%" || goto failed
call :GeneratePatch "ModMenu: Chapter 1" "%VANILLA_CH1%" "%MODMENU_CH1%" "%MODMENU_CH1_OUT%" || goto failed
call :GeneratePatch "ModMenu: Chapter 2" "%VANILLA_CH2%" "%MODMENU_CH2%" "%MODMENU_CH2_OUT%" || goto failed
call :GeneratePatch "ModMenu: Chapter 3" "%VANILLA_CH3%" "%MODMENU_CH3%" "%MODMENU_CH3_OUT%" || goto failed
call :GeneratePatch "ModMenu: Chapter 4" "%VANILLA_CH4%" "%MODMENU_CH4%" "%MODMENU_CH4_OUT%" || goto failed
call :GeneratePatch "ModMenu: Chapter 5" "%VANILLA_CH5%" "%MODMENU_CH5%" "%MODMENU_CH5_OUT%" || goto failed

call :GeneratePatch "Difficulty: Demo" "%VANILLA_DEMO%" "%DIFF_DEMO%" "%DIFF_DEMO_OUT%" || goto failed
call :GeneratePatch "Difficulty: Chapter 1" "%VANILLA_CH1%" "%DIFF_CH1%" "%DIFF_CH1_OUT%" || goto failed
call :GeneratePatch "Difficulty: Chapter 2" "%VANILLA_CH2%" "%DIFF_CH2%" "%DIFF_CH2_OUT%" || goto failed
call :GeneratePatch "Difficulty: Chapter 3" "%VANILLA_CH3%" "%DIFF_CH3%" "%DIFF_CH3_OUT%" || goto failed
call :GeneratePatch "Difficulty: Chapter 4" "%VANILLA_CH4%" "%DIFF_CH4%" "%DIFF_CH4_OUT%" || goto failed
call :GeneratePatch "Difficulty: Chapter 5" "%VANILLA_CH5%" "%DIFF_CH5%" "%DIFF_CH5_OUT%" || goto failed

echo.
echo All patches applied successfully.
pause
exit /b 0

:failed
echo.
echo [ERROR] File generation failed.
pause
exit /b 1

:GeneratePatch
set "name=%~1"
set "old=%~2"
set "new=%~3"
set "out=%~4"

echo.
echo Generating xdelta [%name%]...

if not exist "%old%" (
    echo [ERROR] Missing vanilla reference "%old%"
    exit /b 1
)
if not exist "%new%" (
    echo [ERROR] Missing modded reference "%new%"
    exit /b 1
)
if exist "%out%" del "%out%"

"%XDELTA_BIN%" -e -s "%old%" "%new%" "%out%"
if errorlevel 1 (
    if exist "%out%" del "%out%"
    echo [ERROR] Failed to generate xdelta [%name%].
    exit /b 1
)

echo %name% xdelta generated.
exit /b 0
