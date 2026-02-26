# Game-Save-Value-Reader-V1

A minimal C# WPF desktop application that validates a search-and-parse pipeline for game save files.

## Purpose

Given a game name and a binary save file, the application:

1. **Identifies** the game (user-entered name, normalised)
2. **Searches** public community sources for documented save-file structure (offset, data type, value name)
3. **Parses** the binary save file at that offset and displays the result as `ValueName: Amount`

No value modification — read-only in V1.

## Solution Structure

```
src/
  GameSaveValueReader.Core/       – Platform-agnostic pipeline modules
    Modules/GameIdentification/   – Step 1: game name normalisation
    Modules/SaveStructureSearch/  – Step 2: local knowledge base + GitHub HTTP search
    Modules/SaveParse/            – Step 3: binary file reader
  GameSaveValueReader/            – WPF front-end (Windows only)
tests/
  GameSaveValueReader.Tests/      – xUnit tests for all three modules
```

## Building

Requires .NET 8 SDK or later.

```bash
dotnet build
dotnet test
```

The WPF application (`src/GameSaveValueReader`) targets `net8.0-windows` and must be run on Windows.

## Supported Games (built-in knowledge base)

| Game | Value | Data Type |
|------|-------|-----------|
| Fable: The Lost Chapters | Gold | int32 |
| Fable Anniversary | Gold | int32 |
| Gothic | Ore | int32 |
| Gothic 2 | Ore | int32 |
| Baldur's Gate | Gold | int32 |
| Baldur's Gate 2 | Gold | int32 |

Additional games can be added to `src/GameSaveValueReader.Core/Resources/games.json`.
