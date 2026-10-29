@echo off
rem Launch KSA through StarMap, on Windows.
rem
rem StarMap loads the game into its own process and resolves its files from the working
rem directory. The game's native libraries are found through PATH.
rem
rem KSA_DIR is the game install, defaulting to ..\KSA beside the repo. StarMap reads the game
rem location from its config, which we write on every launch.
setlocal enableextensions

set "here=%~dp0"
set "here=%here:~0,-1%"
for %%d in ("%here%\..\..\..") do set "workspace=%%~fd"
if not defined STARMAP_DIR set "STARMAP_DIR=%workspace%\StarMap"
if not defined KSA_DIR set "KSA_DIR=%workspace%\KSA"
for %%d in ("%KSA_DIR%") do set "ksa_dir=%%~fd"

rem JSON escapes the backslashes in the path
set "json_ksa_dir=%ksa_dir:\=\\%"
(
  echo {
  echo   "GameLocation": "%json_ksa_dir%",
  echo   "RepositoryLocation": "",
  echo   "GameArguments": []
  echo }
) > "%STARMAP_DIR%\StarMapConfig.json" || exit /b 1
echo KSA %ksa_dir%

cd /d "%STARMAP_DIR%" || exit /b 1
set "PATH=%ksa_dir%;%PATH%"
.\StarMap.exe %*
