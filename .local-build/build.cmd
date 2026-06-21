@echo off
CHCP 1252
setlocal

:: -------------------------------------------------------
:: Aufruf:  build.cmd [Release|Debug]
:: Default: Release
:: -------------------------------------------------------
set "CONFIGURATION=%~1"
if not defined CONFIGURATION set "CONFIGURATION=Release"

if /i "%CONFIGURATION%" neq "Release" if /i "%CONFIGURATION%" neq "Debug" (
    echo [FEHLER] Unbekannte Konfiguration: "%CONFIGURATION%"
    echo          Erlaubt: Release, Debug
    exit /b 1
)

set "SLNX_FILE="
for %%F in ("%~dp0..\*.slnx") do (
    set "SLNX_FILE=%%F"
)

if not defined SLNX_FILE (
    echo [FEHLER] Keine .slnx-Datei in "%~dp0..\" gefunden.
    exit /b 1
)

echo [INFO] Solution gefunden: %SLNX_FILE%
echo [INFO] Starte dotnet build --configuration %CONFIGURATION% ...
echo.

dotnet build "%SLNX_FILE%" --configuration %CONFIGURATION%

if %ERRORLEVEL% neq 0 (
    echo.
    echo [FEHLER] Build fehlgeschlagen. Exit-Code: %ERRORLEVEL%
    exit /b %ERRORLEVEL%
)

echo.
echo [OK] Build erfolgreich abgeschlossen ^(%CONFIGURATION%^).
exit /b 0
