# Repository Guidelines

## Project Structure & Module Organization
- Unity project root with key folders:
  - `Assets/` – game content. Scenes in `Assets/Scenes/`, settings in `Assets/Settings/`, third‑party in `Assets/TextMesh Pro/`.
  - `Packages/` – Unity package dependencies (`manifest.json`).
  - `ProjectSettings/` – project/editor settings.
  - Do not commit `Library/`, `Logs/`, or `Temp/` (local caches).
- Place gameplay code under `Assets/Scripts/` using namespaces (e.g., `CrashLab.*`).

## Build, Test, and Development Commands
- Open in Editor: use Unity Hub to add this folder, or launch the Editor and open `.`.
- Build (Editor): File → Build Settings → choose target platform → Build.
- CLI build (example):
  - macOS: `/Applications/Unity/Hub/Editor/<version>/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath . -buildTarget StandaloneOSX -executeMethod BuildScripts.BuildRelease`
  - Replace `BuildScripts.BuildRelease` with your static build method if/when added.
- Play mode: press Play in the Editor; prefer testing from `Assets/Scenes/SampleScene.unity` or your scene.

## Coding Style & Naming Conventions
- Language: C# for gameplay code.
- Indentation: 4 spaces; UTF‑8; LF line endings.
- Braces: K&R style (`void Foo() { ... }`).
- Naming: `PascalCase` for public types/methods; `camelCase` for locals/params; `_camelCase` for private fields; `UPPER_SNAKE_CASE` for constants.
- One public type per file; file name matches type name.
- Use `namespace CrashLab` (and sub‑namespaces by feature).

## Testing Guidelines
- Framework: Unity Test Framework (NUnit).
- Structure:
  - Edit Mode tests → `Assets/Tests/EditMode/`
  - Play Mode tests → `Assets/Tests/PlayMode/`
- Run: Window → Test Runner → Edit/Play Mode. CLI example: `-runTests -testPlatform editmode -projectPath .`.
- Aim for meaningful coverage on gameplay and utilities; fast tests in Edit Mode, behavior in Play Mode.

## Commit & Pull Request Guidelines
- Commits: use Conventional Commits (`feat:`, `fix:`, `chore:`, etc.). Keep messages imperative and scoped (e.g., `feat(player): add dash mechanic`).
- PRs: include a clear description, linked issues, testing notes, and relevant screenshots or short clips (scene view or in‑game). Keep PRs focused and small when possible.

## Security & Configuration Tips
- Keep secrets out of the repo; prefer environment variables or Unity services.
- Maintain a proper `.gitignore` for Unity (exclude `Library/`, `Logs/`, `Temp/`, and build artifacts).

