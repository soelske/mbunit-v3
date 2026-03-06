# Roadmap: Gallio.AutoCAD.UI.Core — .NET 8 Migration

## Overview

Create Gallio.AutoCAD.UI.Core as an SDK-style .csproj that bridges the original Gallio.AutoCAD.UI (.NET Framework) to net8.0-windows, using the established Core bridge pattern. Phase 1 stands up the project file with correct settings and source inclusion. Phase 2 achieves 0 build errors by resolving any incompatibilities.

## Phases

- [x] **Phase 1: Project Setup** - Create and configure the SDK-style project with correct settings, wildcards, and references (completed 2026-03-05)
- [ ] **Phase 2: Build Clean** - Resolve all compile errors and reach 0 errors on net8.0-windows

## Phase Details

### Phase 1: Project Setup
**Goal**: A correctly configured Gallio.AutoCAD.UI.Core.csproj exists with all source included and references in place
**Depends on**: Nothing (first phase)
**Requirements**: PROJ-01, PROJ-02, PROJ-03, SRC-01, SRC-02, SRC-03, DEP-01, DEP-02, DEP-03
**Success Criteria** (what must be TRUE):
  1. Gallio.AutoCAD.UI.Core.csproj exists targeting net8.0-windows with UseWindowsForms=true
  2. All .cs files from Gallio.AutoCAD.UI are included via wildcard Compile Include with Link attributes
  3. AssemblyInfo.cs is excluded; Revision.cs is linked from the AutoCAD-local path (..\Revision.cs)
  4. ProjectReferences to Gallio.UI.Core.csproj and Gallio.AutoCAD.Core.csproj are present
  5. Original Gallio.AutoCAD.UI.csproj (old-format) is unchanged
**Plans**: 1 plan
Plans:
- [ ] 01-01-PLAN.md — Create Gallio.AutoCAD.UI.Core.csproj and verify 0-error build

### Phase 2: Build Clean
**Goal**: Gallio.AutoCAD.UI.Core compiles with 0 errors on net8.0-windows
**Depends on**: Phase 1
**Requirements**: BUILD-01, BUILD-02, BUILD-03
**Success Criteria** (what must be TRUE):
  1. `dotnet build Gallio.AutoCAD.UI.Core.csproj` exits with 0 errors
  2. Any incompatible source files are excluded or replaced with .NET 8 equivalents in the Core project directory
  3. The original Gallio.AutoCAD.UI.csproj still builds as before (no source files were modified)
**Plans**: TBD

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Project Setup | 1/1 | Complete    | 2026-03-05 |
| 2. Build Clean | 0/? | Not started | - |
