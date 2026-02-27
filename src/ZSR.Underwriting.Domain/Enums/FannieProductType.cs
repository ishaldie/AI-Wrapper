namespace ZSR.Underwriting.Domain.Enums;

/// <summary>
/// Fannie Mae Multifamily product types, each with distinct underwriting
/// parameters per the official term sheets at multifamily.fanniemae.com.
/// </summary>
public enum FannieProductType
{
    Conventional,
    SmallLoan,
    AffordableHousing,
    SeniorsIL,
    SeniorsAL,
    SeniorsALZ,
    StudentHousing,
    ManufacturedHousing,
    Cooperative,
    SARM,
    GreenRewards,
    Supplemental,
    NearStabilization,
    ROAR
}
