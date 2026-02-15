# Plan: Core Data Models & Database

## Phase 1: Domain Entities
- [x] Create `Deal` entity with status enum (Draft, InProgress, Complete, Archived) c5b9357
- [x] Create `Property` entity with address, units, year built, building type f74d0c8
- [~] Create `UnderwritingInput` entity with all protocol input fields
- [ ] Create `LoanTerms` value object
- [ ] Create `RealAiData` entity for cached API response data
- [ ] Create `CalculationResult` entity for all computed metrics
- [ ] Create `UnderwritingReport` entity for assembled report
- [ ] Create `UploadedDocument` entity for file references
- [ ] Write unit tests for entity validation

## Phase 2: EF Core Configuration
- [ ] Create `AppDbContext` with all DbSets
- [ ] Configure entity relationships (Deal → Property, Inputs, Results, Report)
- [ ] Configure indexes on frequently queried fields
- [ ] Configure cascade delete behavior
- [ ] Create initial migration
- [ ] Write integration tests for CRUD operations

## Phase 3: Repository Layer
- [ ] Create `IDealRepository` interface in Domain
- [ ] Implement `DealRepository` in Infrastructure
- [ ] Create `IUnitOfWork` interface
- [ ] Register repositories in DI container
- [ ] Write integration tests for repository methods
