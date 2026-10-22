# Project Structure

## Layout

```text
basegame/
├── AGENTS.md, README.md
├── docs/                          project docs; ADRs in docs/adr/ (created when needed)
├── unity/                         main Unity project
│   ├── Assets/
│   │   ├── Scenes/, Settings/, …  project-wide Unity defaults
│   │   ├── Playground/<Experiment>/
│   │   └── Integration/<Scenario>/
│   └── Packages/
│       └── com.guenbankhai.basegame.<module>/
│           ├── package.json
│           ├── Runtime/
│           ├── Editor/
│           └── Tests/
└── validation/                    empty Unity project for checking a module installs alone (created later)
```

Folders and packages are created only when needed.

| Area | Role |
|---|---|
| Playground | Experiments: try, compare, rewrite, or delete freely |
| Integration | Scenarios that combine modules; real modules only |
| Packages | Modules: reusable, held to the project's quality bar |
| validation/ | Proves a module installs and runs in a clean project |

## Dependencies

| From \ To | Packages | Integration | Playground |
|---|---|---|---|
| Playground | ✅ | ❌ | ❌ (experiments never use each other) |
| Integration | ✅ | — | ❌ |
| Packages | ✅ declared, no cycles | ❌ | ❌ |

- A module dependency is declared in both `package.json` and the asmdef.
- Unity does not block every violation (a package can still reference an asmdef in `Assets/`), so these rules are kept by review.

## Naming

| Item | Convention | Example |
|---|---|---|
| Package | `com.guenbankhai.basegame.<module>` (lowercase) | `com.guenbankhai.basegame.inventory` |
| Module namespace / asmdef | `BaseGame.<Module>`, `.Editor`, `.Tests` | `BaseGame.Inventory.Tests` |
| Experiment namespace / asmdef | `BaseGame.Playground.<Experiment>`, `.Tests` | `BaseGame.Playground.InventoryGrid` |
| Scenario namespace / asmdef | `BaseGame.Integration.<Scenario>` | `BaseGame.Integration.CraftingLoop` |
| Unity version in `package.json` | `"unity": "6000.6"`, `"unityRelease": "5f1"` | |
| Company / Product (this project) | `GuenBanKhai` / `BaseGame` | |

## Setup notes

- **Experiment**: one asmdef at the experiment root. It references only module asmdefs and the Unity package assemblies it uses (e.g. `Unity.InputSystem`). Code without an asmdef lands in `Assembly-CSharp`, which asmdefs cannot reference.
- **Experiment tests** (optional): `Tests/` asmdef `BaseGame.Playground.<Experiment>.Tests`, Editor platform only, referencing the experiment asmdef and the test framework. No other extra asmdefs.
- **Module**: `Runtime/` asmdef `BaseGame.<Module>`; `Editor/` asmdef `BaseGame.<Module>.Editor`, Editor platform only; `Tests/` asmdef `BaseGame.<Module>.Tests`, referencing the test framework.
- **Module tests**: tests in embedded packages (under `unity/Packages/`) run without extra setup. A package installed any other way (local path, Git, registry), e.g. in `validation/`, must be listed under `"testables"` in that project's `manifest.json`.
- Modules never hard-code save paths; use `Application.persistentDataPath`.
