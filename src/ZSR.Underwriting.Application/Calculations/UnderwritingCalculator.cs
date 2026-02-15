using ZSR.Underwriting.Domain.Interfaces;

namespace ZSR.Underwriting.Application.Calculations;

public class UnderwritingCalculator : IUnderwritingCalculator
{
    public decimal CalculateGpr(decimal rentPerUnit, int unitCount)
    {
        return rentPerUnit * unitCount * 12;
    }

    public decimal CalculateVacancyLoss(decimal gpr, decimal occupancyPercent)
    {
        var vacancyRate = 1m - (occupancyPercent / 100m);
        return gpr * vacancyRate;
    }

    public decimal CalculateNetRent(decimal gpr, decimal vacancyLoss)
    {
        return gpr - vacancyLoss;
    }

    public decimal CalculateOtherIncome(decimal netRent, decimal? actualOtherIncome = null, decimal otherIncomePercent = 0.135m)
    {
        if (actualOtherIncome.HasValue)
            return actualOtherIncome.Value;

        return netRent * otherIncomePercent;
    }

    public decimal CalculateEgi(decimal netRent, decimal otherIncome)
    {
        return netRent + otherIncome;
    }

    public decimal CalculateOperatingExpenses(decimal egi, decimal? actualExpenses, decimal opExRatio = 0.5435m)
    {
        if (actualExpenses.HasValue)
            return actualExpenses.Value;

        return Math.Round(egi * opExRatio, 2);
    }

    public decimal CalculateNoi(decimal egi, decimal operatingExpenses)
    {
        return egi - operatingExpenses;
    }

    public decimal CalculateNoiMargin(decimal noi, decimal egi)
    {
        if (egi == 0m)
            return 0m;

        return Math.Round((noi / egi) * 100m, 1);
    }
}
