---
name: handoff
description: Compact the current conversation into a handoff for another agent or session to pick up.
argument-hint: "What will the next session be used for?"
disable-model-invocation: true
---

Write a handoff summarising the current conversation so a fresh agent can continue the work. Output it in the conversation as one copyable Markdown block. Write it to a file only when the user asks: then save it to the temporary directory of the user's OS (`$TMPDIR`, else `/tmp`; `%TEMP%` on Windows) unless they name a location, not the current workspace.

Keep only what the next agent needs, under these headings (omit any that would be empty):

- **Objective**: what the work is for, in one or two sentences.
- **Agreed decisions**: each with its reason when the reason isn't obvious. These are settled; the next agent builds on them.
- **Completed work**: what is done, and how it was verified.
- **Remaining issues**: open questions, known problems, unverified parts.
- **Relevant files**: paths, and commits by SHA.
- **Next requested action**: what the user wants done next, in their terms.
- **Suggested skills**: which skills from this collection the next agent should call the Skill tool for.

Summarise; don't replay the conversation. Do not duplicate content already captured in other artifacts (specs, ADRs, commits, diffs). Reference them by path or SHA instead.

Redact any sensitive information, such as API keys, passwords, or personally identifiable information.

If the user passed arguments, treat them as a description of what the next session will focus on and tailor the handoff accordingly.
