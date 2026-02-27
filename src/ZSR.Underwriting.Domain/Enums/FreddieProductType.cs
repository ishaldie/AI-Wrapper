namespace ZSR.Underwriting.Domain.Enums;

/// <summary>
/// Freddie Mac Multifamily product types, each with distinct underwriting
/// parameters per the official term sheets at mf.freddiemac.com.
/// </summary>
public enum FreddieProductType
{
    Conventional,
    SmallBalanceLoan,
    TargetedAffordable,
    SeniorsIL,
    SeniorsAL,
    SeniorsSN,
    StudentHousing,
    ManufacturedHousing,
    FloatingRate,
    ValueAdd,
    ModerateRehab,
    LeaseUp,
    Supplemental,
    TaxExemptLIHTC,
    Section8,
    NOAHPreservation
}
