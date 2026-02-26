# Conductor Growth Experience (CGX)

**Track:** `bug_fixes_20260216`
**Purpose:** Log observations during implementation for continuous improvement analysis.

---

## Frustrations & Friction

- [2026-02-16] Hook auto-generates OAuth external auth code in Login.razor and AuthPageTests.cs every time any file is edited. Had to rewrite files 3+ times before learning to work WITH the hook. The hook changes `@onclick` social buttons to `<a href="/api/auth/external-login?provider=...">` links and adds error-handling code. Wasted ~10 minutes fighting it.

---

## Patterns Observed

### Good Patterns (to encode)
<!-- Workflows that worked well and should be automated/standardized -->

### Anti-Patterns (to prevent)
<!-- Mistakes or inefficiencies that should be caught earlier -->

---

## Missing Capabilities

<!-- Tools, commands, or features that would have helped -->
<!-- Format: - Description | Suggested solution | Scope (project/global) -->

---

## Insights & Suggestions

<!-- General observations about improving the development experience -->

---

## Improvement Candidates

<!-- Concrete suggestions for new/modified extensions -->
<!-- Format:
### [Type: skill|command|agent] Name
- **Scope:** project | global
- **Rationale:** Why this would help
- **Source:** Specific conversation/moment that inspired this
-->
