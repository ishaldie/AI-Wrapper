namespace ZSR.Underwriting.Domain.Enums;

public enum DealPhase
{
    Acquisition,  // Draft, Screening/InProgress, Complete
    Contract,     // UnderContract
    Ownership,    // Closed, Active
    Exit          // Disposition, Sold
}
