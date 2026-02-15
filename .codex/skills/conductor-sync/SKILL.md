---
name: conductor-sync
description: Sync Conductor commands, skills, and related configuration across supported AI CLIs; use after extension or config changes.. Use when the user asks about conductor sync or mentions $conductor-sync.
---

# Conductor Sync

Sync Conductor configuration to all registered AI engine CLIs.

This command syncs:
- Context files (CLAUDE.md, GEMINI.md, CODEX.md)
- Commands to all platforms
- Skills to platforms that support them
- Agents to platforms that support them (Claude only)

---

## Sync Process

1. **Generate Context File**
   - Read conductor/product.md, workflow.md, tech-stack.md
   - Include active track information
   - Include code styleguides

2. **Sync to Engines**
   - Claude: CLAUDE.md, .claude/commands/, .claude/skills/
   - Gemini: GEMINI.md, .gemini/commands/, .gemini/skills/
   - Codex: CODEX.md, .codex/commands/, .codex/skills/

3. **Report Status**
   - Show which engines were synced
   - Report any errors or skipped engines

---

## When to Sync

Sync automatically happens when:
- Project is registered with Conductor
- Context files are modified
- Extensions are added/removed

You can also trigger a manual sync with this command.
