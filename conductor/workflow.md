# Workflow: ZSR Underwriting Wrapper

## Development Methodology
- **TDD (Test-Driven Development)**
  1. Write failing test first
  2. Run test, confirm it fails
  3. Implement minimum code to pass
  4. Run test, confirm it passes
  5. Refactor if needed

## Commit Strategy
- **Per phase**: One commit per completed phase (batch of tasks)
- Commit message format: `conductor(phase): [Phase description]`
- Task-level commits: `conductor(task): [Task description]`
- Checkpoint commits: `conductor(checkpoint): Phase [N] complete`
- Revert commits: `conductor(revert): Revert [scope] - [reason]`

## Code Review
- Plans must be approved before implementation begins
- Phase completion requires user verification before proceeding

## Testing Requirements
- Unit tests for all calculation logic and services
- Integration tests for database operations and external API clients
- bUnit tests for critical Blazor components
- Tests live in `ZSR.Underwriting.Tests/`

## Solution Structure
```
AI Wrappers/
├── conductor/                    # Conductor workflow files
├── src/
│   ├── ZSR.Underwriting.Domain/        # Entities, interfaces, value objects
│   ├── ZSR.Underwriting.Application/   # Services, DTOs, calculation engine
│   ├── ZSR.Underwriting.Infrastructure/# EF Core, API clients, file storage
│   └── ZSR.Underwriting.Web/           # Blazor Server app
├── tests/
│   └── ZSR.Underwriting.Tests/         # xUnit + bUnit tests
└── ZSR.Underwriting.sln
```

## Quality Standards
- Follow C# naming conventions (PascalCase for public members, camelCase for private)
- Clean Architecture: dependencies point inward (Domain has zero external deps)
- Use dependency injection throughout
- All external API calls wrapped in resilience policies (Polly)
- Async/await for all I/O-bound operations
- FluentValidation for input validation
- Structured logging with Serilog
