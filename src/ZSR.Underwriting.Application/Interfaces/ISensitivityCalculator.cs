using ZSR.Underwriting.Application.Calculations;

namespace ZSR.Underwriting.Application.Interfaces;

public interface ISensitivityCalculator
{
    List<SensitivityScenario> RunScenarios(
        decimal gpr, decimal occupancyPercent, decimal otherIncomePercent,
        decimal opExRatio, decimal purchasePrice,
        decimal debtService, decimal reserves, decimal equityRequired,
        decimal exitCapPercent, decimal terminalNoi);
}
