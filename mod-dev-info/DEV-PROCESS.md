### Dev Process

This is an outline of my general development workflow for:
- my own future reference
- anyone who ever wants to pick up some work for this (fork repo and then create a PR)
- anyone who wants to use this project as a reference point for their own mod development

**Requirements:**
- Clone of repo (git, Git GUI or GitHub Desktop recommended)
- Deltarune and/or Deltarune Demo
  - content folder: go to steam > right click Deltarune > Properties > Installed Files > Browse...
  - save folder (Windows): press Win+R, enter `%localappdata%/deltarune`

**Dev process:**
- Fetch from origin
- Make new branch off main (e.g. fix-some-issue, add-some-feature, 192-release, etc)
- Make edits to src files
  - if adding new difficulty options, remember to update the readme and compatibility guide
  - if adding new features to ModMenu, remember to update usage guide and add_modmenu.csx (if affected)
- Apply scripts, test, fix, repeat
  - individual chapter: open data.win in [UTMT](https://github.com/UnderminersTeam/UndertaleModTool/) & run both scripts
  - all chapters: run install-windows.cmd, install-macos.command, or install-linux-proton.sh; as appropriate
  - revert changes: check `ModBackups\` in the content folder for backups, simply copy paste the `chapter#_windows` folders
- Debugging
  - make manual edits to the data.win code to test scenarios/print debug info
  - or; could try [GameMakerMem](https://gamebanana.com/tools/22912) (I've not given it a go yet but looks cool)

**Deployment:**
- Update version strings
  - Custom Difficulty: `README.md` & `release/datapack_dfficulty_dr-fullgame(demo)`
  - ModMenu: `release/datapack_modmenu_dr-fullgame(demo)`
- Make sure `release/references-files_vanilla_dr-fullgame(demo)` are up to date with latest from steam
  - also make sure the meta.toml checksums are up to date [checksum calculator](https://emn178.github.io/online-tools/sha256_checksum.html)
- Run scripts for full-game & demo
- Copy modded data.wins to:
  - Custom Difficulty: `release/datapack_difficulty_dr-fullgame(demo)`
  - ModMenu: `release/datapack_modmenu_dr-fullgame(demo)`
- Run `release/generate-deploy-files-windows.bat` to generate xdeltas (download xdelt3.exe and place in `release/`)
- Copy all required files to `release/installer_difficulty_dr-both` and/or `release/scripts_modmenu_dr-both`
- Zip all datapacks/installers/scripts to output, append _v#-#-# (version string) to zips
- Test data-packs on G3M or Deltamod for full game & demo
- Push branch, create pr
- Draft release
  - copy title and description format from last release
  - upload all deployment files
- Merge pr
- Publish release
- Upload files to GameBanana page
- Add update notes to GameBanana page