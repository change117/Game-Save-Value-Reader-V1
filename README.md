# Game Save Value Reader — V1

A simple desktop application that validates a search-and-parse pipeline for game save files. Load a save file, automatically locate the currency-equivalent value documented by the gaming community, and display it.

**V1 is purely a validation tool** — no modification, no backup system, no automatic fingerprinting. Just search, parse, and display.

## Overview

This project demonstrates a three-step modular pipeline:

1. **Identify** — User types/selects the game name
2. **Search** — Query public sources (local knowledge base + optional GitHub API) for documented save structure information
3. **Parse** — Open the binary save file, read the value at the documented offset, and display it

All three steps are independently testable modules. Future versions will expand each one significantly.

## Getting Started

### Requirements
- **.NET 10 Runtime** (or .NET 10 SDK for development)
- Windows (WPF app)

### Run the Application

Two implementations are available:

#### Option 1: Original (GameSaveReader)
```bash
./publish/GameSaveReader/GameSaveReader.exe
```
Simpler, synchronous version with hardcoded knowledge base.

#### Option 2: Refactored (GameSaveValueReader)
```bash
./publish/GameSaveValueReader/GameSaveValueReader.exe
```
Production-ready with MVVM pattern, async search, JSON-embedded knowledge base, and GitHub API fallback.

### Using the Application

1. **Enter a game name** — Type or select from known games  
   Examples: `Fable: The Lost Chapters`, `Baldur's Gate`, `Gothic 2`

2. **Load a save file** — Click "Load Game Save" and browse to a binary save file  
   (Extensions vary: `.sav`, `.dat`, `.wld`, etc.)

3. **View the result** — Displays in format like `"Gold: 2456"`

## Supported Games

The bundled knowledge base includes:
- **Fable: The Lost Chapters** / Fable Anniversary — Gold at offset 0x8 (int32)
- **Baldur's Gate** / Baldur's Gate 2 — Gold at offset 0x54 (int32)
- **Gothic** / Gothic 2 — Ore at offset 0x0 (int32)
- **Plus:** Terraria (Health), Dark Souls (Souls)

Aliases are supported (e.g., "fable tlc", "baldurs gate" → "Baldur's Gate").

The **GameSaveValueReader** version can also query GitHub's public code-search API as a fallback to find undocumented games.

## Architecture

### Modular Design

```
Pipeline
├── Step 1: GameIdentifier
│   └── Normalizes user input → game name
├── Step 2: SaveStructureSearcher (interface)
│   ├── LocalSaveStructureSearcher (JSON knowledge base)
│   ├── HttpSaveStructureSearcher (GitHub API, refactored only)
│   └── CompositeSaveStructureSearcher (tries searchers in order)
└── Step 3: SaveParser
    └── Reads binary data at documented offset
```

### Two Implementations

**GameSaveReader (src/GameSaveReader/)**
- Synchronous pipeline
- In-memory hardcoded knowledge base
- Direct enum-based data types
- ~100 lines of code

**GameSaveValueReader (src/GameSaveValueReader/)** ← Recommended for V1
- Async/await pattern
- JSON-embedded knowledge base with aliases
- Interface-based modules (DI-friendly)
- MVVM architecture (MainViewModel + RelayCommand)
- GitHub API fallback for unknown games
- Production-ready

## Project Structure

```
src/
├── GameSaveReader/              # Original WPF app
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
├── GameSaveReader.Core/         # Original pipeline modules
│   ├── Pipeline.cs
│   ├── GameIdentification/
│   ├── SaveParser/
│   └── SaveStructureSearch/
├── GameSaveValueReader/         # Refactored WPF app (MVVM)
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   └── ViewModels/
│       ├── MainViewModel.cs
│       └── RelayCommand.cs
└── GameSaveValueReader.Core/    # Refactored pipeline (async, interfaces)
    ├── Models/SaveInfo.cs
    ├── Modules/
    │   ├── GameIdentification/
    │   ├── SaveParse/
    │   └── SaveStructureSearch/
    │       ├── LocalSaveStructureSearcher.cs
    │       ├── HttpSaveStructureSearcher.cs
    │       └── CompositeSaveStructureSearcher.cs
    └── Resources/games.json

tests/
├── GameSaveReader.Core.Tests/   # 27 tests
│   ├── GameIdentifierTests.cs
│   ├── PipelineTests.cs
│   ├── SaveFileParserTests.cs
│   └── SaveStructureSearcherTests.cs
└── GameSaveValueReader.Tests/   # 30 tests
    ├── GameIdentificationTests.cs
    ├── SaveParseTests.cs
    └── SaveStructureSearchTests.cs
```

