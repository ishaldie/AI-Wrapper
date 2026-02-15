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
[checkpoint: edfdfe4]

## Phase 2: EF Core Configuration
- [x] Create `AppDbContext` with all DbSets a53edc9
- [x] Configure entity relationships (Deal → Property, Inputs, Results, Report) a53edc9
- [x] Configure indexes on frequently queried fields a53edc9
- [x] Configure cascade delete behavior a53edc9
- [x] Create initial migration e0b35f4
- [x] Write integration tests for CRUD operations 41ff59f
[checkpoint: a4976b9]

## Phase 3: Repository Layer
- [x] Create `IDealRepository` interface in Domain ba32e50
- [x] Implement `DealRepository` in Infrastructure ba32e50
- [x] Create `IUnitOfWork` interface ba32e50
- [x] Register repositories in DI container ba32e50
- [x] Write integration tests for repository methods ba32e50
