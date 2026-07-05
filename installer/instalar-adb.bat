@echo off
setlocal EnableDelayedExpansion

echo ============================================
echo  Instalador automatico de ADB (Android Debug Bridge)
echo  para PitStop VR / Meta Quest
echo ============================================
echo.

set "INSTALL_DIR=%LOCALAPPDATA%\Android\Sdk\platform-tools"
set "TEMP_ZIP=%TEMP%\platform-tools-latest-windows.zip"
set "DOWNLOAD_URL=https://dl.google.com/android/repository/platform-tools-latest-windows.zip"

REM Verificar si ya esta instalado
if exist "%INSTALL_DIR%\adb.exe" (
    echo ADB ya esta instalado en:
    echo %INSTALL_DIR%
    echo.
    echo Version detectada:
    "%INSTALL_DIR%\adb.exe" --version
    echo.
    goto :configure_path
)

echo Descargando Android SDK Platform Tools desde Google...
powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Invoke-WebRequest -Uri '%DOWNLOAD_URL%' -OutFile '%TEMP_ZIP%' -UseBasicParsing } catch { Write-Error 'No se pudo descargar ADB. Verifica tu conexion a internet.'; exit 1 }"

if not exist "%TEMP_ZIP%" (
    echo ERROR: No se pudo descargar el archivo.
    pause
    exit /b 1
)

echo Descomprimiendo en %INSTALL_DIR%...
if exist "%INSTALL_DIR%" (
    rmdir /S /Q "%INSTALL_DIR%"
)
mkdir "%INSTALL_DIR%"

powershell -NoProfile -ExecutionPolicy Bypass -Command "try { Expand-Archive -Path '%TEMP_ZIP%' -DestinationPath '%LOCALAPPDATA%\Android\Sdk' -Force } catch { Write-Error 'No se pudo descomprimir el archivo.'; exit 1 }"

if not exist "%INSTALL_DIR%\adb.exe" (
    echo ERROR: adb.exe no se encontro despues de descomprimir.
    pause
    exit /b 1
)

del /F /Q "%TEMP_ZIP%"

echo.
echo ADB se instalo correctamente en:
echo %INSTALL_DIR%
echo.

:configure_path
echo Verificando variable de entorno PATH...

powershell -NoProfile -ExecutionPolicy Bypass -Command "^$
    $installDir = '%INSTALL_DIR%';^$
    $currentPath = [Environment]::GetEnvironmentVariable('Path', 'User');^$
    $paths = $currentPath -split ';' | Where-Object { $_ -ne '' };^$
    if ($paths -contains $installDir) {^$
        Write-Host 'La ruta ya esta en el PATH.' -ForegroundColor Green;^$
    } else {^$
        $newPath = if ($currentPath) { $currentPath + ';' + $installDir } else { $installDir };^$
        [Environment]::SetEnvironmentVariable('Path', $newPath, 'User');^$
        Write-Host 'Ruta agregada al PATH del usuario.' -ForegroundColor Green;^$
    }"

echo.
echo Version instalada:
"%INSTALL_DIR%\adb.exe" --version

echo.
echo ============================================
echo  Instalacion completa.
echo  Reinicia PitStop VR si estaba abierto.
echo ============================================
echo.
echo La ruta para configurar en PitStop VR es:
echo %INSTALL_DIR%\adb.exe
echo.
pause
