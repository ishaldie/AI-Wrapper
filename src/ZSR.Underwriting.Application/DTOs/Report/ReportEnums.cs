namespace ZSR.Underwriting.Application.DTOs.Report;

public enum InvestmentDecisionType
{
    Go,
    ConditionalGo,
    NoGo
}

public enum RiskSeverity
{
    Low,
    Medium,
    High
}

public enum DataSource
{
    UserInput,
    ProtocolDefault,
    RealAi,
    Calculated,
    AiGenerated,
    MarketData
}
