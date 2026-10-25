---
name: review-my-module
description: Review a BaseGame module at the playground or release stage against docs/module-workflow.md, using the three-axis review-my-code. Tags each finding Chặn / Nên sửa / Ghi chú, ends with a Pass verdict, and changes nothing.
disable-model-invocation: true
---

# Review My Module

Stage-2 (playground) or stage-4 (release) review of a module. Arguments: `<module> <playground|release> [since <ref>]`.

Call the Skill tool for "review-my-code", then follow it with the overrides below. Where they conflict, this skill wins.

## Overrides

- **Report only.** Do not edit code, docs, `## Kế hoạch (tạm)`, ADRs, `GLOSSARY.md`, or the checklist. Do not offer to fix in the same turn; list proposals and wait for the user to confirm which to apply.
- **Severity inside each axis.** Keep `review-my-code`'s separate axes and do not rerank across them, but tag every finding Chặn / Nên sửa / Ghi chú using the stage's definition of Chặn in `docs/module-workflow.md` (section "Review").
- **Pass criteria come from the project.** Pass means no Chặn on any axis.

## Pin the target

- Read `docs/module-workflow.md` (sections "Review" and the checklist for the stage) and the design doc `docs/modules/<module>.md`.
- Experiment: `unity/Assets/Playground/<Module>/` by default; if it does not exist, find the experiment that uses the module or ask.
- Package: `unity/Packages/com.guenbankhai.<module>/`.
- With `since <ref>`: a re-review. Use it as `review-my-code`'s fixed point and review only that diff and what it affects.
- Without `since`: review the named paths as they stand.

## Stage `playground`

- Target: the experiment folder.
- Spec: the design questions in the doc's `## Kế hoạch (tạm)`, plus the design doc.
- Axes: **Correctness** only, limited to bugs that would give a wrong answer to a design question. **Scope** becomes: is each design question answered, and what is the answer. Skip **Standards**: playground code is thrown away.
- Extra report sections:
  - **Trả lời câu hỏi thiết kế**: each question with its answer and evidence.
  - **Thay đổi đề xuất cho tài liệu và package**: changes to the design doc or the future package, with a gameplay example each; proposed ADRs.
  - **Bài học cho checklist**: proposed new checklist lines.

## Stage `release`

- Target: the package, the experiment, and the module's docs (design doc, ADRs, package `README.md`).
- Spec: the design doc.
- Standards sources: `AGENTS.md`, `docs/coding-standard.md`, `docs/structure.md`, and "Checklist phát hành".
- All three axes. Besides the axes, walk every line of "Checklist phát hành" and report it as met / not met / not verified.
- Compilation: run `dotnet build` on the Unity-generated `.csproj` files (read-only check); say which files the `.csproj` does not yet include. EditMode test results come from the user running them in Unity; ask for them if not given, and report the line as not verified until then.

## Report (in Vietnamese)

1. **Kết luận**: `Pass` or `Chưa pass (n Chặn)`.
2. `## Correctness`, `## Scope`, `## Standards` (as applicable), each finding tagged with its severity, with location, evidence, failure scenario, and proposed fix.
3. The stage's extra sections (playground) or the checklist walk (release).
4. **Đề xuất checklist**: one proposed new line for each Chặn the checklist did not already cover.

Then stop and wait for the user to confirm what to apply.
