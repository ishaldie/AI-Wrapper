---
name: conductor-complete
description: Mark the active track complete and archive it after verification and checkpoints are finished.. Use when the user asks about conductor complete or mentions $conductor-complete.
---

# Conductor Complete

Mark the current track as complete and archive it.

**FIRST: Read all context files before doing anything else.**

Read these files NOW:
- conductor/tracks.md
- Active track's plan.md and spec.md

---

## After Reading Context

### Verify Completion

1. Check all tasks in plan.md are marked `[x]`
2. If any tasks are pending `[ ]` or in-progress `[~]`, warn user

### Archive Process

1. Update tracks.md:
   - Change status from 🔵 (in-progress) to ✅ (completed)
   - Add completion date

2. Move track directory:
   - From: conductor/tracks/<track-id>/
   - To: conductor/archive/<track-id>/

3. Create completion commit:
   - Message: "complete(<track-id>): <description>"
   - Include all track file changes

---

## Critical Rules

1. Always read conductor/ context files FIRST
2. Follow workflow.md EXACTLY as written
3. Get user approval before archiving
4. All tasks must be complete before marking track complete
