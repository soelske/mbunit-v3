# Requirements: Gallio.AutoCAD .NET 8 Migration

**Defined:** 2026-03-05
**Core Value:** Gallio.AutoCAD.UI.Core compileert zonder errors op net8.0-windows

## v1 Requirements

### Project Setup

- [x] **PROJ-01**: Gallio.AutoCAD.UI.Core.csproj aangemaakt als SDK-style project targeting net8.0-windows
- [x] **PROJ-02**: Project gebruikt `EnableDefaultCompileItems=false`, `ImplicitUsings=disable`, `GenerateAssemblyInfo=false`
- [x] **PROJ-03**: `UseWindowsForms=true` ingesteld (WinForms UserControl)

### Source Inclusion

- [x] **SRC-01**: Alle .cs bestanden vanuit Gallio.AutoCAD.UI ingesloten via wildcard Compile Include
- [x] **SRC-02**: `Properties/AssemblyInfo.cs` uitgesloten (GenerateAssemblyInfo=false)
- [x] **SRC-03**: Revision.cs ingesloten via link vanuit `..\Revision.cs`

### Dependencies

- [x] **DEP-01**: ProjectReference naar Gallio.UI.Core.csproj
- [x] **DEP-02**: ProjectReference naar Gallio.AutoCAD.Core.csproj
- [x] **DEP-03**: Eventuele NuGet packages toegevoegd (System.Configuration.ConfigurationManager indien nodig)

### Build

- [ ] **BUILD-01**: Project compileert met 0 errors
- [ ] **BUILD-02**: Incompatibele bestanden uitgesloten of vervangen door .NET 8 equivalenten
- [ ] **BUILD-03**: Bestaande Gallio.AutoCAD.UI.csproj (old-format) blijft ongewijzigd

## v2 Requirements

### Tests

- **TEST-01**: Gallio.AutoCAD.Tests.Core project aanmaken
- **TEST-02**: Bestaande unit tests compileren op net8.0-windows

### Oudere versies

- **PLUG-01**: Plugin versies 210/220/230 Core varianten (als AutoCAD die versies ook op .NET Core draaien)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Warnings reduceren | Focus is 0 errors, warnings zijn acceptabel tijdens migratie |
| Plugin v170/v180/v190 | AutoCAD 17-19 gebruiken .NET Framework, geen .NET 8 support |
| Runtime testing | Vereist AutoCAD installatie, buiten scope van build-migratie |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| PROJ-01 | Phase 1 | Complete |
| PROJ-02 | Phase 1 | Complete |
| PROJ-03 | Phase 1 | Complete |
| SRC-01 | Phase 1 | Complete |
| SRC-02 | Phase 1 | Complete |
| SRC-03 | Phase 1 | Complete |
| DEP-01 | Phase 1 | Complete |
| DEP-02 | Phase 1 | Complete |
| DEP-03 | Phase 1 | Complete |
| BUILD-01 | Phase 2 | Pending |
| BUILD-02 | Phase 2 | Pending |
| BUILD-03 | Phase 2 | Pending |

**Coverage:**
- v1 requirements: 12 total
- Mapped to phases: 12
- Unmapped: 0 ✓

---
*Requirements defined: 2026-03-05*
*Last updated: 2026-03-05 after initial definition*