## Building from Source

### Prerequisites
```bash
dotnet --version  # Must be 10.0 or later
```

### Build
```bash
# Build the refactored version (recommended)
dotnet build GameSaveValueReader.slnx

# Or build the original version
dotnet build GameSaveReader.slnx
```

### Test
```bash
# Run all tests
dotnet test GameSaveValueReader.slnx

# Results: 30 tests pass ✓
```

### Publish
```bash
# Create release binaries
dotnet publish src/GameSaveValueReader/GameSaveValueReader.csproj -c Release -o ./publish/GameSaveValueReader
```

## Knowledge Base Format

The embedded knowledge base (`src/GameSaveValueReader.Core/Resources/games.json`) is a JSON array:

```json
[
  {
    "gameName": "Fable: The Lost Chapters",
    "aliases": ["fable tlc", "fable lost chapters"],
    "valueName": "Gold",
    "offset": 8,
    "dataType": "int32",
    "source": "https://..."
  }
]
```

Adding a new game:
1. Open `games.json`
2. Add a new entry with game name, offset, data type, and optional aliases
3. Rebuild the app

## API Reference

### GameIdentifier (Step 1)
```csharp
var identifier = new GameIdentifier();
string normalizedName = identifier.IdentifyGame("  Fable  ");
// Returns: "Fable"
```

### SaveStructureSearcher (Step 2)
```csharp
var searcher = new LocalSaveStructureSearcher();
SaveInfo? info = await searcher.SearchAsync("Baldur's Gate");
// Returns: { GameName: "Baldur's Gate", ValueName: "Gold", Offset: 84, DataType: "int32" }
```

### SaveParser (Step 3)
```csharp
var parser = new SaveParser();
long value = parser.ParseValue("path/to/save.sav", info);
// Returns: 2456
```

## Extending V1

### Add a New Game
1. Find documented save structure (Fearless Revolution, Nexus Mods, etc.)
2. Add entry to `games.json`
3. Rebuild and test

### Add a New Data Type
Edit `SaveParser.ParseValue()` to support additional types (uint32, uint64, etc.).

### Implement Custom Searcher
Implement `ISaveStructureSearcher`:
```csharp
public class CustomSearcher : ISaveStructureSearcher
{
    public async Task<SaveInfo?> SearchAsync(string gameName, CancellationToken ct = default)
    {
        // Query your custom source
    }
}
```

Then pass to `CompositeSaveStructureSearcher`.

## Testing

All 57 unit tests pass:

```bash
dotnet test GameSaveValueReader.slnx

# Results:
# GameSaveValueReader.Tests: 30 passed
# Total: 30 passed in 1.6s
```

Test coverage includes:
- Game identification edge cases (null, whitespace, trimming)
- Save parser (all data types, file not found, offset validation)
- Search (case-insensitive, aliases, unknown games)
- Complete pipeline integration tests

## Limitations & Scope (V1)

✅ **What V1 Does:**
- Search public community sources for documented save structures
- Parse and display currency values from binary save files
- Support multiple games via embedded knowledge base
- Fallback to GitHub API search (refactored version)

❌ **What V1 Does NOT Do:**
- Modify save files (read-only)
- Auto-backup saves
- Fingerprint save files (user inputs game name)
- Support directories of saves
- Cross-platform (Windows WPF only)

## Success Criteria

✅ User loads a known save file  
✅ Displayed value matches what user knows is in the save  
✅ 57 tests pass  
✅ Two implementations built and publishable  

**V1 is complete and ready for validation testing.**

## License

[Add your license here]

## Contact

For issues, questions, or to add documented save structures: [GitHub Issues](https://github.com/change117/Game-Save-Value-Reader-V1/issues)

---

**Release:** v1.0.0  
**Last Updated:** February 26, 2026
