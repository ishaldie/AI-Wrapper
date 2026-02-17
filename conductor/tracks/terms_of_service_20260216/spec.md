# Spec: Terms of Service & Privacy Policy

**Track ID:** `terms_of_service_20260216`
**Type:** feature
**Date:** 2026-02-16

## Overview

Add Terms of Service (TOS) and Privacy Policy legal pages to the ZSR Underwriting Wrapper, with an acceptance gate that requires users to agree before accessing the application. Content is modeled after RealAI's TOS/Privacy Policy, adapted for an AI-powered underwriting tool with strong disclaimers around AI-generated output accuracy and "not investment advice" language.

The feature covers: public legal pages viewable without auth, a TOS acceptance checkbox on registration, an acceptance gate for OAuth new users and existing users when TOS version changes, and middleware enforcement.

## Requirements

1. **Public Terms of Service page** (`/terms`) — Static SSR, `[AllowAnonymous]`, `PublicLayout`. Displays the full ZSR Underwriting Terms of Service with sections:
   - Acceptance of Terms (consent by use, right to modify, 14-day notice)
   - Description of Service (AI-powered underwriting tool)
   - Account Registration & Security
   - User Content & Uploaded Documents (rent rolls, T12s, OMs — license to process)
   - AI Technology & Output Accuracy (AI output may be incorrect, human review required, no reliance as sole source of truth)
   - Not Investment Advice (reports are analytical tools, not investment recommendations)
   - Intellectual Property (service content, trademarks, user content license)
   - Third-Party Services & Data (RealAI API, Claude AI, web search — no guarantee of third-party accuracy)
   - Acceptable Use (prohibited activities)
   - Disclaimers (AS IS / AS AVAILABLE)
   - Limitation of Liability (capped damages)
   - Indemnification
   - Termination
   - Modifications to Terms
   - General Provisions (governing law, severability, entire agreement)

2. **Public Privacy Policy page** (`/privacy`) — Static SSR, `[AllowAnonymous]`, `PublicLayout`. Displays the full ZSR Underwriting Privacy Policy with sections:
   - Information We Collect (account info, uploaded documents, automatically-collected usage data)
   - How We Use Your Information (service operation, AI processing, report generation, analytics)
   - Third-Party Services (RealAI API, Anthropic/Claude API, hosting provider, payment processor if applicable)
   - Cookies & Tracking (minimal — session cookies for Blazor Server, optional analytics)
   - Data Security (safeguards, no 100% guarantee)
   - Data Retention (how long data is kept, document retention)
   - Your Rights & Choices (access, deletion, correction, opt-out of marketing)
   - Children's Privacy (not directed to under 18)
   - Changes to This Policy
   - Contact Information

3. **Domain model changes** — Add `TosAcceptedAt` (DateTime?) and `TosVersion` (string?) properties to `ApplicationUser`. Create an EF Core migration.

4. **Registration TOS checkbox** — Add "I agree to the Terms of Service and Privacy Policy" checkbox to the Register page (`/register`). Links open `/terms` and `/privacy` in new tabs. Registration form validation fails if unchecked.

5. **OAuth new-user gate** — When a brand-new user is auto-created via Google/Microsoft OAuth in `ExternalAuthEndpoints`, redirect to `/accept-terms` instead of `/search` (since they haven't accepted TOS yet).

6. **Accept Terms page** (`/accept-terms`) — Static SSR, `[Authorize]`, `PublicLayout`. Displays a summary of TOS + Privacy Policy with links to full pages, a checkbox "I agree to the Terms of Service and Privacy Policy", and a submit button. On submit: records `TosAcceptedAt = DateTime.UtcNow` and `TosVersion = current version` on the user entity, then redirects to `/search`.

7. **TOS enforcement middleware** — Custom middleware (or check in `Routes.razor` / a redirect component) that runs after authentication. If the authenticated user's `TosVersion` does not match the current app TOS version from config, redirect to `/accept-terms`. Exempt paths: `/accept-terms`, `/terms`, `/privacy`, `/logout`, `/api/auth/*`, and static assets.

8. **TOS version configuration** — Store `"TosVersion": "1.0"` in `appsettings.json`. Inject via `IConfiguration` or a strongly-typed options class. Bumping the version forces all users to re-accept.

9. **Navigation links** — Add footer links to Terms of Service and Privacy Policy on `PublicLayout`. Add a link in `AppLayout` footer or sidebar.

## Acceptance Criteria

- [ ] `/terms` page displays full Terms of Service and is accessible without authentication
- [ ] `/privacy` page displays full Privacy Policy and is accessible without authentication
- [ ] TOS content includes AI output accuracy disclaimers and "not investment advice" language
- [ ] Privacy Policy content covers uploaded document processing and third-party API data sharing
- [ ] `ApplicationUser` has `TosAcceptedAt` and `TosVersion` fields persisted in DB via migration
- [ ] Registration form requires TOS/Privacy checkbox; form cannot submit without it
- [ ] New OAuth users are redirected to `/accept-terms` before accessing the app
- [ ] Existing users without current TOS version acceptance are redirected to `/accept-terms`
- [ ] `/accept-terms` page records acceptance timestamp and version, then redirects to `/search`
- [ ] Bumping `TosVersion` in appsettings forces all users to re-accept
- [ ] `/accept-terms`, `/terms`, `/privacy`, `/logout` are exempt from TOS redirect
- [ ] All new pages use static SSR (no `@rendermode`) consistent with auth page patterns
- [ ] Footer links to TOS and Privacy Policy are visible on public and authenticated layouts

## Out of Scope

- Cookie consent banner (separate track if needed)
- Admin UI for editing TOS/Privacy content (hardcoded Razor for v1)
- Email notification when TOS version changes
- TOS acceptance history/audit log (only latest acceptance tracked)
- Arbitration clause (internal tool, not needed)
- Payment/subscription terms (no fees currently)
- Mobile app terms (web-only)
- TCPA/SMS consent (no phone communications)
- Detailed state privacy notices (simplified for v1)
