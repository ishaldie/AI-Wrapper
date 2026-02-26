namespace ZSR.Underwriting.Application.DTOs;

public class SellScenario
{
    public decimal EstimatedSalePrice { get; set; }
    public decimal SellingCosts { get; set; }        // Broker fees, closing costs
    public decimal NetProceeds { get; set; }
    public decimal RemainingLoanBalance { get; set; }
    public decimal EquityReturned { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal EquityMultiple { get; set; }
    public decimal RealizedIrr { get; set; }
    public int HoldPeriodMonths { get; set; }
}

public class HoldScenario
{
    public int AdditionalYears { get; set; }
    public decimal ProjectedExitValue { get; set; }
    public decimal ProjectedAnnualNoi { get; set; }
    public decimal ProjectedCashOnCash { get; set; }
    public decimal ProjectedIrr { get; set; }
    public decimal ProjectedEquityMultiple { get; set; }
}

public class RefinanceScenario
{
    public decimal NewLoanAmount { get; set; }
    public decimal CurrentLoanBalance { get; set; }
    public decimal CashOutAmount { get; set; }
    public decimal NewInterestRate { get; set; }
    public decimal NewAnnualDebtService { get; set; }
    public decimal GoForwardCashOnCash { get; set; }
    public decimal RemainingEquity { get; set; }
}
