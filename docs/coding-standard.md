# Coding Standard

Applies to all C# in Playground, Integration, and Packages. Formatting and field naming are enforced by `.editorconfig`.

## Naming

- Follow Microsoft C# conventions: `PascalCase` for types, methods, properties, events, and public fields; `camelCase` for locals and parameters.
- Private fields: `_camelCase` (Unity's Inspector still shows `_moveSpeed` as "Move Speed").
- Constants and `static readonly` fields: `PascalCase`.

## Files

- One main type per file; the file name equals the type name.
- Namespace matches the asmdef name (see `docs/structure.md`).

## Unity

- Use `[SerializeField] private` instead of public fields; expose read access through properties.
- No `Find*` or `SendMessage`. Cache components in `Awake` instead of calling `GetComponent` every frame.
- Between code, use C# `event Action<T>`. Use `UnityEvent` only when the connection is made in the Inspector.

## Null

- Nullable reference types stay disabled.
- Do not use `?.` or `??` on `UnityEngine.Object`: Unity overrides `== null`, and these operators bypass it.

## Static State

- Do not keep gameplay state in `static` fields. State belongs to component or ScriptableObject instances.
- `const`, `static readonly` values, and pure static methods are fine.
- Domain reload is disabled in this project, so static fields keep their values between Play sessions. When static state is unavoidable, reset it:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
private static void ResetStatics()
{
    _cache = null;
}
```

## Language

- C# 9 (the `LangVersion` in Unity-generated `.csproj`): no file-scoped namespaces, global usings, or other C# 10+ features.

## Tests and Docs

- Packages: tests for important behaviour and a short usage doc are required.
- Playground: tests and docs are optional.
