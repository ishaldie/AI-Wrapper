namespace ZSR.Underwriting.Domain.Enums;

public enum DealStatus
{
    Draft,
    InProgress,      // Legacy — alias for Screening (backward compat)
    Screening,       // Actively being underwritten
    Complete,        // Underwriting done, GO/NO GO made
    Archived,        // Passed/shelved (can happen at any phase)
    UnderContract,   // PSA signed, due diligence in progress
    Closed,          // Acquisition closed
    Active,          // In asset management, property operating
    Disposition,     // Preparing to sell or refinance
    Sold             // Final disposition complete
}
