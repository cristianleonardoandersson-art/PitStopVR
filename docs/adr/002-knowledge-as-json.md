# ADR 002: Conocimiento externo en JSON

## Contexto

PitStop VR necesita almacenar perfiles de configuración, reglas de detección y conocimiento sobre hardware y juegos. Este conocimiento cambia con frecuencia y no debe requerir recompilar la aplicación.

## Decisión

Todo el conocimiento vive fuera del código en archivos JSON dentro de la carpeta `knowledge/`.

## Alternativas consideradas

- **Código embebido**: rápido al inicio, pero cualquier cambio requiere recompilar y publicar.
- **Base de datos SQLite**: útil para datos de usuario, pero los perfiles base deben ser legibles y editables con cualquier editor de texto.
- **XML**: válido, pero más verboso y menos amigable que JSON.
- **JSON**: ligero, legible, fácil de versionar con Git, editable manualmente.

## Consecuencias

- Los perfiles se pueden ajustar sin tocar código.
- La comunidad puede contribuir perfiles.
- Se requiere validar el JSON al cargarlo.
- Se debe copiar la carpeta `knowledge/` junto con el ejecutable en el instalador.

## Estado

Aceptada.
