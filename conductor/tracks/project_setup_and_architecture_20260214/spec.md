# Spec: Project Setup & Architecture

## Overview
Scaffold the ASP.NET Blazor Server solution with Clean Architecture, configure Entity Framework Core, set up dependency injection, secrets management, and the base application layout.

## Requirements
1. Create .NET 8 solution with 4 projects (Domain, Application, Infrastructure, Web)
2. Configure Blazor Server with interactive server-side rendering
3. Set up EF Core 8 with SQL Server (production) and SQLite (development)
4. Install core NuGet packages (MudBlazor/Radzen, Serilog, Polly, FluentValidation)
5. Configure dependency injection for all service layers
6. Set up `appsettings.json` with sections for ConnectionStrings, RealAI, Claude API, and application settings
7. Configure user secrets for API keys (never in source control)
8. Create base Blazor layout with navigation (sidebar + top bar)
9. Set up Serilog structured logging
10. Create `.gitignore` for .NET projects
11. Add `README.md` with setup instructions

## Acceptance Criteria
- [ ] `dotnet build` succeeds with zero warnings
- [ ] `dotnet run` launches Blazor Server app on localhost
- [ ] EF Core can connect to SQLite in dev mode
- [ ] Base layout renders with navigation shell
- [ ] Serilog writes structured logs to console and file
- [ ] API keys stored in user secrets, not in config files
- [ ] Solution follows Clean Architecture dependency rules

## Out of Scope
- Authentication (separate track)
- Database schema/entities (separate track)
- Any business logic or UI beyond scaffolding
