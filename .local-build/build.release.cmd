@echo off
CHCP 1252
call "%~dp0build.cmd" Release
exit /b %ERRORLEVEL%
