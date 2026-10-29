---
name: review-my-module
description: Review a BaseGame module at the playground or release stage against the gates and rules in docs/review.md, using the three-axis review-my-code. Tags each finding Chặn / Nên sửa / Ghi chú, lists Thu gọn, ends with a Pass verdict, and changes nothing.
disable-model-invocation: true
---

# Review My Module

Stage-2 (playground) or stage-3 (release) review of a module. Arguments: `<module> <playground|release>`.

Call the Skill tool for "review-my-code", then follow it with the overrides below. Where they conflict, this skill wins; where either conflicts with `docs/review.md`, `docs/review.md` wins.

## Overrides

- **Rules come from `docs/review.md`.** Read "Luật chung" and the stage's gate section ("Cổng playground" or "Cổng phát hành"). Classify, report and pass by it.
- **Independent run.** Review the named paths as they stand, not a diff since an earlier round.
- **Severity inside each axis.** Keep `review-my-code`'s separate axes and do not rerank across them; tag each finding by "Xếp mức" in `docs/review.md`.
- **Gates first.** Before the axes, walk every gate of the stage and record each as `Đạt` / `Chưa đạt` / `Chưa kiểm được` with short evidence.

## Pin the target

- Design doc: `docs/modules/<module>.md`.
- Experiment: `unity/Assets/Playground/<Module>/` by default; if it does not exist, find the experiment that uses the module or ask.
- Package: `unity/Packages/com.guenbankhai.<module>/`.

## Stage `playground`

- Target: the experiment folder.
- Spec: the design questions in the doc's `## Kế hoạch (tạm)`, plus the design doc.
- Axes: **Correctness** only, limited to bugs that would give a wrong answer to a design question. **Scope** becomes: is each design question answered, and what is the answer. Skip **Standards**: playground code is thrown away.
- Extra report sections: the answer to each design question, with evidence; proposed changes to the design doc, each with a gameplay example.

## Stage `release`

- Target: the package, the experiment, and the module's docs (design doc, ADRs, package `README.md` and `CHANGELOG.md`).
- Spec: the design doc, plus the API block, rule table and Settings table in the package `README.md` (their only home once a package exists).
- Standards sources: `AGENTS.md`, `docs/coding-standard.md`, `docs/structure.md`.
- All three axes, plus the gates R1–R8.
- Compilation: run `dotnet build` on the Unity-generated `.csproj` files (read-only check); say which files the `.csproj` does not yet include. EditMode test results come from the user running them in Unity; ask for them if not given, and report R1 as `Chưa kiểm được` until then.

## Report

In the order of "Báo cáo" in `docs/review.md`, with the gate table first, the findings grouped under `## Correctness`, `## Scope`, `## Standards` (as applicable), then Thu gọn and the stage's extra sections.

Then stop and wait for the user to confirm what to apply.
