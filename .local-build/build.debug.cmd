@echo off
CHCP 1252
call "%~dp0build.cmd" Debug
exit /b %ERRORLEVEL%
