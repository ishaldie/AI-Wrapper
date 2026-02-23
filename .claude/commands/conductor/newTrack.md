---
name: Conductor New Track
description: Create a scoped development track with spec.md, plan.md, and cgx.md; use when starting new feature, bugfix, refactor, or docs work.
---

# Conductor New Track

Create a new development track (feature, bugfix, refactor, or docs).

**FIRST: Read all context files before doing anything else.**

Read these files NOW:
- conductor/product.md
- conductor/tech-stack.md
- conductor/workflow.md
- conductor/tracks.md
- conductor/code_styleguides/ (all files)

---

## After Reading Context

### Phase 0: Discovery (MANDATORY — Do NOT skip)

**Before creating any files, you MUST understand what the user wants to build.** Do not generate a spec, plan, or track directory until this phase is complete.

**Step 1: Ask what they're building.**
If the user hasn't already provided a clear description, ask:
> "What are you trying to build? Describe the feature/fix/change in your own words."

**Step 2: Ask clarifying questions.**
Based on their answer, ask 2-5 targeted questions to fill in gaps. Focus on:
- **User behavior:** What should the user see/do? What's the workflow?
- **Scope boundaries:** What's explicitly NOT included?
- **Technical constraints:** Are there existing patterns this must follow? Integration points?
- **Success criteria:** How will we know it's done? What does "working" look like?

**Step 3: Summarize your understanding.**
Write a 3-5 sentence summary of what you'll build and present it to the user. Ask:
> "Does this match what you have in mind? Anything to add or change?"

**Step 4: Get explicit approval to proceed.**
Only after the user confirms your summary, move to track creation below.

**CRITICAL: Do NOT rush this phase.** A 2-minute conversation here prevents hours of rework later. If the user gives a vague one-liner like "add notifications" or "fix the dashboard", you MUST ask follow-up questions — do not guess and start generating files.

---

## Track Creation (after Discovery is complete)

### 1. Generate Track ID
- Convert description to kebab-case
- Example: "Add user authentication" → `add-user-authentication`

### 2. Determine Track Type
- feature: New functionality
- bugfix: Fix existing behavior
- refactor: Improve code without changing behavior
- docs: Documentation updates

### 3. Create spec.md
Generate specification with:
- Overview
- Requirements (numbered list)
- Acceptance criteria
- Out of scope

**Show spec to user and get approval before proceeding.**

### 4. Create plan.md
Generate implementation plan with:
- Phases (logical groupings)
- Tasks within each phase
- Each task marked `[ ]` (pending)

**Show plan to user and get approval before proceeding.**

### 5. Create Track Files
```
conductor/tracks/<track-id>/
├── spec.md
├── plan.md
├── cgx.md      # Conductor Growth Experience tracking
└── metadata.json
```

The `cgx.md` file is automatically created to capture:
- Frustrations and friction points during implementation
- Good patterns to encode and anti-patterns to prevent
- Missing capabilities that would help
- Improvement candidates for new skills/commands/agents

### 6. Update tracks.md
Add entry to conductor/tracks.md index. **You MUST use this exact format** for the Conductor UI to detect tracks:

```markdown
---

## [ ] Track: <Track Description>
*Link: [./conductor/tracks/<track-id>/](./conductor/tracks/<track-id>/)*

---
```

**Format rules:**
- Use `## [ ] Track: Description` for the heading (with checkbox: `[ ]` = new, `[~]` = in progress, `[x]` = completed)
- Use `*Link: [./conductor/tracks/<id>/](./conductor/tracks/<id>/)*` for the track link
- Separate each track entry with `---`
- Do NOT use numbered headings like "Track 1:", emoji status indicators, or `**Track folder:**` format

---

## Critical Rules

1. Always read conductor/ context files FIRST
2. Follow workflow.md EXACTLY as written
3. Get user approval before making changes
4. Spec and plan MUST be approved before writing files
5. tracks.md entries MUST follow the exact format above for UI detection
