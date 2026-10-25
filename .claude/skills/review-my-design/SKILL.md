---
name: review-my-design
description: Review a BaseGame module design doc against the design checklist and pass criteria in docs/module-workflow.md. Tags each finding Chặn / Nên sửa / Ghi chú, ends with a Pass verdict, and changes nothing.
disable-model-invocation: true
---

# Review My Design

Stage-1 review of a module design. Arguments: `<module> [since <ref>]`.

Call the Skill tool twice, for "grilling" and "domain-modeling", then follow them with the overrides below. Where they conflict, this skill wins.

## Overrides

- **Report only.** Do not edit any file: not the design doc, its `## Kế hoạch (tạm)`, ADRs, `GLOSSARY.md`, or the checklist. `domain-modeling` normally writes glossary terms and ADRs inline; here, propose them in the report instead. Apply anything only after the user confirms it, as a separate step.
- **Pass criteria come from the project.** The session ends when no Chặn is open, not when every branch of the design tree is exhausted.

## Process

### 1. Pin the target

- Design doc: `docs/modules/<module>.md`. Also read the ADRs it links, the module's section in `GLOSSARY.md`, `docs/coding-standard.md` (section "API của module"), and `docs/module-workflow.md` (sections "Review" and "Checklist thiết kế").
- With `since <ref>`: this is a re-review. Review only `git diff <ref> -- docs/ GLOSSARY.md` and what those changes affect. Findings in untouched text count only if the change made them wrong.
- Without `since`: full review.

### 2. Check

- Walk every line of "Checklist thiết kế" against the doc. Note lines not met.
- Run every example in the doc by hand, especially in "Quy ước cho module dùng …": write the state, events and clamps after each step.
- Check normal run vs skip/restore give the same result, including with the modules the doc names as consumers.
- Read surrounding docs only where a finding depends on them.

### 3. Classify

Tag each finding with the stage-1 definition of Chặn in `docs/module-workflow.md`. A finding outside the checklist is Chặn only if it meets that definition; otherwise it is Nên sửa or Ghi chú. Every finding needs a concrete scenario (input or state → wrong result) and a proposed fix. Drop findings without one.

### 4. Report (in Vietnamese)

1. **Kết luận**: `Pass` or `Chưa pass (n Chặn)`.
2. **Chặn**: one grilling round in the grilling format (numbered, with a recommended answer, worded so "yes" accepts it). Later rounds only for decisions that hang off these answers.
3. **Nên sửa**: a numbered list, each with its recommended change, for the user to approve as a batch; say for each whether to fix now or note in `## Kế hoạch (tạm)`. Do not start grilling rounds for these.
4. **Ghi chú**: short list.
5. **Đề xuất checklist**: one proposed new line for each Chặn the checklist did not already cover.
6. **Checklist chưa đạt**: lines from "Checklist thiết kế" not yet met that are not already findings above.

Then stop and wait for the user to confirm what to apply.
