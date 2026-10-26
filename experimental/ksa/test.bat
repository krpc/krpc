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
if not exist "%venv%\Lib\site-packages\krpc-%krpc_version%.dist-info" (
  if exist "%venv%" rmdir /s /q "%venv%"
  python -m venv "%venv%" || exit /b 1
  "%venv%\Scripts\pip.exe" install --quiet --disable-pip-version-check "krpc==%krpc_version%" pytest || exit /b 1
)

cd /d "%here%"
"%venv%\Scripts\python.exe" -m pytest -p no:cacheprovider -c test\pytest.ini %*
