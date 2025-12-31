# AutoCAD Preferences in Gallio Projects - Implementation Summary

## Overzicht

Deze implementatie breidt Gallio uit om AutoCAD preferences op te slaan in `.gallio` project bestanden. 
Wanneer een gebruiker zonder de AutoCAD plugin Icarus gebruikt, worden deze velden gewoon genegeerd 
(backward compatible).

## Architectuur

### 1. Core Gallio (Platform-agnostisch)

#### `TestProject.cs` (Gallio/Gallio/Runner/Projects/)
Voegt 4 nieuwe properties toe voor AutoCAD preferences:
- `AutoCADCommandLineArguments` (string, nullable)
- `AutoCADStartupAction` (int?, nullable - mapped to enum in AutoCAD project)
- `AutoCADUserSpecifiedExecutable` (string, nullable)
- `AutoCADWorkingDirectory` (string, nullable)

**Belangrijke punten:**
- Gebruikt primitieve types (string, int?) om geen dependency op AutoCAD te hebben
- Nullable values betekenen "niet ingesteld" - backward compatible
- Wordt gekopieerd in `Copy()` methode

#### `TestProjectData.cs` (Gallio/Gallio/Runner/Projects/Schema/)
Voegt XML serialization toe voor dezelfde properties:
- Gebruikt `[XmlElement]` attributes voor XML serialization
- Kopieert waardes van/naar `TestProject` in constructor en `InitializeTestProject()`
- **Geen validatie** - nullable values zijn altijd geldig

### 2. AutoCAD Extension (Platform-specifiek)

#### `TestProjectExtensions.cs` (Extensions/AutoCAD/Gallio.AutoCAD/Projects/)
Extension methods voor `TestProject`:

```csharp
// Laad preferences van TestProject naar IAcadPreferenceManager
testProject.LoadAutoCADPreferences(preferenceManager);

// Sla preferences op van IAcadPreferenceManager naar TestProject
testProject.SaveAutoCADPreferences(preferenceManager);

// Check of project AutoCAD preferences heeft
bool hasPrefs = testProject.HasAutoCADPreferences();
```

**Belangrijke punten:**
- Cast int? naar `StartupAction` enum
- Alleen niet-null waarden worden overgeschreven
- Type-safe conversie tussen runtime en persistence layer

#### `PluginPreferencesSynchronizer.cs` (Extensions/Icarus/Gallio.Icarus/Projects/)
Generic synchronizer die automatisch plugin preferences detecteert via **reflectie**:

```csharp
Handles<SavingProject>   // Voor save: TestProject ? PreferenceManager
Handles<ProjectLoaded>   // Na load: TestProject ? PreferenceManager
```

**Belangrijke punten:**
- Gebruikt **reflectie** om extension methods te vinden (geen harde dependency op AutoCAD)
- Werkt met **elke** plugin die het patroon volgt
- Faalt gracefully als plugin niet geladen is

## Gebruik in Icarus

### Setup (eenmalig bij Icarus startup):
```csharp
// In Icarus initialization (bijv. IcarusProgram.cs of via convention scanning)
var eventAggregator = RuntimeAccessor.ServiceLocator.Resolve<IEventAggregator>();
var projectTreeModel = RuntimeAccessor.ServiceLocator.Resolve<IProjectTreeModel>();

var synchronizer = new PluginPreferencesSynchronizer(projectTreeModel);
eventAggregator.Add(synchronizer);
```

**Voordeel:** Geen code nodig in AutoCAD plugin - werkt automatisch!

### Workflow:

1. **Gebruiker opent project:**
   - `ProjectLoaded` event wordt gefired
   - `PluginPreferencesSynchronizer` detecteert AutoCAD preferences in XML via reflectie
   - Roept `TestProject.LoadAutoCADPreferences()` aan (indien AutoCAD plugin geladen is)
   - Preferences worden geladen naar `IAcadPreferenceManager`
   - Gebruiker ziet AutoCAD settings in UI

2. **Gebruiker wijzigt AutoCAD settings:**
   - Settings worden opgeslagen in `IAcadPreferenceManager` (bestaande code)
   - Nog niet in project file

