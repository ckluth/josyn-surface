@echo off
CHCP 1252
setlocal

set "LOCAL_BUILD=%~dp0"

echo.
echo === build ===
call "%LOCAL_BUILD%build.cmd"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo.
echo === test ===
call "%LOCAL_BUILD%test.cmd"
if %ERRORLEVEL% neq 0 exit /b %ERRORLEVEL%

echo.
echo [OK] Build - Test erfolgreich.
