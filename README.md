# PitStop VR

Administrador inteligente del ecosistema VR para simracing.

**Lema:** CONFIGURAR UNE VEZ. CORRER SIEMPRE.

## Propósito

PitStop VR detecta el hardware, software VR, juegos instalados y aplica perfiles optimizados para cada simulador con un solo clic.

Elimina la necesidad de configurar manualmente:

- Meta Quest Link
- SteamVR
- OpenXR
- Oculus Debug Tool
- Parámetros de juegos

## Estructura

```
docs/           Documentación y decisiones de arquitectura (ADR)
knowledge/      Conocimiento externo en JSON (perfiles, reglas)
scripts/        Scripts de utilidad
src/            Código fuente
  PitStopVR.Core            Modelos y utilidades compartidas
  PitStopVR.Inspector       Detección de hardware y software VR
  PitStopVR.Knowledge       Carga y validación de perfiles y reglas
  PitStopVR.Configuration   Aplicación de perfiles y backups
  PitStopVR.App             Interfaz de usuario WPF
tests/          Pruebas unitarias con xUnit
```

## Tecnología

- C# 13
- .NET 9
- WPF
- Visual Studio 2022 Community
- xUnit
- JSON
- SQLite (futuro)

## Cómo compilar

```bash
dotnet build PitStopVR.sln
```

## Cómo ejecutar tests

```bash
dotnet test PitStopVR.sln
```

## Flujo de trabajo

1. Desarrollar en esta notebook.
2. Hacer commit y push a GitHub.
3. En la PC gamer, hacer pull y probar con el hardware VR real.

## Reglas del proyecto

1. Nunca pedir dos veces la misma configuración.
2. Todo debe poder automatizarse.
3. Todo debe poder editarse manualmente.
4. Todo cambio importante se documenta en `docs/adr/`.
5. El usuario no debe tocar manualmente OpenXR, SteamVR ni Meta.
6. Todos los perfiles son editables desde `knowledge/profiles/`.
7. El conocimiento vive fuera del código.
8. Arquitectura modular.
9. Diagnóstico inteligente.
10. Backup antes de modificar configuraciones.

## Licencia

Por definir.
