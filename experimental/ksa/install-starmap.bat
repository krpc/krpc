@echo off
rem Download StarMap and point it at the KSA install, on Windows.
rem
rem STARMAP_DIR is the install folder, defaulting to ..\StarMap beside the repo.
rem KSA_DIR is the game install, defaulting to ..\KSA beside the repo.
setlocal enableextensions

set "here=%~dp0"
set "here=%here:~0,-1%"
call "%here%\versions.bat"
set "url=https://github.com/StarMapLoader/StarMap/releases/download/%starmap_version%/StarMap-%starmap_version%.zip"
for %%d in ("%here%\..\..\..") do set "workspace=%%~fd"
if not defined STARMAP_DIR set "STARMAP_DIR=%workspace%\StarMap"
if not defined KSA_DIR set "KSA_DIR=%workspace%\KSA"
for %%d in ("%STARMAP_DIR%") do set "starmap_dir=%%~fd"
for %%d in ("%KSA_DIR%") do set "ksa_dir=%%~fd"

if not exist "%ksa_dir%\KSA.dll" (
  echo KSA.dll not found in %ksa_dir%. Set KSA_DIR to the KSA install. 1>&2
  exit /b 1
)

set "zip=%TEMP%\StarMap-%starmap_version%-%RANDOM%.zip"
curl.exe -sSfL -o "%zip%" "%url%" || goto fail
set "sha256="
for /f "skip=1 delims=" %%h in ('certutil -hashfile "%zip%" SHA256') do if not defined sha256 set "sha256=%%h"
set "sha256=%sha256: =%"
if /i not "%sha256%"=="%starmap_sha256%" (
  echo %zip%: sha256 mismatch 1>&2
  goto fail
)

if exist "%starmap_dir%" rmdir /s /q "%starmap_dir%"
mkdir "%starmap_dir%"
tar -x -f "%zip%" -C "%starmap_dir%" || goto fail
del /q "%zip%"
rem JSON escapes the backslashes in the path
set "json_ksa_dir=%ksa_dir:\=\\%"
(
  echo {
  echo   "GameLocation": "%json_ksa_dir%",
  echo   "RepositoryLocation": "",
  echo   "GameArguments": []
  echo }
) > "%starmap_dir%\StarMapConfig.json"
echo Installed StarMap %starmap_version% into %starmap_dir%
exit /b 0

:fail
del /q "%zip%" 2>nul
exit /b 1
