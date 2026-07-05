# Instalación portable de PitStop VR

Esta guía explica cómo generar y usar el instalador portable de PitStop VR para la PC gamer. No requiere que esté instalado el .NET 9 Runtime en la PC de destino.

## Generar el paquete de instalación

1. Abrí PowerShell en la carpeta raíz del repositorio.
2. Ejecutá:

```powershell
.\publish.ps1 -Zip
```

3. Al finalizar tendrás:
   - `publish/` — carpeta con el ejecutable y los scripts de instalación.
   - `PitStopVR-v1.0.0-win-x64.zip` — paquete listo para distribuir.

### Opciones del script

```powershell
.\publish.ps1 -Configuration Release -SelfContained -Zip
```

| Parámetro | Descripción | Valor por defecto |
|---|---|---|
| `-Configuration` | `Release` o `Debug` | `Release` |
| `-SelfContained` | Incluye el runtime de .NET en el ejecutable | `true` |
| `-Zip` | Crea un archivo ZIP además de la carpeta `publish` | `false` |
| `-OutputDirectory` | Carpeta de salida | `publish` |

## Instalar en la PC gamer

1. Copiá la carpeta `publish` o el archivo ZIP a la PC gamer.
2. Si usaste ZIP, descomprimilo.
3. Dentro de la carpeta `publish`, hacé doble clic en **`instalar.bat`**.
4. El acceso directo aparecerá en el menú Inicio como **PitStop VR**.

La instalación se guarda en:

```text
%LOCALAPPDATA%\PitStopVR
```

## Desinstalar

1. Abrí la carpeta de instalación:

```text
%LOCALAPPDATA%\PitStopVR
```

2. Ejecutá **`desinstalar.bat`**.

Esto elimina la carpeta de instalación y el acceso directo del menú Inicio.

## Requisitos en la PC gamer

Con `-SelfContained` (opción por defecto):
- Windows 10/11 de 64 bits.
- **No se necesita instalar .NET 9 Runtime.**

Sin `-SelfContained`:
- .NET 9 Desktop Runtime: https://dotnet.microsoft.com/en-us/download/dotnet/9.0

## Dependencias opcionales para captura de métricas reales

Para usar PitStopVR en modo real (no simulado), la PC gamer también necesita:

| Componente | Para qué sirve | Link |
|---|---|---|
| Steam + SteamVR | Métricas de cualquier visor SteamVR | https://store.steampowered.com/app/250820/SteamVR/ |
| ADB (Platform Tools) | Conectar con Meta Quest | https://developer.android.com/tools/releases/platform-tools |
| OVR Metrics Tool | Métricas internas del Quest | https://developer.oculus.com/downloads/package/ovr-metrics-tool/ |

La app incluye una ventana **"Verificar ecosistema"** que detecta automáticamente qué componentes faltan y ofrece links de descarga.

## Actualizar a una nueva versión

Ejecutá nuevamente:

```powershell
.\publish.ps1 -Zip
```

Y volvé a instalar con `instalar.bat` en la PC gamer. El instalador reemplaza la versión anterior.

## Solución de problemas

### El antivirus bloquea el ejecutable

El ejecutable self-contained empaqueta bibliotecas nativas. Algunos antivirus pueden detectarlo como sospechoso. Agregá una excepción para `PitStopVR.App.exe`.

### El modo real no captura métricas

1. Abrí **PitStop VR**.
2. Hacé clic en **Verificar ecosistema**.
3. Revisá qué dependencias faltan y seguí los links para instalarlas.
4. Configurá las rutas de ADB y SteamVR en **Configuración** si no se detectaron automáticamente.

### Error al ejecutar `publish.ps1`

Asegurate de tener instalado el **.NET 9 SDK**:

```powershell
dotnet --version
```

Si no aparece la versión, descargalo desde:
https://dotnet.microsoft.com/en-us/download/dotnet/9.0
