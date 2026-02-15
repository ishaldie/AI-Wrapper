# Plan: Project Setup & Architecture

## Phase 1: Solution Scaffolding
- [x] Create .NET 10 solution file `ZSR.Underwriting.sln`
- [x] Create `src/ZSR.Underwriting.Domain` class library project
- [x] Create `src/ZSR.Underwriting.Application` class library project
- [x] Create `src/ZSR.Underwriting.Infrastructure` class library project
- [x] Create `src/ZSR.Underwriting.Web` Blazor Server project
- [x] Create `tests/ZSR.Underwriting.Tests` xUnit test project
- [x] Set up project references (Clean Architecture dependency flow)
- [x] Add `.gitignore` for .NET

## Phase 2: NuGet Packages & Configuration
- [x] Install MudBlazor (or Radzen) in Web project
- [x] Install EF Core + SQLite + SQL Server providers in Infrastructure
- [x] Install Serilog packages in Web project
- [x] Install FluentValidation in Application project
- [x] Install Polly in Infrastructure project
- [x] Install Anthropic SDK (or HTTP client setup) in Infrastructure
- [x] Configure `appsettings.json` with all config sections
- [x] Set up user secrets for API keys

## Phase 3: Base Application Setup
- [x] Configure dependency injection in `Program.cs`
- [x] Set up Serilog logging pipeline
- [x] Create base Blazor layout (`MainLayout.razor`) with MudBlazor
- [x] Create navigation sidebar with placeholder menu items
- [x] Create `_Imports.razor` with common using statements
- [x] Verify `dotnet build` and `dotnet run` succeed
- [x] Write smoke test to verify DI container resolves services
