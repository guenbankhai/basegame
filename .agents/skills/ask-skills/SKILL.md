---
name: ask-skills
description: Ask which skill or flow fits your situation. A router over the skills in this collection.
disable-model-invocation: true
---

# Ask Skills

You don't remember every skill, so ask.

Before stating what a skill does or recommending a step be skipped, read that skill's SKILL.md: the summaries here are for orientation only. Recommend only skills listed here. Answer with the recommendation and stop; run a skill only when the user asked you to.

There is no mandatory flow. Every step below is optional: take the one the situation needs. A **flow** is a common path through the skills; most work travels the main one, and the rest are on-ramps or standalone tools.

## The main flow: idea → working code

1. **Sharpen the idea, when it is fuzzy.** **`/grill-with-docs`** interviews you about it, using the project's `GLOSSARY.md` and ADRs as context and proposing updates to them. With no project under it, use **`/grill-me`**: the same interview, stateless. Both run the **`/grilling`** primitive. Skip this step when the requirements are already clear.
2. **Build it** with **`/implement`**, in the same context window as the thinking that produced it. It drives **`/tdd`** (one red-green slice at a time) where tests fit. Reach for **`/tdd`** on its own when you want to build one concrete behaviour test-first.
3. **Review it** with **`/review-my-code`** when you want a review: correctness, scope against what was asked, and the project's standards, reported per axis. It reports; it fixes only what you then pick.

## On-ramps

- **Something's broken** → **`/diagnosing-bugs`**. For the hard ones: the bug that resists a first glance, the intermittent flake, the regression between two known-good states. It refuses to theorise until it has a **tight feedback loop** (one command that already goes red on *this* bug), then fixes with a regression test when you asked for a fix. Where the finding is that there's no good seam to lock the bug down, take that to **`/codebase-design`**.
- **A question talking won't settle** → **`/prototype`**. Throwaway code that answers one design question: a single HTML file that lets anyone click through a state model, or several radically different UI variants behind a switcher. Use it when a grilling session keeps circling a "how should this behave / look" question; the one-line answer goes back into the thinking. Never a required step: if the design is settled, go straight to `/implement`.
- **You need knowledge first** → **`/research`**. Open-ended discovery or a focused read of sources you name, against primary sources, answered in the conversation with citations. Research feeds the thinking; take what it finds into step 1 or straight into a decision.

## Codebase design

**`/codebase-design`** is the deep-module vocabulary (module, interface, depth, seam, adapter, leverage, locality) plus two ways to use it: a **review** of existing code that surfaces deepening candidates, and **design it twice** to compare radically different interfaces for one module. It recommends; restructuring happens only when you ask. `/tdd` speaks the same vocabulary.

## Vocabulary underneath

- **`/domain-modeling`**: sharpen the project's *domain* language: challenge a fuzzy term, resolve an overloaded word, record a hard-to-reverse decision as an ADR. It proposes glossary entries and ADRs and writes them when you agree. `/grill-with-docs` drives it.
- **`/codebase-design`** (above) is the vocabulary for a module's *shape*.

## Phase boundaries

A **phase** is a chunk of work inside a session: the grilling, the implementation, the review. At the **boundary** between two of them you have five options:

- **Continue**: stay put. Costs nothing, loses nothing.
- **Fresh session** (`/clear` in Claude Code, `/new` in Codex): when nothing here matters to what's next.
- **`/handoff`** writes a portable summary. Narrow: only for a **new harness**, a **new directory**, a **colleague**, or forking a side task **mid-phase**.
- **Subagent**: send a tightly-scoped task to its own window and get a report back.
- **`/compact`** compresses this context. The **default**, at the bottom of the tree rather than the first reach.

Read [PHASE-BOUNDARIES.md](PHASE-BOUNDARIES.md) for the ordered tree and the reasoning behind each branch. Make the decision **at** a boundary; mid-phase, continue.

## Standalone

- **`/grill-me`**: the interview with no project under it (a plan, a design, a piece of writing). In a project, `/grill-with-docs` runs the same interview with the project's docs as context.
- **`/grilling`** is the interview primitive itself: rounds, the frontier, facts are the agent's job and decisions are yours. Reach for it directly when you want the interview with no wrapper.
- **`/wait-what`** is the corrective for a message that didn't land. Use it mid-conversation, inside any other skill, and the agent re-pitches what it just said with the context you were missing, in plain English, using the `GLOSSARY.md` vocabulary. It explains; the work waits until you say go.
- **`/handoff`**: compact the conversation into a short handoff (objective, decisions, done, open issues, files, next action) for another session or agent.