3. **Gebruiker slaat project op:**
   - `SavingProject` event wordt gefired
   - `PluginPreferencesSynchronizer` roept `TestProject.SaveAutoCADPreferences()` aan
   - Preferences worden gekopieerd van `IAcadPreferenceManager` naar `TestProject`
   - `ProjectController.Save()` serialiseert `TestProject` naar XML via `TestProjectData`
   - AutoCAD preferences staan nu in `.gallio` file

## XML Voorbeeld

```xml
<?xml version="1.0" encoding="utf-8"?>
<testProject xmlns="http://www.gallio.org/">
  <testPackage>
    <!-- ... -->
  </testPackage>
  <testFilters />
  <extensionSpecifications />
  <reportDirectory>Reports</reportDirectory>
  <reportNameFormat>test-report-{0}-{1}</reportNameFormat>
  
  <!-- AutoCAD Preferences (optioneel) -->
  <autoCADCommandLineArguments>/b</autoCADCommandLineArguments>
  <autoCADStartupAction>0</autoCADStartupAction>
  <autoCADUserSpecifiedExecutable>C:\Program Files\Autodesk\AutoCAD 2024\acad.exe</autoCADUserSpecifiedExecutable>
  <autoCADWorkingDirectory>C:\Projects\MyProject</autoCADWorkingDirectory>
</testProject>
```

## Backward Compatibility

**Zonder AutoCAD plugin:**
- XML deserializer slaat onbekende elementen over (of leest ze als null)
- `TestProject` heeft nullable properties - geen null-reference exceptions
- Icarus werkt normaal zonder AutoCAD

**Met AutoCAD plugin:**
- Als geen preferences in XML: properties blijven null
- `HasAutoCADPreferences()` return false
- `LoadAutoCADPreferences()` doet niets
- Gebruiker kan settings instellen die bij volgende save worden opgeslagen

## Testing Checklist

### Unit Tests (TODO):
- [ ] `TestProjectData` serialization/deserialization met AutoCAD properties
- [ ] `TestProjectExtensions.SaveAutoCADPreferences()`
- [ ] `TestProjectExtensions.LoadAutoCADPreferences()`
- [ ] `TestProjectExtensions.HasAutoCADPreferences()`
- [ ] Null handling in alle extension methods

### Integration Tests (TODO):
- [ ] Open project zonder AutoCAD preferences
- [ ] Open project met AutoCAD preferences
- [ ] Wijzig preferences en save project
- [ ] Open project in Icarus zonder AutoCAD plugin (ignore unknown XML)

### Manual Tests:
1. Open Icarus met AutoCAD plugin
2. Create new project
3. Set AutoCAD preferences in Control Panel
4. Save project
5. Close Icarus
6. Open project again
7. Verify AutoCAD preferences are restored

## Voordelen van deze aanpak

? **Geen breaking changes** - bestaande code blijft werken
? **Clean separation** - core Gallio kent geen AutoCAD types  
? **Extensible** - andere plugins kunnen hetzelfde patroon volgen
? **Type-safe** - compile-time checks via extension methods
? **Testable** - alle lagen kunnen apart getest worden
? **Backward compatible** - oude projects blijven werken

## Alternatieven overwogen

### ? Property bag (Dictionary<string, string>)
- **Pro:** Meer flexibel, kan runtime properties toevoegen
- **Con:** Geen type safety, geen intellisense, geen compile-time checks
- **Con:** Moeilijker te documenteren en valideren

### ? AutoCAD-specifieke types in TestProject
- **Pro:** Type-safe, geen casting nodig
- **Con:** Core Gallio krijgt dependency op AutoCAD
- **Con:** Elke extension zou eigen types toevoegen ? mess

### ? Primitieve types + extension methods (gekozen)
- **Pro:** Best of both worlds
- **Pro:** Type safety in AutoCAD project
- **Pro:** Geen dependencies in core
- **Pro:** Volgt bestaande patterns (zoals ReportNameFormat)

## Toekomstige uitbreidingen

Als andere plugins (bijv. VisualStudio, ReSharper) ook preferences willen opslaan:

1. Voeg properties toe aan `TestProject` (primitieve types)
2. Voeg XML serialization toe aan `TestProjectData`  
3. Maak extension methods in je plugin project
4. Maak event handler die `SavingProject`/`ProjectLoaded` handled

**Naming convention:** `{PluginName}{PropertyName}`  
Bijvoorbeeld: `VisualStudioDebuggerPath`, `ReSharperTestRunnerMode`
