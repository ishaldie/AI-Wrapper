using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Domain.Entities;

namespace ZSR.Underwriting.Application.Interfaces;

public interface IVarianceCalculator
{
    VarianceReport CalculateVariance(CalculationResult projections, IReadOnlyList<MonthlyActual> actuals);
}
