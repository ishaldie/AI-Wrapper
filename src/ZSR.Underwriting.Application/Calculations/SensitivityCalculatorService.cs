using ZSR.Underwriting.Application.Interfaces;

namespace ZSR.Underwriting.Application.Calculations;

public class SensitivityCalculatorService : ISensitivityCalculator
{
    public List<SensitivityScenario> RunScenarios(
        decimal gpr, decimal occupancyPercent, decimal otherIncomePercent,
        decimal opExRatio, decimal purchasePrice,
        decimal debtService, decimal reserves, decimal equityRequired,
        decimal exitCapPercent, decimal terminalNoi)
    {
        return SensitivityCalculator.RunScenarios(
            gpr, occupancyPercent, otherIncomePercent, opExRatio, purchasePrice,
            debtService, reserves, equityRequired, exitCapPercent, terminalNoi);
    }
}
