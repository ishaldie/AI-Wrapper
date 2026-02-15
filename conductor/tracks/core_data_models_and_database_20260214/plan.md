# Plan: Core Data Models & Database

## Phase 1: Domain Entities
- [x] Create `Deal` entity with status enum (Draft, InProgress, Complete, Archived) c5b9357
- [x] Create `Property` entity with address, units, year built, building type f74d0c8
- [x] Create `UnderwritingInput` entity with all protocol input fields d238a9d
- [x] Create `LoanTerms` value object 3082cd6
- [x] Create `RealAiData` entity for cached API response data 8bdd9bc
- [x] Create `CalculationResult` entity for all computed metrics cbdbcc5
- [x] Create `UnderwritingReport` entity for assembled report 6064e32
- [x] Create `UploadedDocument` entity for file references (completed by Document Upload track)
- [x] Write unit tests for entity validation (83 tests passing across all entities)

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
