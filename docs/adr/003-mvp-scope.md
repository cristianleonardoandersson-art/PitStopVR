# ADR 003: Alcance del MVP

## Contexto

PitStop VR tiene una visión grande, pero debemos entregar algo funcional antes de construir todos los módulos.

## Decisión

El MVP incluye:

- VR Inspector: detectar hardware básico, SteamVR, Meta Quest Link y juegos de Steam.
- Knowledge Engine: cargar perfiles desde JSON.
- Configuration Engine: aplicar perfiles de SteamVR, OpenXR, Oculus Debug Tool y argumentos de juego, siempre con backup previo.
- PitStop App: seleccionar juego, seleccionar perfil, aplicar y lanzar.

## Lo que NO incluye el MVP

- IA.
- Benchmark automatizado.
- Performance Monitor en tiempo real.
- Soporte para juegos fuera de Steam.
- Marketplace.
- Instalador.

## Criterio de salida del MVP

Poder elegir un juego de Steam, elegir un perfil, aplicar la configuración y lanzar el juego en modo VR.

## Estado

Aceptada.
