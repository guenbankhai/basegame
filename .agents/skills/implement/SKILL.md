---
name: implement
description: "Implement a piece of agreed work, described in the conversation or in a file you point to."
disable-model-invocation: true
---

Implement the work the user described: in this conversation, or in the spec or notes file they point to. If its scope is materially ambiguous, ask first; otherwise start.

Read the relevant code and the project rules before editing, and build on the decisions already agreed.

Call the Skill tool with "tdd" where it fits, at agreed seams.

Run typechecking regularly, single test files regularly, and the full test suite once at the end, using whichever of these the project has.

If the work hits a conflict with an agreed decision, the project rules, or the architecture, stop that part, report it, and wait; carry on with the unaffected parts.

Once done, report what changed, the verification commands you ran and their results, and anything left unverified. Offer a review with `/review-my-code`; run it only if the user asks. Leave committing to the user.
