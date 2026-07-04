# ADR 001: Tecnología de interfaz de usuario

## Contexto

PitStop VR necesita una interfaz de usuario de escritorio para Windows. Debe integrarse bien con el sistema operativo, permitir acceso a archivos, registro y procesos locales.

## Decisión

Usar WPF sobre .NET 9 con Visual Studio 2022 Community.

## Alternativas consideradas

- **MAUI**: orientado a aplicaciones multiplataforma. Para Windows puro es más pesado y menos maduro que WPF.
- **WinUI 3**: tecnología moderna, pero más volátil y con menos documentación práctica para herramientas de sistema.
- **Consola**: suficiente para prototipos, pero el producto final necesita UI amigable.
- **WPF**: maduro, bien soportado en Visual Studio, ideal para herramientas de escritorio Windows que acceden a registro y archivos.

## Consecuencias

- La aplicación solo funcionará en Windows.
- Aprovechamos el diseñador de Visual Studio y XAML.
- Podemos migrar a WinUI en el futuro si es necesario.

## Estado

Aceptada.
