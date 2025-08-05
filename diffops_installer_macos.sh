echo "Installing difficulty options for all chapters..."
./diffops_installfiles/UTMT_CLI_v0.8.3.0-macOS/UndertaleModCli load chapter1_mac/game.ios --scripts diffops_installfiles\Scripts\modmenu_ch1to4.csx --verbose false --output chapter1_mac/game.ios
./diffops_installfiles/UTMT_CLI_v0.8.3.0-macOS/UndertaleModCli load chapter2_mac/game.ios --scripts diffops_installfiles\Scripts\modmenu_ch1to4.csx --verbose false --output chapter2_mac/game.ios
./diffops_installfiles/UTMT_CLI_v0.8.3.0-macOS/UndertaleModCli load chapter3_mac/game.ios --scripts diffops_installfiles\Scripts\modmenu_ch1to4.csx --verbose false --output chapter3_mac/game.ios
./diffops_installfiles/UTMT_CLI_v0.8.3.0-macOS/UndertaleModCli load chapter4_mac/game.ios --scripts diffops_installfiles\Scripts\modmenu_ch1to4.csx --verbose false --output chapter4_mac/game.ios
./diffops_installfiles/UTMT_CLI_v0.8.3.0-macOS/UndertaleModCli load chapter1_mac/game.ios --scripts diffops_installfiles/Scripts/diffops_ch1.csx --verbose false --output chapter1_mac/game.ios
./diffops_installfiles/UTMT_CLI_v0.8.3.0-macOS/UndertaleModCli load chapter2_mac/game.ios --scripts diffops_installfiles/Scripts/diffops_ch2.csx --verbose false --output chapter2_mac/game.ios
./diffops_installfiles/UTMT_CLI_v0.8.3.0-macOS/UndertaleModCli load chapter3_mac/game.ios --scripts diffops_installfiles/Scripts/diffops_ch3.csx --verbose false --output chapter3_mac/game.ios
./diffops_installfiles/UTMT_CLI_v0.8.3.0-macOS/UndertaleModCli load chapter4_mac/game.ios --scripts diffops_installfiles/Scripts/diffops_ch4.csx --verbose false --output chapter4_mac/game.ios
echo "Finished installation."
read -p "Press enter to continue"
