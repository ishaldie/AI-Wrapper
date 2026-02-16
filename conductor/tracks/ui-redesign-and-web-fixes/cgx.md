# CGX: UI Redesign & Web Fixes

## Friction Points
- **MudBlazor SSR incompatibility**: MudTextField does NOT render `name` attributes in static SSR mode. Auth forms (Login/Register) must use plain HTML `<input>` with explicit `name="Input.PropertyName"` for `[SupplyParameterFromForm]` binding.
- **Static asset auth blocking**: Fallback auth policy (`RequireAuthenticatedUser`) blocks MudBlazor CSS/JS for unauthenticated users. Must use `MapStaticAssets().AllowAnonymous()`.
- **MudBlazor Typography FontWeight**: The `FontWeight` property is `string`, not `int`. Using `FontWeight = 700` causes CS0029. Must be `FontWeight = "700"`.
- **App process port conflicts**: Running app locks port 5118. Must kill process before rebuild/restart.
- **Antiforgery token duplication**: `EditForm` with `FormName` auto-includes antiforgery token. Adding explicit `<AntiforgeryToken />` creates duplicate hidden inputs.

## Good Patterns
- **HTML inputs for SSR forms**: Use `<input name="Input.Email" value="@CurrentInput.Email" />` pattern for any static SSR form that needs `[SupplyParameterFromForm]` binding.
- **card-elevated CSS class**: Reusable `Elevation="0"` + `border: 1px solid #E5E7EB` + hover shadow transition pattern.
- **animate-in + delay-N**: Staggered page load animations via CSS only - no JS needed.
- **`IsDarkMode="false"`**: Force light mode on MudThemeProvider to prevent system dark-mode preference from applying unconfigured PaletteDark.

## Missing Capabilities
- No way to detect MudBlazor SSR compatibility issues at build time - they only surface at runtime when form data is empty.

## Improvement Candidates
- Create a reusable `SsrTextField` component that wraps HTML input with consistent styling and proper `name` attribute generation.
- Add CSS styleguide to `conductor/code_styleguides/` for the Plus Jakarta Sans design system tokens.
