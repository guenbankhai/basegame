---
name: review-my-design
description: Review a BaseGame module design doc against the design gates and rules in docs/review.md. Tags each finding Chặn / Nên sửa / Ghi chú, lists Thu gọn, ends with a Pass verdict, and changes nothing.
disable-model-invocation: true
---

# Review My Design

Stage-1 review of a module design. Argument: `<module>`.

Every rule for classifying, reporting and passing comes from `docs/review.md`. This skill only says what to read and what to check. Each run is independent: review the doc as it stands, with no knowledge of earlier rounds.

## 1. Read

- `docs/review.md`: sections "Luật chung" and "Cổng thiết kế" only.
- The design doc `docs/modules/<module>.md`, the ADRs it links, and the module's section in `GLOSSARY.md`.
- `docs/coding-standard.md`, section "API của module".
- Other docs only where a finding depends on them.

## 2. Check

- Walk every design gate (T1–T9) against the doc, and record each as `Đạt` / `Chưa đạt` / `Chưa kiểm được` with short evidence. Do not stop early.
- Run every example already in the doc by hand (gate T7): write the state, events and clamps after each step.
- Check the doc's rules against the core invariants B1–B4. Use only scenarios that pass "Dùng đúng cách" in `docs/review.md`; scenarios that need a module not yet designed are Nên sửa at most.
- Look for Thu gọn signs in the doc.
- Use "Gợi ý soi" in `docs/review.md` as places to look; it is not a gate.

## 3. Report

Classify and report exactly as `docs/review.md` says (sections "Xếp mức" and "Báo cáo"). Propose glossary entries and ADRs in the report instead of writing them.

Then stop and wait for the user to confirm what to apply.
