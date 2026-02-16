namespace ZSR.Underwriting.Application.Calculations;

public static class SensitivityCalculator
{
    public static decimal CalculateIncomeStressNoi(
        decimal gpr, decimal occupancyPercent, decimal otherIncomePercent,
        decimal opExRatio, decimal incomeReductionPercent)
    {
        var calc = new UnderwritingCalculator();
        var stressedGpr = gpr * (1m - incomeReductionPercent / 100m);
        var vacancyLoss = calc.CalculateVacancyLoss(stressedGpr, occupancyPercent);
        var netRent = calc.CalculateNetRent(stressedGpr, vacancyLoss);
        var otherIncome = calc.CalculateOtherIncome(netRent, otherIncomePercent: otherIncomePercent);
        var egi = calc.CalculateEgi(netRent, otherIncome);
        var opEx = calc.CalculateOperatingExpenses(egi, null, opExRatio);
        return calc.CalculateNoi(egi, opEx);
    }

    public static decimal CalculateOccupancyStressNoi(
        decimal gpr, decimal baseOccupancyPercent, decimal occupancyDropPercent,
        decimal otherIncomePercent, decimal opExRatio)
    {
        var calc = new UnderwritingCalculator();
        var stressedOccupancy = baseOccupancyPercent - occupancyDropPercent;
        var vacancyLoss = calc.CalculateVacancyLoss(gpr, stressedOccupancy);
        var netRent = calc.CalculateNetRent(gpr, vacancyLoss);
        var otherIncome = calc.CalculateOtherIncome(netRent, otherIncomePercent: otherIncomePercent);
        var egi = calc.CalculateEgi(netRent, otherIncome);
        var opEx = calc.CalculateOperatingExpenses(egi, null, opExRatio);
        return calc.CalculateNoi(egi, opEx);
    }

    public static List<SensitivityScenario> RunScenarios(
        decimal gpr, decimal occupancyPercent, decimal otherIncomePercent,
        decimal opExRatio, decimal purchasePrice,
        decimal debtService, decimal reserves, decimal equityRequired,
        decimal exitCapPercent, decimal terminalNoi)
    {
        var calc = new UnderwritingCalculator();

        // Base case NOI
        var vacancyLoss = calc.CalculateVacancyLoss(gpr, occupancyPercent);
        var netRent = calc.CalculateNetRent(gpr, vacancyLoss);
        var otherIncome = calc.CalculateOtherIncome(netRent, otherIncomePercent: otherIncomePercent);
        var egi = calc.CalculateEgi(netRent, otherIncome);
        var opEx = calc.CalculateOperatingExpenses(egi, null, opExRatio);
        var baseNoi = calc.CalculateNoi(egi, opEx);

        var baseExitValue = calc.CalculateExitValue(terminalNoi, exitCapPercent);

        // Stress scenarios
        var incomeStressNoi = CalculateIncomeStressNoi(gpr, occupancyPercent, otherIncomePercent, opExRatio, 5m);
        var occupancyStressNoi = CalculateOccupancyStressNoi(gpr, occupancyPercent, 10m, otherIncomePercent, opExRatio);
        var stressedExitValue = calc.CalculateExitValue(terminalNoi, exitCapPercent + 1.0m);

        return new List<SensitivityScenario>
        {
            new() { Name = "Base Case", Noi = baseNoi, NoiDelta = 0m, ExitValue = baseExitValue, ExitValueDelta = 0m },
            new() { Name = "Income -5%", Noi = incomeStressNoi, NoiDelta = incomeStressNoi - baseNoi, ExitValue = baseExitValue, ExitValueDelta = 0m },
            new() { Name = "Occupancy -10%", Noi = occupancyStressNoi, NoiDelta = occupancyStressNoi - baseNoi, ExitValue = baseExitValue, ExitValueDelta = 0m },
            new() { Name = "Cap Rate +100bps", Noi = baseNoi, NoiDelta = 0m, ExitValue = stressedExitValue, ExitValueDelta = stressedExitValue - baseExitValue },
        };
    }
}

public class SensitivityScenario
{
    public string Name { get; set; } = "";
    public decimal Noi { get; set; }
    public decimal NoiDelta { get; set; }
    public decimal ExitValue { get; set; }
    public decimal ExitValueDelta { get; set; }
}
