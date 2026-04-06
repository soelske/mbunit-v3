# Versie verhogen — checklist

## Versienummers uitgelegd

| Attribuut | Doel | Wanneer verhogen |
|---|---|---|
| `AssemblyVersion` | Runtime binding identity — DLL's die gebouwd zijn tegen versie X werken niet meer als deze verandert | Alleen bij **breaking changes** (verwijderde/gewijzigde publieke API) |
| `AssemblyFileVersion` | Zichtbaar in Windows bestandseigenschappen | Bij elke release (patch, minor, major) |
| NuGet `<Version>` | Semantic versioning voor NuGet packages | Bij elke release |
| `<version>` in `.plugin` | Weergave in Gallio plugin browser | Bij elke release |

> **Vuistregel patch release (bv. 4.0.0 → 4.0.1):**
> - `AssemblyVersion` blijft **ongewijzigd** (`4.0.0.0`)
> - `AssemblyFileVersion` wordt `4.0.1.0`
> - NuGet `<Version>` wordt `4.0.1`
> - `.plugin` `<version>` tags worden `4.0.1.0`
> - `.plugin` `fullName` assembly bindings blijven `4.0.0.0`

---

## Revision.cs bestanden

Elk product heeft zijn eigen `Revision.cs` zodat versies onafhankelijk van elkaar kunnen worden verhoogd:

| Bestand | Gebruikt door |
|---|---|
| `src/Revision.cs` | Gallio, MbUnit |
| `src/Extensions/AutoCAD/Revision.cs` | Gallio.AutoCAD plugin |
| `src/Extensions/Echo/Revision.cs` | Gallio.Echo plugin |
| `src/Extensions/Icarus/Revision.cs` | Gallio.Icarus (GUI) |

---

## Bestanden die aangepast moeten worden

### Gallio + MbUnit

- `src/Revision.cs` — `AssemblyFileVersion`
- `src/Gallio/Gallio.Core/Gallio.Core.csproj` — `<Version>`
- `src/MbUnit/MbUnit.Core/MbUnit.Core.csproj` — `<Version>`
- `src/Gallio/Gallio/Gallio.plugin` — `<version>`
- `src/Gallio/Gallio35/Gallio35.plugin` — `<version>`
- `src/Gallio/Gallio40/Gallio40.plugin` — `<version>`
- `src/Gallio/Gallio.UI/Gallio.UI.plugin` — `<version>`
- `src/Gallio/Gallio.Reports/Gallio.Reports.plugin` — `<version>`
- `src/Gallio/Gallio.Reports.Core/Gallio.Reports.plugin` — `<version>`
- `src/MbUnit/MbUnit/MbUnit.plugin` — `<version>`
- `src/MbUnit/MbUnit35/MbUnit35.plugin` — `<version>`
- `src/MbUnit/MbUnit40/MbUnit40.plugin` — `<version>`

### Icarus

- `src/Extensions/Icarus/Revision.cs` — `AssemblyFileVersion` (en `AssemblyVersion` indien breaking)
- `src/Extensions/Icarus/Gallio.Icarus/Gallio.Icarus.plugin` — `<version>`

### AutoCAD plugin

- `src/Extensions/AutoCAD/Revision.cs` — `AssemblyFileVersion`
- `src/Extensions/AutoCAD/Gallio.AutoCAD.Core/Gallio.AutoCAD.Core.csproj` — `<Version>`
- `src/Extensions/AutoCAD/Gallio.AutoCAD.Plugin/Gallio.AutoCAD.Plugin250.csproj` — `<Version>`
- `src/Extensions/AutoCAD/Gallio.AutoCAD/Gallio.AutoCAD.plugin` — `<version>`
- `src/Extensions/AutoCAD/Gallio.AutoCAD.UI/Gallio.AutoCAD.UI.plugin` — `<version>`

### Echo plugin

- `src/Extensions/Echo/Revision.cs` — `AssemblyFileVersion`
- `src/Extensions/Echo/Gallio.Echo/Gallio.Echo.plugin` — `<version>`

---

## Valkuilen

- **Nooit** `Version=` in `fullName` attributen van `.plugin` bestanden aanpassen bij een patch release — dit breekt de assembly binding voor bestaande test DLL's.
- **Nooit** `AssemblyVersion` verhogen tenzij er echt een breaking API change is — anders kunnen gebruikers hun test DLL's niet meer laden zonder rebuild.
- **PackageReference versies** in `.csproj` bestanden (bv. `NHamcrest`) **niet** meenemen bij een versie bump — alleen de `<Version>` van het product zelf.
- Na een `AssemblyVersion` wijziging moeten alle afhankelijke projecten (test DLL's van gebruikers) opnieuw gebouwd worden.
