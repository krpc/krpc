@echo off
rem Build the kRPC for KSA mod into experimental\ksa\bin\kRPC, on Windows.
rem
rem   build.bat            build the mod folder
rem   build.bat --install  also copy it into the KSA mods folder and enable it
rem   build.bat --testing  also build the TestingTools service, for the tests
rem
rem The mod and the client stubs are built from a kRPC release: the server core and
rem the code generator come from its krpctools package, installed into a virtualenv.
rem
rem KSA_DIR is the game install, defaulting to ..\KSA beside the repo.
rem KSA_MODS_DIR is the user mods folder, defaulting to
rem "<Documents>\My Games\Kitten Space Agency\mods".
setlocal enableextensions

set "here=%~dp0"
set "here=%here:~0,-1%"
call "%here%\versions.bat"
set "out=%here%\bin\kRPC"
if not defined KSA_DIR set "KSA_DIR=%here%\..\..\..\KSA"
for %%d in ("%KSA_DIR%") do set "ksa_dir=%%~fd"

set install=0
set "projects=server\KRPC.KSA.csproj service\KRPC.KSA.Service.csproj"
:args
if "%~1"=="" goto args_done
if "%~1"=="--install" (
  set install=1
) else if "%~1"=="--testing" (
  set "projects=%projects% testing\KRPC.KSA.TestingTools.csproj"
) else (
  echo unknown argument: %~1 1>&2
  exit /b 1
)
shift
goto args
:args_done

set "venv=%here%\bin\venv"
set "site_packages=%venv%\Lib\site-packages"
if not exist "%site_packages%\krpctools-%krpc_version%.dist-info" (
  if exist "%venv%" rmdir /s /q "%venv%"
  python -m venv "%venv%" || exit /b 1
  "%venv%\Scripts\pip.exe" install --quiet --disable-pip-version-check "krpctools==%krpc_version%" || exit /b 1
)
rem The .NET builds of KRPC.Core and the ServiceDefinitions tool
set "krpc_bin=%site_packages%\krpctools\bin"

if exist "%out%" rmdir /s /q "%out%"
for %%p in (%projects%) do (
  dotnet build "%here%\%%p" -c Release -warnaserror -o "%out%" "-p:KSADir=%ksa_dir%\\" "-p:KRPCCoreDll=%krpc_bin%\KRPC.Core.dll" || exit /b 1
)
rem The server reads the API documentation from the XML file beside each assembly
copy /y "%krpc_bin%\KRPC.Core.xml" "%out%\" >nul || exit /b 1
if exist "%out%\KRPC.KSA.TestingTools.dll" (
  copy /y "%here%\testing\settings.toml" "%out%\" >nul || exit /b 1
)
del /q "%out%\*.pdb" 2>nul

rem Generate the service definitions, which runs the server's service scan over the
rem service and checks its documentation
dotnet "%krpc_bin%\ServiceDefinitions.dll" ^
  "--output=%here%\bin\KRPC.KSA.json" "--reference-dir=%ksa_dir%" "--reference-dir=%out%" ^
  KSA "%out%\KRPC.KSA.Service.dll" || exit /b 1

rem Client stubs, at the paths each client includes them from
set "stubs=%here%\bin\stubs"
if exist "%stubs%" rmdir /s /q "%stubs%"
call :stub csharp csharp\KSA.cs || exit /b 1
call :stub cpp cpp\include\krpc\services\ksa.hpp || exit /b 1
call :stub cnano cnano\include\krpc_cnano\services\ksa.h || exit /b 1
call :stub java java\krpc\client\services\KSA.java || exit /b 1
powershell -NoProfile -Command "(Get-Content -Raw '%here%\stubs\README.md').Replace('@CLIENT_VERSION@', '%krpc_version%') | Set-Content -NoNewline -Encoding ascii '%stubs%\README.md'" || exit /b 1
copy /y "%here%\bin\KRPC.KSA.json" "%stubs%\" >nul || exit /b 1
echo Client stubs in %stubs%

if "%install%"=="1" (
  call :install || exit /b 1
)
exit /b 0

:stub
set "path_=%stubs%\%~2"
for %%f in ("%path_%") do if not exist "%%~dpf" mkdir "%%~dpf"
"%venv%\Scripts\krpc-clientgen.exe" %1 KSA "%here%\bin\KRPC.KSA.json" "--output=%path_%"
exit /b

:install
if not defined KSA_MODS_DIR (
  for /f "delims=" %%d in ('powershell -NoProfile -Command "[Environment]::GetFolderPath('MyDocuments')"') do set "KSA_MODS_DIR=%%d\My Games\Kitten Space Agency\mods"
)
for %%d in ("%KSA_MODS_DIR%\..") do set "manifest=%%~fd\manifest.toml"
if exist "%KSA_MODS_DIR%\kRPC" rmdir /s /q "%KSA_MODS_DIR%\kRPC"
if not exist "%KSA_MODS_DIR%" mkdir "%KSA_MODS_DIR%"
xcopy /e /i /q /y "%out%" "%KSA_MODS_DIR%\kRPC" >nul || exit /b 1
findstr /c:"id = \"kRPC\"" "%manifest%" >nul 2>&1
if errorlevel 1 (
  >>"%manifest%" echo.
  >>"%manifest%" echo [[mods]]
  >>"%manifest%" echo id = "kRPC"
  >>"%manifest%" echo enabled = true
)
echo Installed into %KSA_MODS_DIR%\kRPC
exit /b 0
