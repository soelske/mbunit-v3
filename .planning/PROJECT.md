# Gallio.AutoCAD .NET 8 Migration

## What This Is

Migratie van de Gallio AutoCAD extensie naar .NET 8 (net8.0-windows), als onderdeel van de bredere Gallio/MbUnit .NET Core migratie. Het patroon is gevestigd: nieuwe "Core" SDK-style projecten includeren source via wildcards vanuit de originele .NET Framework projecten, zodat beide versies naast elkaar blijven bestaan.

## Core Value

`Gallio.AutoCAD.UI.Core` compileert zonder errors op net8.0-windows, zodat de AutoCAD extensie volledig bruikbaar is in de .NET 8 omgeving.

## Requirements

### Validated

- Gallio.AutoCAD.Core compileert: ✓ 0 errors (38 warnings) — existing
- Gallio.AutoCAD.Plugin250 compileert: ✓ 0 errors — existing (AutoCAD 2025, AutoCAD.NET 25.1)

### Active

- [ ] Gallio.AutoCAD.UI.Core project aanmaken (SDK-style, net8.0-windows, UseWindowsForms=true)
- [ ] Source wildcards vanuit Gallio.AutoCAD.UI via Compile Include
- [ ] Incompatibele bestanden uitsluiten of vervangen
- [ ] References naar Gallio.UI.Core en Gallio.AutoCAD.Core
- [ ] 0 build errors

### Out of Scope

- Gallio.AutoCAD.Tests.Core — tests later
- Plugin versies 170/180/190/210/220/230 — oudere AutoCAD versies blijven .NET Framework
- Warnings reduceren — migratie focus is 0 errors, niet 0 warnings

## Context

- Bestaand patroon (Gallio.Core, Gallio.UI.Core, MbUnit.Core, Gallio.Icarus.Core):
  - `EnableDefaultCompileItems=false`
  - `ImplicitUsings=disable`
  - `GenerateAssemblyInfo=false`
  - Wildcard: `<Compile Include="..\OldProject\**\*.cs" Link="..." />`
  - Replacement .cs bestanden in de Core project directory zelf
- Gallio.AutoCAD.UI bevat: `ControlPanel/StartupPreferencePane.cs/.Designer.cs/.resx` + `StartupPreferencePaneProvider.cs`
- Dependencies: Gallio.UI (old) → Gallio.UI.Core (new), Gallio.AutoCAD (old) → Gallio.AutoCAD.Core (new)
- Gallio.UI.Core.csproj bestaat al en compileert (gebruikt als referentie)

## Constraints

- **Tech stack**: net8.0-windows ONLY — geen plain net8.0
- **UseWindowsForms**: true — WinForms UserControl
- **Coexistence**: originele Gallio.AutoCAD.UI ongewijzigd laten
- **Patroon**: identiek aan andere Core projecten in dit project

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Wildcard source include | Consistent met Gallio.Core/UI.Core/Icarus.Core patroon | — Pending |
| EnableDefaultCompileItems=false | Voorkomt CS0101 duplicates | — Pending |
| ImplicitUsings=disable | Timer ambiguity workaround (consistent met andere Core projects) | — Pending |

---
*Last updated: 2026-03-05 after initialization*
