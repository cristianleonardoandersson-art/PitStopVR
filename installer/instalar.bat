@echo off
setlocal EnableDelayedExpansion

echo ============================================
echo  Instalador portable de PitStop VR
echo ============================================
echo.

set "SOURCE_DIR=%~dp0"
set "INSTALL_DIR=%LOCALAPPDATA%\PitStopVR"
set "START_MENU=%APPDATA%\Microsoft\Windows\Start Menu\Programs"
set "SHORTCUT_NAME=PitStop VR.lnk"

if not exist "%SOURCE_DIR%\PitStopVR.App.exe" (
    echo ERROR: No se encontro PitStopVR.App.exe en esta carpeta.
    echo Asegurate de ejecutar este archivo desde la carpeta publicada.
    pause
    exit /b 1
)

echo Instalando en: %INSTALL_DIR%

if exist "%INSTALL_DIR%" (
    echo Eliminando instalacion anterior...
    rmdir /S /Q "%INSTALL_DIR%"
)

mkdir "%INSTALL_DIR%"
xcopy /E /I /Y "%SOURCE_DIR%\*" "%INSTALL_DIR%" > nul

echo Creando acceso directo en el menu Inicio...
powershell -NoProfile -ExecutionPolicy Bypass -Command "^$
    ws = New-Object -ComObject WScript.Shell;^$
    s = ws.CreateShortcut('%START_MENU%\%SHORTCUT_NAME%');^$
    s.TargetPath = '%INSTALL_DIR%\PitStopVR.App.exe';^$
    s.WorkingDirectory = '%INSTALL_DIR%';^$
    s.IconLocation = '%INSTALL_DIR%\PitStopVR.App.exe,0';^$
    s.Save()"

echo.
echo ============================================
echo  PitStop VR se instalo correctamente.
echo  Podes abrirlo desde el menu Inicio.
echo ============================================
echo.
pause
