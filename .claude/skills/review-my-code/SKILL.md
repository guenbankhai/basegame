---
name: review-my-code
description: "Review a set of changes (a diff since a fixed point, a commit, uncommitted work, or named files) along three axes: Correctness (does it work and what could break?), Scope (does it match what was asked?), and Standards (does it follow this repo's documented coding standards?). Reports findings side by side; fixes nothing unless asked. Use when the user wants to review a branch, a commit, work-in-progress changes, or asks to \"review since X\"."
---

Three-axis review of a set of changes:

- **Correctness**: does the code do what it should, and what could break?
- **Scope**: does the code faithfully implement what was asked, and only that?
- **Standards**: does the code conform to this repo's documented coding standards?

Each axis runs as its own **parallel sub-agent** so they don't pollute each other's context, then this skill aggregates their findings, reported separately so one axis doesn't mask another (see _Why separate axes_). This skill reports; it changes no code.

## Process

### 1. Pin the target

Work out what to review from what the user said, and capture it with read-only git:

- **A fixed point** (commit SHA, branch, tag, `main`, `HEAD~5`): `git diff <fixed-point>...HEAD` (three-dot, so the comparison is against the merge-base), plus the commit list from `git log <fixed-point>..HEAD --oneline`.
- **One commit**: `git show <sha>`.
- **Uncommitted work**: `git diff` and `git diff --staged`.
- **Named files or paths**: review them as they stand.

If it's unclear which of these the user means, ask. Before going further, confirm the ref resolves (`git rev-parse <ref>`) and the diff is non-empty. A bad ref or empty diff should fail here, not inside parallel sub-agents.

### 2. Identify the spec source

Find what the change was supposed to do, in this order:

1. The user's request in this conversation, and any decisions agreed for it.
2. A path the user passed as an argument.
3. A spec or notes file under `docs/`, `specs/`, or `.scratch/` matching the branch name or feature, or one the commit messages point to.
4. If nothing is found, ask the user where the spec is. If they say there isn't one, the **Scope** axis is skipped and reported as "no spec available".

### 3. Identify the standards sources

Search the repo for every file that documents how code should be written. The project's `AGENTS.md` / `CLAUDE.md` and the docs they point to are on the list, and when `CODING_STANDARDS.md` or `CONTRIBUTING.md` exists, it must be on the list.

On top of whatever the repo documents, the Standards axis always carries the **smell baseline** below: a fixed set of Fowler code smells (_Refactoring_, ch.3) that applies even when a repo documents nothing. Two rules bind it:

- **The repo overrides.** A documented repo standard always wins; where it endorses something the baseline would flag, suppress the smell.
- **Always a judgement call.** Each smell is a labelled heuristic ("possible Feature Envy"), never a hard violation. Like any standard here, skip anything tooling already enforces.

Each smell reads *what it is* → *how to fix*; match it against the diff:

- **Mysterious Name**: a function, variable, or type whose name doesn't reveal what it does or holds. → rename it; if no honest name comes, the design's murky.
- **Duplicated Code**: the same logic shape appears in more than one hunk or file in the change. → extract the shared shape, call it from both.
- **Feature Envy**: a method that reaches into another object's data more than its own. → move the method onto the data it envies.
- **Data Clumps**: the same few fields or params keep travelling together (a type wanting to be born). → bundle them into one type, pass that.
- **Primitive Obsession**: a primitive or string standing in for a domain concept that deserves its own type. → give the concept its own small type.
- **Repeated Switches**: the same `switch`/`if`-cascade on the same type recurs across the change. → replace with polymorphism, or one map both sites share.
- **Shotgun Surgery**: one logical change forces scattered edits across many files in the diff. → gather what changes together into one module.
- **Divergent Change**: one file or module is edited for several unrelated reasons. → split so each module changes for one reason.
- **Speculative Generality**: abstraction, parameters, or hooks added for needs the spec doesn't have. → delete it; inline back until a real need shows.
- **Message Chains**: long `a.b().c().d()` navigation the caller shouldn't depend on. → hide the walk behind one method on the first object.
- **Middle Man**: a class or function that mostly just delegates onward. → cut it, call the real target direct.
- **Refused Bequest**: a subclass or implementer that ignores or overrides most of what it inherits. → drop the inheritance, use composition.

### 3b. Identify the project's review rules

If the repo documents how reviews classify and pass findings (for BaseGame: `docs/review.md`, section "Luật chung" plus the section that matches the target), those rules decide severity, which scenarios count, extra report sections (such as a simplification list), and the pass verdict. Paste them into every axis brief in step 4. They override the defaults in this skill wherever the two differ.

### 4. Spawn the axis sub-agents in parallel

Issue the sub-agent calls together, in the foreground, and aggregate the reports they return. Each prompt includes the diff command and commit list, the axis brief below, and that axis's sources: the spec for Scope; the standards files from step 3 **plus the smell baseline pasted in full** for Standards (the sub-agent has no other access to it). Skip the Scope sub-agent when there is no spec.

For a small diff (a few hunks), review the axes yourself instead, one at a time, writing each axis's findings before starting the next: the sub-agents would cost more than they save.

Every brief also carries: "Read surrounding code where a hunk's correctness depends on it. Every finding cites its location and quotes the hunk; a finding without evidence is dropped. Report only; change nothing."

- **Correctness**: "Report bugs and risks the change introduces: wrong logic, unhandled edge cases and error paths, broken callers, data loss, security issues, concurrency hazards, performance regressions, and changed behaviour left untested. For each, give the concrete scenario that fails (inputs or state → wrong result). Mark each as confirmed (you traced or ran it) or suspected. Under 400 words."
- **Scope**: "Report: (a) requirements the spec asked for that are missing or partial; (b) behaviour in the diff that wasn't asked for (scope creep); (c) requirements that look implemented but where the implementation looks wrong. Quote the spec line for each finding. Under 400 words."
- **Standards**: "Report, per file/hunk where relevant, (a) every place the diff violates a documented standard: cite the standard (file + the rule); and (b) any baseline smell you spot: name it and quote the hunk. Distinguish hard violations from judgement calls: documented-standard breaches can be hard, but baseline smells are always judgement calls, and a documented repo standard overrides the baseline. Skip anything tooling enforces. Under 400 words."

### 5. Report

Present the findings under `## Correctness`, `## Scope`, and `## Standards` headings. Do **not** merge or rerank findings across axes, because the axes are deliberately separate. When step 3b found project review rules, tag each finding with their severity, drop what they say to drop, and add the sections and verdict they require.

End with a one-line summary: total findings per axis, and the worst issue _within each axis_ (if any). Don't pick a single winner across axes: that's the reranking the separation exists to prevent.

Then stop. Offer to fix the findings the user picks; fix nothing until they do.

## Why separate axes

A change can pass one axis and fail another:

- Code that follows every standard but implements the wrong thing → **Standards pass, Scope fail.**
- Code that does exactly what was asked but breaks the project's conventions → **Scope pass, Standards fail.**
- Code that is in scope and clean but breaks on an edge case → **Scope and Standards pass, Correctness fail.**

Reporting them separately stops one axis from masking another.
