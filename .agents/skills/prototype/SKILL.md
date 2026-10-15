---
name: prototype
description: Build a throwaway prototype to answer a design question. Use when the user wants to sanity-check whether a state model or logic feels right, or explore what a UI should look like.
---

# Prototype

A prototype is **throwaway code that answers a question**. The question decides the shape.

## When to build one

Knowing a prototype would help is not permission to build one:

- **The user asked for a prototype**: build it.
- **The user asked you to experiment with or validate an idea**: a prototype is one way to do it. Use it when it is the cheapest way to settle the question.
- **The user asked for an explanation, research, analysis, or discussion**: write no prototype files. If a prototype would settle something that talking won't, suggest it in one line and wait for a yes.

It is never a required step before implementing: if the design is already settled, skip it. If something already built is misbehaving, that is `/diagnosing-bugs`; a prototype explores what to build, not why the built thing is broken.

Keep it to one question, or a few closely related ones. If it can't be answered in one sitting, the question is too big; split it.

## Pick a branch

Identify which question is being answered, using the user's prompt, the surrounding code, or by asking if the user is around:

- **"Does this logic / state model feel right?"** → [LOGIC.md](LOGIC.md). Build a single shareable HTML file (free-play buttons plus tabbed guided walkthroughs) that pushes the state machine through cases that are hard to reason about on paper, and that a non-developer can drive.
- **"What should this look like?"** → [UI.md](UI.md). Generate several radically different UI variations on a single route or a single HTML file, switchable via a URL search param and a floating bottom bar.

The two branches produce very different artifacts, so getting this wrong wastes the whole prototype. If the question is genuinely ambiguous and the user isn't reachable, default to whichever branch better matches the surrounding code (a backend module → logic; a page or component → UI) and state the assumption at the top of the prototype.

## Rules that apply to both

1. **Throwaway from day one, and clearly marked as such.** Locate the prototype code close to where it will actually be used (next to the module or page it's prototyping for) so context is obvious, but name it so a casual reader can see it's a prototype, not production. For throwaway UI routes, obey whatever routing convention the project already uses; don't invent a new top-level structure.
2. **Trivial to run.** A UI prototype inside the project starts from one command in the project's task runner: `pnpm <name>`, `python <path>`, `bun <path>`, etc. A logic demo or a standalone UI file is a single HTML file the user double-clicks. Either way, no thinking required to start it.
3. **No persistence by default.** State lives in memory. Persistence is the thing the prototype is _checking_, not something it should depend on. If the question explicitly involves a database, hit a scratch DB or a local file with a clear "PROTOTYPE, wipe me" name.
4. **Skip the polish.** No tests, no error handling beyond what makes the prototype _runnable_, no abstractions, no new dependencies. The point is to learn something fast.
5. **Surface the state.** After every action (logic) or on every variant switch (UI), print or render the full relevant state so the user can see what changed.
6. **Simplest environment that answers the question.** Plain HTML/CSS/JavaScript is the default. Don't reach for the production stack by habit; use a specific runtime or framework only when the question depends on how it behaves (its timing, rendering, APIs, or the real app's context). When you report, say which it was: a conceptual simulation shows the model or the layout, not how the target environment behaves, so name what is still unverified there.
7. **Report the answer when done.** In the conversation, give the verdict and the question it settled, and where the prototype lives and how to run it. Leave the prototype where it is, clearly marked. Folding the validated decision into the real code, keeping the prototype as a primary source (upstream commits it to a throwaway `prototype/<name>` branch out of main), or deleting it happens only when the user asks.
