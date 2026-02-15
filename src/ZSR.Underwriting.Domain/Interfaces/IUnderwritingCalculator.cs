namespace ZSR.Underwriting.Domain.Interfaces;

public interface IUnderwritingCalculator
{
    decimal CalculateGpr(decimal rentPerUnit, int unitCount);
    decimal CalculateVacancyLoss(decimal gpr, decimal occupancyPercent);
    decimal CalculateNetRent(decimal gpr, decimal vacancyLoss);
    decimal CalculateOtherIncome(decimal netRent, decimal? actualOtherIncome = null, decimal otherIncomePercent = 0.135m);
    decimal CalculateEgi(decimal netRent, decimal otherIncome);
    decimal CalculateOperatingExpenses(decimal egi, decimal? actualExpenses, decimal opExRatio = 0.5435m);
    decimal CalculateNoi(decimal egi, decimal operatingExpenses);
    decimal CalculateNoiMargin(decimal noi, decimal egi);
}
