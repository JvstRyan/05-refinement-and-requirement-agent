# Generate Requirements Document

Generate a requirements document that serves as **pre-curated context** for an implementing agent.

> **Context engineering note**: The implementing agent has a finite attention budget. Your
> job is to produce the *smallest set of high-signal information* that fully enables
> autonomous implementation — not a dump of everything you researched. Every low-value
> token you include dilutes the agent's focus. Be comprehensive on what matters; ruthlessly
> omit what doesn't.

**Start by reading the spec.md file** to understand what needs to be done, the examples
provided, and any other considerations.

## Research Process

### 0. Task Analysis
- **First step**: Read and analyze the spec.md file in the project root
- Extract the feature description, examples, documentation references, and considerations
- Use this as the foundation for all subsequent research

### 1. Codebase Analysis

- Search for similar patterns/features in the workspace
- Identify the **specific files and line ranges** the implementing agent will need — no more
- Note existing conventions and architectural patterns to follow
- Check test patterns and validation approaches

### 2. External Research

- Find relevant library documentation; record **direct URLs to the specific section**, not homepage links
- Identify common pitfalls and gotchas worth flagging
- Research integration patterns only where non-obvious

### 3. Curation — what to include vs. exclude

Include only information that:
- The implementing agent cannot trivially infer from the code itself
- Prevents a likely mistake or misunderstanding
- Specifies a non-obvious integration requirement

Exclude:
- Background research the agent doesn't need to act on
- Alternative approaches you considered but rejected (unless the rejected path is a likely trap)
- General documentation that can be found from the URLs you provide

## Requirements Document Structure

### Right altitude for guidance

Aim for the Goldilocks zone:
- **Too prescriptive**: step-by-step instructions for every line of code — creates a brittle document and gives the agent no room to apply its own capability
- **Too vague**: "follow existing patterns" without saying which files or what pattern — falsely assumes shared context

The right altitude: specific enough to prevent wrong decisions, flexible enough that the agent applies judgment within those guardrails.

### Sections to include

**1. Feature overview** — one paragraph: what it does, why it exists, how it fits the existing system

**2. Key references** — a curated list of files and URLs the agent will need. For each file reference, include:
  - Exact relative path
  - Relevant line range (if applicable)
  - One sentence on what pattern to take from it

**3. Architectural constraints** — decisions that are already made and must not be changed (naming conventions, layer boundaries, error handling patterns, etc.)

**4. Gotchas** — library quirks, version-specific behaviour, or integration traps that aren't obvious from reading the code

**5. Ordered task list** — a numbered list of discrete implementation steps. This becomes the implementing agent's TODO list directly. Each step should:
  - Be a single, completable unit of work
  - Reference the specific file(s) involved
  - Note any validation to run after that step

**6. Validation gates** — specific commands the agent must run and what a passing result looks like:
  - Test commands (e.g., `dotnet test --filter Category=Unit`)
  - Linting or build commands
  - Any integration test requirements

**7. Completion checklist** — a flat list of checkboxes the agent verifies before declaring done

## Self-Validation Before Saving

Before saving the document, review it as if you are the implementing agent reading it for the
first time. Work through each check and fix problems before proceeding.

### Completeness check — every section is present:
- [ ] Feature overview (one paragraph)
- [ ] Key references with exact paths, line ranges, and one-sentence pattern descriptions
- [ ] Architectural constraints
- [ ] Gotchas
- [ ] Ordered task list
- [ ] Validation gates with specific runnable commands
- [ ] Completion checklist

### Altitude check — for each task in the ordered list, ask:
- Could a capable agent implement this step without additional research? If **no** → add the missing detail
- Does this step dictate exact lines of code? If **yes** → relax it to a pattern reference with a file link

### Two-sided quality test:
- "Would the implementing agent need to search the codebase beyond the files I referenced?" → if yes, identify and add the missing references
- "Would the implementing agent need to read files I did not reference?" → if yes, either add those references explicitly or remove the dependency

### Spec coverage check:
- Re-read spec.md and confirm every requirement maps to at least one task in the ordered list
- Flag any gaps and add the missing tasks before saving

Only save the document when all four checks pass.

## Output Requirements

Save the requirements document as: `docs/implementation-plans/{feature-name}.md`

The document should enable the implementing agent to work autonomously **without re-doing
your research**. If the implementing agent would need to perform broad codebase exploration
or external research to fill gaps, the document is incomplete. If the implementing agent
would need to read more than the files you explicitly referenced, the document is over-scoped.
