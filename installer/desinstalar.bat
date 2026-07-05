@echo off
setlocal EnableDelayedExpansion

echo ============================================
echo  Desinstalador de PitStop VR
echo ============================================
echo.

set "INSTALL_DIR=%LOCALAPPDATA%\PitStopVR"
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
set "SHORTCUT_NAME=PitStop VR.lnk"

if not exist "%INSTALL_DIR%" (
    echo PitStop VR no parece estar instalado en %INSTALL_DIR%.
    pause
    exit /b 0
)

echo Eliminando archivos de instalacion...
rmdir /S /Q "%INSTALL_DIR%"

if exist "%START_MENU%\%SHORTCUT_NAME%" (
    echo Eliminando acceso directo del menu Inicio...
    del "%START_MENU%\%SHORTCUT_NAME%"
)

echo.
echo ============================================
echo  PitStop VR fue desinstalado.
echo ============================================
echo.
pause
