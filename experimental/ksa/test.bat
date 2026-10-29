@echo off
rem Run the KSA integration tests, on Windows. Builds and installs the mod with the
rem TestingTools service, then runs pytest over test\. Arguments go to pytest, for example:
rem
rem   test.bat                                 launch KSA, run every test, quit
rem   test.bat test\test_orbit.py -v           one file
rem   test.bat -k test_rename                  by name
rem   test.bat --no-launch                     against a game started with run-ksa.bat
rem
rem A launched game is saved as krpctest after loading, and each test class loads that
rem save. --no-launch needs that save, from an earlier launched run.
setlocal enableextensions

set "here=%~dp0"
set "here=%here:~0,-1%"
call "%here%\versions.bat"

call "%here%\build.bat" --install --testing || exit /b 1

set "venv=%here%\bin\test-venv"
set "site_packages=%venv%\Lib\site-packages"
set "reinstall="
if not exist "%site_packages%\krpc-%krpc_version%.dist-info" set reinstall=1
if not exist "%site_packages%\grpc_tools" set reinstall=1
if not exist "%site_packages%\PIL" set reinstall=1
if defined reinstall (
  if exist "%venv%" rmdir /s /q "%venv%"
  python -m venv "%venv%" || exit /b 1
  "%venv%\Scripts\pip.exe" install --quiet --disable-pip-version-check "krpc==%krpc_version%" pytest grpcio grpcio-tools pillow || exit /b 1
)
rem Python stubs for the gRPC tests, which import them from bin\grpc-python
set "grpc_python=%here%\bin\grpc-python"
if exist "%grpc_python%" rmdir /s /q "%grpc_python%"
mkdir "%grpc_python%"
"%venv%\Scripts\python.exe" -m grpc_tools.protoc "-I%here%\bin\stubs\grpc" ^
  "--python_out=%grpc_python%" "--grpc_python_out=%grpc_python%" "%here%\bin\stubs\grpc\KSA.proto" "%here%\bin\stubs\grpc\StdLib.proto" || exit /b 1

cd /d "%here%"
"%venv%\Scripts\python.exe" -m pytest -p no:cacheprovider -c test\pytest.ini %*
