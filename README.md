# Game Save Value Reader V1

A WPF desktop application that reads and displays currency-equivalent values from game save files.

## Purpose

V1 validates a search-and-parse pipeline: it accepts a game save file, looks up documented save structure information for that game, locates the currency-equivalent value in the save file, and displays it. **No value modification in this version.**

## Architecture

The pipeline is split into three independently testable modules:

1. **Game Identification** (`GameSaveReader.Core.GameIdentification`) — Accepts the game name from user input.
2. **Save Structure Search** (`GameSaveReader.Core.SaveStructureSearch`) — Looks up the offset, data type, and community name of the currency value using a local knowledge base sourced from public modding resources (Fearless Revolution, Nexus Mods, GitHub, modding wikis). Implements `ISaveStructureSearcher` for future expansion to live web search.
3. **Save Parser** (`GameSaveReader.Core.SaveParser`) — Reads the binary save file at the documented offset and data type, returning the value.

## Supported Games (V1 Knowledge Base)

| Game | Value Name | Data Type |
|---|---|---|
| Fable - The Lost Chapters | Gold | Int32 |
| Terraria | Health | Int32 |
| Dark Souls | Souls | Int32 |

## Building

```bash
dotnet build
```

## Testing

```bash
dotnet test tests/GameSaveReader.Core.Tests
```

## Usage

1. Run the WPF application (Windows only).
2. Select or type a game name in the dropdown.
3. Click **Load Game Save** and select the save file.
4. The result displays as `ValueName: Amount`.
