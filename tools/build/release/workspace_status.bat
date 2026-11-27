@echo off
rem The commit, for the VERSION.txt of each package, marked -dirty when tracked files differ
rem from it. Prints nothing outside a git checkout.
set commit=
for /f %%i in ('git rev-parse HEAD 2^>nul') do set commit=%%i
if not defined commit exit /b 0
git diff --quiet HEAD -- 2>nul || set commit=%commit%-dirty
echo STABLE_GIT_COMMIT %commit%
exit /b 0
