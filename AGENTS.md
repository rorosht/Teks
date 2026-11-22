# AGENTS.md — TEKS (Unity 6 Project Context)

## Project Overview
- **Engine**: Unity 6 (`6000.6.1f1`), URP (Universal Render Pipeline), Windows PC.
- **Language**: C# (`Assets/Scripts/`, `Assets/Editor/`).
- **Scene**: `Assets/Scenes/SampleScene.unity` (kept clean at ~12 KB; runtime world is built dynamically by `TeksGameBootstrapper`).

## Hard Technical Constraints (MUST FOLLOW)
1. **New Input System ONLY** (`activeInputHandler: 1`):
   - Always use `UnityEngine.InputSystem.Keyboard.current`, `Mouse.current`, and `InputSystemUIInputModule`.
   - NEVER use legacy `UnityEngine.Input` or `StandaloneInputModule`.
2. **UI Text**:
   - TextMeshPro Essentials are NOT imported. Always use `UnityEngine.UI.Text` with `Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")`.
3. **Unity 6 APIs**:
   - Use `Rigidbody.linearVelocity` (not `velocity`), `PhysicsMaterial` (not `PhysicMaterial`), `FindAnyObjectByType<T>()` / `FindObjectsByType<T>()` (without `FindObjectsSortMode`).
4. **URP Materials (Build-Safe)**:
   - Always create materials through `CardArtGenerator.CreateLitMaterial(...)` or `CardArtGenerator.CreateUnlitEmissiveMaterial(...)` which clone bundled templates in `Assets/Resources/TeksMaterials/`. Never rely on bare `Shader.Find()` alone.
5. **2-Card Teks Matches & Same-Area Battles**:
   - Always 2 cards only (player Pamato + NPC Pamato; never a 3rd Panggulo card).
   - Battles occur right where the player challenges the NPC, not teleported to center court.
6. **Clean Compilation Verification**:
   - Build BOTH `Assembly-CSharp.csproj` and `Assembly-CSharp-Editor.csproj` with:
     ```powershell
     foreach ($f in "Assembly-CSharp.csproj","Assembly-CSharp-Editor.csproj") { if (Test-Path $f) { (Get-Content $f) | Where-Object { $_ -notmatch "Microsoft\.Unity\.Analyzers\.dll" } | Set-Content $f } }
     $env:APPDATA="C:\Users\User\.gemini\antigravity\brain\0287d44a-7382-495f-b2ed-fe6427a84b00\scratch"
     dotnet build Assembly-CSharp-Editor.csproj /nodeReuse:false -p:UseSharedCompilation=false
     ```
   - Must succeed with **0 Errors and 0 Warnings**.
7. **Keep `SampleScene.unity` Clean**:
   - Never commit or save procedural `BarangayCourtWorld` objects into `SampleScene.unity`.
