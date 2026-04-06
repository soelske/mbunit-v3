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

## NuGet release notes

Elke NuGet release moet voorzien worden van release notes. Deze worden ingebakken in het `.nupkg` bestand bij de build.

**Waar staan ze?**
- `src/Gallio/Gallio.Core/Gallio.Core.csproj` — `<PackageReleaseNotes>`
- `src/MbUnit/MbUnit.Core/MbUnit.Core.csproj` — `<PackageReleaseNotes>`

**Richtlijnen:**
- Schrijf in het **Engels**
- Begin met het versienummer op de eerste regel
- Wees specifiek — vermeld alleen wat daadwerkelijk veranderd is in dat package
- AutoCAD/Icarus fixes horen **niet** in Gallio of MbUnit release notes
- Als er geen functionele wijzigingen zijn: `- Compatibility update with Gallio X.X.X`

**Voorbeeld:**
```
4.0.1
- Fix: Restore TestRunnerFactory and AutoCAD startup settings from .gallio project file on open
- Fix: CachingPluginLoader now detects newly added plugin files in a stale cache
- Fix: Plugin preferences correctly loaded via lazy assembly activation on project open
```

> De `<PackageReleaseNotes>` wordt via de `-ReleaseNotes` parameter doorgegeven aan `Build-MultiTarget-Package.ps1` en ingebakken in het tijdelijke packaging project. Vergeet dit **niet** bij te werken voor elke release, anders verschijnt "(none specified)" op nuget.org.

---

## Copyright encoding

Het copyright symbool `©` moet als XML entity `&#169;` geschreven worden in `.csproj` bestanden en packaging scripts. Het directe UTF-8 karakter `©` veroorzaakt `Â©` op nuget.org door een encoding conflict met PowerShell's `Out-File`.

**Correct:**
```xml
<Copyright>Copyright &#169; 2005-2026 Gallio Project</Copyright>
```

**Fout (veroorzaakt Â© op nuget.org):**
```xml
<Copyright>Copyright © 2005-2026 Gallio Project</Copyright>
```

Dit geldt voor:
- `Gallio.Core.csproj`
- `MbUnit.Core.csproj`
- `Build-MultiTarget-Package.ps1` (Gallio én MbUnit)

---

## Valkuilen

- **Nooit** `Version=` in `fullName` attributen van `.plugin` bestanden aanpassen bij een patch release — dit breekt de assembly binding voor bestaande test DLL's.
- **Nooit** `AssemblyVersion` verhogen tenzij er echt een breaking API change is — anders kunnen gebruikers hun test DLL's niet meer laden zonder rebuild.
- **PackageReference versies** in `.csproj` bestanden (bv. `NHamcrest`) **niet** meenemen bij een versie bump — alleen de `<Version>` van het product zelf.
- Na een `AssemblyVersion` wijziging moeten alle afhankelijke projecten (test DLL's van gebruikers) opnieuw gebouwd worden.
- **Release notes bijwerken** vóór de build — ze worden ingebakken bij compile time, niet bij upload.
