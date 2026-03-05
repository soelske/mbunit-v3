---
phase: 01-project-setup
plan: 01
subsystem: infra
tags: [dotnet8, winforms, sdk-style, autocad, bridge-project, csproj]

# Dependency graph
requires:
  - phase: none
    provides: n/a
provides:
  - Gallio.AutoCAD.UI.Core.csproj targeting net8.0-windows with UseWindowsForms=true
  - SDK-style bridge for StartupPreferencePane and StartupPreferencePaneProvider
  - net8.0-windows compilation of Gallio.AutoCAD.UI source with 0 errors
affects:
  - 02-project-setup (any future plans referencing Gallio.AutoCAD.UI.Core)

# Tech tracking
tech-stack:
  added: []
  patterns:
    - "SDK-style bridge with EnableDefaultCompileItems=false + wildcard Compile Include"
    - "ImplicitUsings=disable for Timer ambiguity avoidance"
    - "GenerateAssemblyInfo=false + exclude Properties/AssemblyInfo.cs"
    - "AutoCAD-local Revision.cs via ..\Revision.cs (not root src/Revision.cs)"

key-files:
  created:
    - src/Extensions/AutoCAD/Gallio.AutoCAD.UI.Core/Gallio.AutoCAD.UI.Core.csproj
  modified: []

key-decisions:
  - "AssemblyName and RootNamespace set to Gallio.AutoCAD.UI to match original project output"
  - "Revision.cs path is ..\Revision.cs (AutoCAD-local, src/Extensions/AutoCAD/Revision.cs) not root src/Revision.cs"
  - "No NuGet packages required — source only uses System.Windows.Forms APIs already provided by UseWindowsForms=true"
  - "ProjectReferences to Gallio.AutoCAD.Core and Gallio.UI.Core cover all namespace dependencies"

patterns-established:
  - "Core bridge pattern: EnableDefaultCompileItems=false, ImplicitUsings=disable, GenerateAssemblyInfo=false"
  - "Wildcard Include for ..\\OriginalProject\\**\\*.cs with Link metadata"
  - "Explicit Remove for obj/, bin/, and Properties/AssemblyInfo.cs"

requirements-completed: [PROJ-01, PROJ-02, PROJ-03, SRC-01, SRC-02, SRC-03, DEP-01, DEP-02, DEP-03]

# Metrics
duration: 1min
completed: 2026-03-05
---

# Phase 1 Plan 01: Gallio.AutoCAD.UI.Core Bridge Project Summary

**SDK-style net8.0-windows bridge for Gallio.AutoCAD.UI (StartupPreferencePane + StartupPreferencePaneProvider) compiling with 0 errors and 2524 CA1416 warnings**

## Performance

- **Duration:** 1 min
- **Started:** 2026-03-05T21:06:17Z
- **Completed:** 2026-03-05T21:07:18Z
- **Tasks:** 2
- **Files modified:** 1

## Accomplishments
- Created Gallio.AutoCAD.UI.Core.csproj as an SDK-style bridge targeting net8.0-windows
- Build succeeds with 0 errors (2524 CA1416 platform-compat warnings — acceptable for migration phase)
- Original Gallio.AutoCAD.UI/ directory is completely untouched (confirmed via git diff)

## Task Commits

Each task was committed atomically:

1. **Task 1: Create Gallio.AutoCAD.UI.Core.csproj** - `0f4fdb237` (feat)
2. **Task 2: Verify build succeeds with 0 errors** - (build-only verification, no new files)

**Plan metadata:** (docs commit follows)

## Files Created/Modified
- `src/Extensions/AutoCAD/Gallio.AutoCAD.UI.Core/Gallio.AutoCAD.UI.Core.csproj` - SDK-style bridge project file targeting net8.0-windows

## Decisions Made
- **AssemblyName = Gallio.AutoCAD.UI**: Matches original project output name so consuming code and plugin references remain stable
- **Revision.cs path = ..\Revision.cs**: Points to src/Extensions/AutoCAD/Revision.cs (AutoCAD-local), not the root src/Revision.cs — consistent with Gallio.AutoCAD.Core pattern
- **No NuGet packages**: Source (StartupPreferencePane + StartupPreferencePaneProvider) only uses System.Windows.Forms APIs provided by UseWindowsForms=true; ConfigurationManager is not needed
- **No source exclusions beyond obj/bin/AssemblyInfo.cs**: Source is clean — no incompatible files in ControlPanel/

## Deviations from Plan

None — plan executed exactly as written.

## Issues Encountered

None — first build attempt succeeded with 0 errors.

## User Setup Required

None — no external service configuration required.

## Next Phase Readiness
- Gallio.AutoCAD.UI.Core.csproj is ready for use in any solution that needs the AutoCAD UI extension on net8.0-windows
- All 9 Phase 1 requirements (PROJ-01 through DEP-03) are satisfied by this single .csproj file
- Warning count (2524) is entirely CA1416 platform-compat warnings — addressable with [SupportedOSPlatform] attributes in a future phase

---
*Phase: 01-project-setup*
*Completed: 2026-03-05*
