# Milestone 1 Verification

## Completed From Shell

- Standalone Core/Application compile with `csc`.
- Standalone Core/Application smoke check with `csi`.
- EditMode test source compile with `csc` and Unity package-cache NUnit.
- Unity generated production script assemblies:
  - `Library/ScriptAssemblies/Blobs.Core.dll`
  - `Library/ScriptAssemblies/Blobs.Application.dll`
  - `Library/ScriptAssemblies/Blobs.Tests.EditMode.dll`
- Regenerated Unity project references confirm:
  - `Blobs.Application.csproj` references `Blobs.Core.csproj`.
  - `Blobs.Tests.EditMode.csproj` references `Blobs.Core.csproj` and `Blobs.Application.csproj`.

## Completed In Editor

- Unity Test Runner EditMode suite passed for `Blobs.Tests.EditMode`.
- Assembly Definition inspector confirmed the intended Milestone 1 boundaries.

Use the Assembly Definition inspector to confirm:

- `Blobs.Core` has no references.
- `Blobs.Application` references `Blobs.Core`.
- `Blobs.Tests.EditMode` is Editor-only and references `Blobs.Core`, `Blobs.Application`, `UnityEngine.TestRunner`, and `UnityEditor.TestRunner`.
