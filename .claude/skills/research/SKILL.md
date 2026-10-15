---
name: research
description: Investigate a question against high-trust primary sources and answer with cited findings. Use when the user wants a topic researched, options compared, docs or API facts gathered, specific references read, or reading legwork delegated to a background agent.
---

# Research

Pick the mode from the request:

- **Focused**: the user named sources or asked a precise question. Work from those sources first and answer exactly that question. If they don't settle it, say what's missing and ask before widening the search.
- **Open-ended**: the user wants the options. Find the credible alternatives and compare them on the criteria that matter in the user's context.

When the reading is broad (many sources, long docs), spin up a **background agent** to do it, so you and the user keep working while it reads; it reports back the findings and their sources. A focused question answerable from a page or two, read yourself.

In both modes:

1. Investigate against **primary sources** (official docs, source code, specs, first-party APIs), not a secondary write-up of them. Follow every claim back to the source that owns it.
2. Stop when the question is answered with evidence. Depth follows the question, not the topic.
3. Answer in the conversation: the direct answer first, then the findings, each citing its source (URL or path, plus version or date where it matters). Keep facts, assumptions, and your recommendation visibly separate.

Write the findings to a file only when the user asks. Then save it where the repo already keeps such notes; match the existing convention, and if there is none, ask where.
