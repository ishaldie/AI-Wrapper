# Tech Stack: ZSR Underwriting Wrapper

## Framework
- **ASP.NET 10** (.NET 10 SDK)
- **Blazor Server** for interactive UI (real-time C# components, no JS framework)
- **C#** as primary language

## Architecture
- **Clean Architecture** pattern: Domain -> Application -> Infrastructure -> Presentation
- **Solution structure**:
  - `ZSR.Underwriting.Domain` — Entities, value objects, interfaces
  - `ZSR.Underwriting.Application` — Use cases, services, DTOs, calculation engine
  - `ZSR.Underwriting.Infrastructure` — EF Core, Claude client, file storage
  - `ZSR.Underwriting.Web` — Blazor Server app, pages, components, layout

## Database
- **Entity Framework Core** (Code First)
- **SQL Server** (production) / **SQLite** (development)
- Migrations managed via `dotnet ef`

## AI Integration
- **Claude API** (Anthropic) for prose generation, risk analysis, investment decisions
- Anthropic .NET SDK or direct HTTP client

## External Integrations
- **Web search** — supplemental market context (employers, pipeline, rates)

## Authentication
- **ASP.NET Identity** with cookie-based auth
- Role-based access: Analyst, Admin

## Key Libraries
- **MudBlazor** — Blazor component library (tables, forms, charts)
- **QuestPDF** or **IronPDF** — PDF report generation
- **FluentValidation** — input validation
- **Serilog** — structured logging
- **Polly** — HTTP retry/resilience for external API calls

## Testing
- **xUnit** — unit and integration tests
- **bUnit** — Blazor component tests
- **Moq** or **NSubstitute** — mocking
- Tests in `ZSR.Underwriting.Tests` project

## Target Platform
- Web (desktop browser, responsive)
- Hosted on Azure App Service or IIS
