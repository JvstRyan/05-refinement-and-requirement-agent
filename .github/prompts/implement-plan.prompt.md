# Implement Requirements

Implement a feature using the requirements document as your **primary context**.

> **Context engineering note**: The requirements document was purpose-built by a planning
> agent to contain exactly the high-signal context you need: curated code references,
> documentation URLs, architectural patterns, and an ordered task list. Treat it as your
> source of truth. Do **not** re-research what it already documents — that wastes your
> attention budget on information already distilled for you.

## Context Loading Strategy

Context is a finite resource. Load it deliberately:

1. **Load the requirements document first** — this is your pre-curated context. It replaces broad codebase exploration.
2. **Load referenced files just-in-time** — only read a file from the codebase when the plan explicitly references it and you are about to work on that step.
3. **Do not load files speculatively** — avoid reading files "just in case". Trust the plan's references.
4. **Supplement only for gaps** — if the plan references something that is missing or unclear, perform a targeted search to fill that specific gap, then continue.

## Execution Process

### 1. Load and internalize the requirements document

- Read the requirements document in full before writing any code
- Identify the ordered task list, referenced files, validation gates, and success criteria
- Create a TODO list mirroring the plan's task order — this is your structured note for tracking progress across the full implementation

### 2. Set up your working context

- Open and read each file the plan references for the **first task only**
- Note the patterns, naming conventions, and integration points relevant to that task
- Load additional referenced files just-in-time as you advance through tasks

### 3. Implement task by task

- Work through the TODO list in the order specified by the plan
- Mark each task in-progress before starting, completed immediately when done
- Mirror the code patterns from the referenced files exactly — do not invent new conventions
- Apply error handling as specified in the plan

### 4. Validate at each gate

- After completing a logical group of tasks, run the validation command(s) specified in the plan
- Fix failures immediately before continuing to the next task
- Re-run until each gate passes — do not accumulate failures

### 5. Self-validate before final sign-off

Before running the final validation suite, review your own implementation as a fresh code
reviewer — not as the person who wrote it. The goal is to catch gaps before they become
failures, not to confirm you did the work.

Work through each check actively:

**Requirements coverage** — re-read the requirements document from the top:
- For each task in the ordered list, confirm the implementation satisfies its *intent*, not just that you touched the relevant file
- For each item in the completion checklist, verify it is fully done — not partially addressed or deferred
- Note any requirement you cannot confidently confirm is met, and address it before proceeding

**Pattern fidelity** — compare your code against the referenced files:
- Did you follow the patterns from the referenced files, or introduce new conventions anywhere?
- Are naming conventions, error handling shapes, and layer boundaries consistent with what was specified?

**Error handling** — review the error scenarios specified in the plan:
- Is each one handled in the implementation?
- Do the error paths behave as specified, or were they left as stubs?

Fix every gap found before proceeding. Add unresolved items back to your TODO list rather
than ignoring them.

### 6. Final verification

- Run the full validation suite one final time
- Re-read the requirements document to confirm nothing was skipped
- Report a brief completion summary: what was implemented, which tests pass

## Guiding Principles

<instructions>
- **Trust the plan**: The planning agent already researched patterns, gotchas, and integration points. Use what it documented.
- **Protect your attention budget**: Every speculative file read or redundant search dilutes focus. Load only what you need, when you need it.
- **Keep notes in your TODO list**: For long implementations, your TODO list is your external memory. Keep it current so you can re-orient after any context reset.
- **Right altitude for decisions**: Follow the plan's explicit guidance precisely. For anything the plan leaves open, apply the patterns you observed in the referenced files — do not introduce new conventions.
- **Self-validation is not a formality**: The review in step 5 should surface real gaps. If it never finds anything, you are not reviewing critically enough.
- **Validate early and often**: Run the plan's validation gates incrementally rather than all at the end. Catching failures early keeps the context clean.
</instructions>

## Success Criteria

Implementation is complete when:
- All tasks from the plan's ordered task list are done
- The self-validation review in step 5 found and resolved all gaps
- All validation gates specified in the plan pass
- Code follows the patterns from the referenced files
- No requirements from the plan's checklist are skipped
