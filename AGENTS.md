# Agent Rules

Rules for agents working on BaseGame: general rules first, then project rules. Skills define task-specific workflows; where a skill's default conflicts with a project rule, the project rule wins.

## Principles

- Follow the user's intent and agreed scope. Discussion, research, diagnosis, or review does not authorize implementation.
- Respect agreed decisions, project architecture, and conventions. Report conflicts rather than silently overriding them.
- Investigate relevant context independently. Ask only when ambiguity materially affects the outcome or a decision requires user input.
- Prefer simple, focused solutions. Avoid unnecessary complexity and unrelated changes.

## Execution Boundaries

Work autonomously within the authorized task, including actions required by explicitly requested skills. Unless authorized by the user or project rules:

- Git is read-only. Do not change branches, history, index, or working-tree state through Git operations, or create commits.
- Do not publish or modify remote resources.
- Do not install dependencies.
- Do not modify agent instructions or unrelated documentation.
- Do not expand scope or override agreed decisions.

If a conflict affects only part of the task, report it and continue unaffected work.

## Verification & Reporting

- Verify results proportionately. Never claim success without evidence; disclose what was not verified.
- Report changes, verification, and unresolved issues concisely.

## Project: BaseGame

### Context

- Personal Unity project for learning, experimenting with, and building gameplay mechanics. The user is the only developer and makes all structural and architectural decisions; agents propose.
- Unity project: `unity/`, Editor `6000.6.5f1` (URP, C#, PC). `README.md` is an informal overview, not a source of rules.

### Communication

- Reply in Vietnamese.
- Write all docs in Vietnamese, concise but complete. Exceptions: this file and skills stay in English; glossary terms (`GLOSSARY.md`) and code identifiers stay in English inside Vietnamese text; `package.json` metadata (`displayName`, `description`) stays English, since it is package metadata, not a doc.
- Write code in English. Comments are English and short: explain the mechanism or the reason, not what the code already says.
- Where a skill asks for Simplified Technical English, use plain Vietnamese.

### Structure

- Code lives in `unity/Assets/Playground/<Experiment>/`, `unity/Assets/Integration/<Scenario>/`, or `unity/Packages/com.guenbankhai.<module>/`.
- Experiments and scenarios depend on modules only; modules depend on other modules only, without cycles.
- Do not create experiments, scenarios, or packages unless asked.

### Unity

- Write text files only (`.cs`, `.asmdef`, `package.json`). For scenes, prefabs, assets, and settings, give the user step-by-step Editor instructions; minimise manual setup in code.
- Keep file names equal to type names; move or rename a script with its `.meta`.

### Reference Docs

Read only when the task needs it:

- `docs/structure.md`: creating or changing an experiment, scenario, package, or asmdef; naming and dependencies.
- `docs/coding-standard.md`: before writing or reviewing C#.
- `docs/module-workflow.md`: starting, advancing, finishing, or reviewing a module stage; deciding where module docs and decisions go.

### Planning & Verification

- Before adding files, types, or public API, propose where they go and what they expose, unless already agreed.
- Code handed over must compile and follow these rules. Check compilation with `dotnet build` on the Unity-generated `.csproj`; files Unity has not yet added to it are unchecked, so say so.
- Verify easy logic yourself; leave Unity runtime behaviour to the user. Do not run the Unity Editor or batchmode.
- Report what the user still needs to check, with concrete steps.
- Review module stages by `docs/module-workflow.md` (checklist, severity, pass criteria). A review only reports; apply nothing until the user confirms.
