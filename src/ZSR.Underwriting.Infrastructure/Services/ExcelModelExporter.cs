using System.Text.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using ZSR.Underwriting.Application.Interfaces;
using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;
using ZSR.Underwriting.Infrastructure.Data;

namespace ZSR.Underwriting.Infrastructure.Services;

public class ExcelModelExporter : IExcelModelExporter
{
    private readonly AppDbContext _db;

    public ExcelModelExporter(AppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> ExportAsync(Guid dealId, CancellationToken ct = default)
    {
        var deal = await _db.Deals
            .Include(d => d.Property)
            .Include(d => d.CalculationResult)
            .Include(d => d.CapitalStackItems)
            .FirstOrDefaultAsync(d => d.Id == dealId, ct)
            ?? throw new InvalidOperationException($"Deal {dealId} not found.");

        var rentRoll = await _db.RentRollUnits
            .Where(r => r.DealId == dealId)
            .OrderBy(r => r.UnitNumber)
            .ToListAsync(ct);

        var actuals = await _db.MonthlyActuals
            .Where(m => m.DealId == dealId)
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .ToListAsync(ct);

        var comps = await _db.SecuritizationComps
            .Where(c => c.PropertyType == deal.PropertyType && c.State == ExtractState(deal.Address))
            .OrderByDescending(c => c.OriginationDate)
            .Take(20)
            .ToListAsync(ct);

        var expenses = !string.IsNullOrEmpty(deal.DetailedExpensesJson)
            ? JsonSerializer.Deserialize<DetailedExpenses>(deal.DetailedExpensesJson)
            : null;

        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook();

            var stylesPart = wbPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = CreateStylesheet();
            stylesPart.Stylesheet.Save();

            var sheets = wbPart.Workbook.AppendChild(new Sheets());
            uint sheetId = 1;

            if (IsMultifamily(deal.PropertyType))
                BuildMultifamilyWorkbook(wbPart, sheets, ref sheetId, deal, expenses, actuals, rentRoll, comps);
            else
                BuildCommercialWorkbook(wbPart, sheets, ref sheetId, deal, expenses, actuals, rentRoll, comps);
        }

        return ms.ToArray();
    }

    // ── Template routers ────────────────────────────────────────────

    private static bool IsMultifamily(PropertyType type) => type is
        PropertyType.Multifamily or PropertyType.LIHTC or PropertyType.Bridge;

    private void BuildMultifamilyWorkbook(WorkbookPart wbPart, Sheets sheets, ref uint sheetId,
        Deal deal, DetailedExpenses? expenses, List<MonthlyActual> actuals,
        List<RentRollUnit> rentRoll, List<SecuritizationComp> comps)
    {
        AddSheet(wbPart, sheets, sheetId++, "Year 1 Pro-forma", sd => BuildMfProformaSheet(sd, deal, expenses, actuals));
        AddSheet(wbPart, sheets, sheetId++, "Underwriting", sd => BuildMfUnderwritingSheet(sd, deal, expenses));
        AddSheet(wbPart, sheets, sheetId++, "Rent Roll", sd => BuildRentRollSheet(sd, rentRoll));
        AddSheet(wbPart, sheets, sheetId++, "Sources & Uses", sd => BuildSourcesUsesSheet(sd, deal));
        AddSheet(wbPart, sheets, sheetId++, "Comps", sd => BuildCompsSheet(sd, comps));
    }

    private void BuildCommercialWorkbook(WorkbookPart wbPart, Sheets sheets, ref uint sheetId,
        Deal deal, DetailedExpenses? expenses, List<MonthlyActual> actuals,
        List<RentRollUnit> rentRoll, List<SecuritizationComp> comps)
    {
        AddSheet(wbPart, sheets, sheetId++, "Loan Sizing", sd => BuildCmLoanSizingSheet(sd, deal, expenses));
        AddSheet(wbPart, sheets, sheetId++, "Underwriting", sd => BuildCmUnderwritingSheet(sd, deal, expenses));
        AddSheet(wbPart, sheets, sheetId++, "Cash Flow", sd => BuildCashFlowSheet(sd, deal, actuals));
        AddSheet(wbPart, sheets, sheetId++, "Sources & Uses", sd => BuildSourcesUsesSheet(sd, deal));
        AddSheet(wbPart, sheets, sheetId++, "Comps", sd => BuildCompsSheet(sd, comps));
    }

    // ══════════════════════════════════════════════════════════════════
    // MULTIFAMILY SHEETS
    // ══════════════════════════════════════════════════════════════════

    private void BuildMfProformaSheet(SheetData sd, Deal deal, DetailedExpenses? expenses,
        List<MonthlyActual> actuals)
    {
        var calc = deal.CalculationResult;
        var units = deal.UnitCount > 0 ? deal.UnitCount : 1;

        // Title row
        AddRow(sd, StyleBoldHeader, deal.PropertyName);
        AddRow(sd, StyleNormal, deal.Address,
            "", "", "", "", "", "", "", "", "", "", "", "", "",
            $"{deal.UnitCount} Units");
        AddRow(sd, StyleNormal, "");

        // If we have monthly actuals, use them for the 12-month pro-forma
        var recentActuals = actuals
            .OrderByDescending(a => a.Year * 100 + a.Month)
            .Take(12)
            .OrderBy(a => a.Year * 100 + a.Month)
            .ToList();

        if (recentActuals.Count > 0)
        {
            BuildMonthlyProforma(sd, recentActuals, units);
        }
        else
        {
            // Fall back to annual summary from calc result
            BuildAnnualProforma(sd, deal, expenses, calc, units);
        }
    }

    private static void BuildMonthlyProforma(SheetData sd, List<MonthlyActual> months, int units)
    {
        // Column headers: [Label] [Month1..Month12] [Total] [Per Unit]
        var headers = new List<string> { "" };
        foreach (var m in months)
            headers.Add($"{new DateTime(m.Year, m.Month, 1):MMM yyyy}");
        headers.Add("Total");
        headers.Add("Per Unit");
        AddRow(sd, StyleColumnHeader, headers.ToArray());

        // Occupancy row
        var occRow = new List<string> { "OCCUPANCY" };
        foreach (var m in months)
            occRow.Add($"{m.OccupancyPercent:F0}%");
        var avgOcc = months.Average(m => m.OccupancyPercent);
        occRow.Add($"{avgOcc:F0}%");
        occRow.Add("");
        AddRow(sd, StyleNormal, occRow.ToArray());
        AddRow(sd, StyleNormal, "");

        // RENTAL INCOME section
        AddRow(sd, StyleSectionHeader, "RENTAL INCOME");
        AddMonthlyLine(sd, "Gross Potential Rent", months, m => m.GrossRentalIncome, units);
        AddMonthlyLine(sd, "Less: Vacancy Loss", months, m => -m.VacancyLoss, units);
        AddMonthlyLine(sd, "Total Rental Income", months, m => m.GrossRentalIncome - m.VacancyLoss, units);
        AddRow(sd, StyleNormal, "");

        // OTHER INCOME
        AddRow(sd, StyleSectionHeader, "OTHER INCOME");
        AddMonthlyLine(sd, "Other Income", months, m => m.OtherIncome, units);
        AddMonthlyLine(sd, "Total Income", months, m => m.EffectiveGrossIncome, units);
        AddRow(sd, StyleNormal, "");

        // EXPENSES
        AddRow(sd, StyleSectionHeader, "EXPENSES");
        AddMonthlyLine(sd, "Payroll", months, m => m.Payroll, units);
        AddMonthlyLine(sd, "Repairs & Maintenance", months, m => m.Repairs, units);
        AddMonthlyLine(sd, "Marketing", months, m => m.Marketing, units);
        AddMonthlyLine(sd, "General & Admin", months, m => m.Administrative, units);
        AddMonthlyLine(sd, "Utilities", months, m => m.Utilities, units);
        AddMonthlyLine(sd, "Management Fees", months, m => m.Management, units);
        AddMonthlyLine(sd, "Taxes", months, m => m.PropertyTaxes, units);
        AddMonthlyLine(sd, "Property Insurance", months, m => m.Insurance, units);
        AddMonthlyLine(sd, "Other Expenses", months, m => m.OtherExpenses, units);
        AddMonthlyLine(sd, "Total Expenses", months, m => m.TotalOperatingExpenses, units);
        AddRow(sd, StyleNormal, "");

        // NOI
        AddRow(sd, StyleSectionHeader, "NET OPERATING INCOME");
        AddMonthlyLine(sd, "NOI", months, m => m.NetOperatingIncome, units);
    }

    private static void BuildAnnualProforma(SheetData sd, Deal deal, DetailedExpenses? expenses,
        CalculationResult? calc, int units)
    {
        // Single-column annual summary when no monthly data exists
        AddRow(sd, StyleColumnHeader, "", "Annual ($)", "Per Unit ($)", "% of EGI");
        AddRow(sd, StyleNormal, "");

        AddRow(sd, StyleSectionHeader, "RENTAL INCOME");
        AddUwLine(sd, "Gross Potential Rent", calc?.GrossPotentialRent, units, calc?.EffectiveGrossIncome);
        AddUwLine(sd, "Less: Vacancy & Credit Loss", NegateNullable(calc?.VacancyLoss), units, calc?.EffectiveGrossIncome);
        AddRow(sd, StyleNormal, "");

        AddRow(sd, StyleSectionHeader, "OTHER INCOME");
        AddUwLine(sd, "Other Income", calc?.OtherIncome, units, calc?.EffectiveGrossIncome);
        AddUwLine(sd, "Total Income", calc?.EffectiveGrossIncome, units, calc?.EffectiveGrossIncome);
        AddRow(sd, StyleNormal, "");

        AddRow(sd, StyleSectionHeader, "EXPENSES");
        if (expenses is not null)
        {
            AddUwLine(sd, "Payroll", expenses.Payroll, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Repairs & Maintenance", expenses.RepairsAndMaintenance, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Marketing", expenses.Marketing, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "General & Admin", expenses.GeneralAndAdmin, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Utilities", expenses.Utilities, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Management Fees", expenses.ManagementFee, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Taxes", expenses.RealEstateTaxes, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Property Insurance", expenses.Insurance, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Replacement Reserves", expenses.ReplacementReserves, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Other Expenses", expenses.OtherExpenses, units, calc?.EffectiveGrossIncome);
            AddRow(sd, StyleNormal, "");
            AddUwLine(sd, "Total Expenses", expenses.Total, units, calc?.EffectiveGrossIncome);
        }
        else
        {
            AddUwLine(sd, "Total Expenses", calc?.OperatingExpenses, units, calc?.EffectiveGrossIncome);
        }
        AddRow(sd, StyleNormal, "");

        AddRow(sd, StyleSectionHeader, "NET OPERATING INCOME");
        AddUwLine(sd, "NOI", calc?.NetOperatingIncome, units, calc?.EffectiveGrossIncome);
    }

    private void BuildMfUnderwritingSheet(SheetData sd, Deal deal, DetailedExpenses? expenses)
    {
        var calc = deal.CalculationResult;
        var prop = deal.Property;
        var units = deal.UnitCount > 0 ? deal.UnitCount : 1;

        AddRow(sd, StyleBoldHeader, "UNDERWRITING ANALYSIS");
        AddRow(sd, StyleNormal, "");

        // Property Details
        AddRow(sd, StyleSectionHeader, "PROPERTY DETAILS");
        AddLabelValue(sd, "Property Name", deal.PropertyName);
        AddLabelValue(sd, "Address", deal.Address);
        AddLabelValue(sd, "Property Type", deal.PropertyType.ToString());
        AddLabelValue(sd, "Units", deal.UnitCount.ToString("N0"));
        if (prop?.YearBuilt is not null) AddLabelValue(sd, "Year Built", prop.YearBuilt.Value.ToString());
        if (prop?.SquareFootage is not null) AddLabelValue(sd, "Total SF", prop.SquareFootage.Value.ToString("N0"));
        AddRow(sd, StyleNormal, "");

        // Capitalization Summary
        AddRow(sd, StyleSectionHeader, "CAPITALIZATION SUMMARY");
        AddLabelCurrency(sd, "Purchase Price", deal.PurchasePrice);
        AddLabelCurrency(sd, "Price Per Unit", calc?.PricePerUnit);
        if (deal.CapexBudget is > 0) AddLabelCurrency(sd, "Initial Capital Budget", deal.CapexBudget);
        var totalBasis = deal.PurchasePrice + (deal.CapexBudget ?? 0) + (deal.PurchasePrice * 0.02m);
        AddLabelCurrency(sd, "Total Cost Basis (est.)", totalBasis);
        AddLabelCurrency(sd, "Debt", calc?.LoanAmount);
        var equity = totalBasis - (calc?.LoanAmount ?? 0);
        AddLabelCurrency(sd, "Equity Required", equity > 0 ? equity : 0);
        AddRow(sd, StyleNormal, "");

        // Investment Returns
        AddRow(sd, StyleSectionHeader, "INVESTMENT RETURNS");
        AddLabelPercent(sd, "Going-In Cap Rate", calc?.GoingInCapRate);
        AddLabelPercent(sd, "Exit Cap Rate", calc?.ExitCapRate);
        AddLabelPercent(sd, "IRR", calc?.InternalRateOfReturn);
        AddLabelDecimal(sd, "Equity Multiple", calc?.EquityMultiple, "0.00x");
        AddLabelPercent(sd, "Cash-on-Cash Return (Y1)", calc?.CashOnCashReturn);
        AddRow(sd, StyleNormal, "");

        // Loan Terms
        AddRow(sd, StyleSectionHeader, "LOAN TERMS");
        AddLabelPercent(sd, "LTV", deal.LoanLtv);
        AddLabelCurrency(sd, "Loan Amount", calc?.LoanAmount);
        AddLabelPercent(sd, "Interest Rate", deal.LoanRate);
        AddLabelValue(sd, "Amortization", deal.AmortizationYears.HasValue ? $"{deal.AmortizationYears} years" : "N/A");
        AddLabelValue(sd, "Loan Term", deal.LoanTermYears.HasValue ? $"{deal.LoanTermYears} years" : "N/A");
        AddLabelValue(sd, "Interest Only", deal.IsInterestOnly ? "Yes" : "No");
        AddRow(sd, StyleNormal, "");

        // Debt Metrics
        AddRow(sd, StyleSectionHeader, "DEBT METRICS");
        AddLabelDecimal(sd, "DSCR", calc?.DebtServiceCoverageRatio, "0.00x");
        var debtYield = (calc?.NetOperatingIncome is > 0 && calc?.LoanAmount is > 0)
            ? calc.NetOperatingIncome / calc.LoanAmount * 100
            : null;
        AddLabelPercent(sd, "Debt Yield", debtYield);
        AddLabelCurrency(sd, "Annual Debt Service", calc?.AnnualDebtService);
        AddRow(sd, StyleNormal, "");

        // Operating Statement
        AddRow(sd, StyleSectionHeader, "OPERATING STATEMENT");
        AddRow(sd, StyleColumnHeader, "", "Annual ($)", "Per Unit ($)", "% of EGI");
        AddUwLine(sd, "Gross Potential Rent", calc?.GrossPotentialRent, units, calc?.EffectiveGrossIncome);
        AddUwLine(sd, "Less: Vacancy", NegateNullable(calc?.VacancyLoss), units, calc?.EffectiveGrossIncome);
        AddUwLine(sd, "Other Income", calc?.OtherIncome, units, calc?.EffectiveGrossIncome);
        AddUwLine(sd, "Effective Gross Income", calc?.EffectiveGrossIncome, units, calc?.EffectiveGrossIncome);
        AddRow(sd, StyleNormal, "");
        AddUwLine(sd, "Total Expenses", calc?.OperatingExpenses ?? expenses?.Total, units, calc?.EffectiveGrossIncome);
        AddUwLine(sd, "NOI", calc?.NetOperatingIncome, units, calc?.EffectiveGrossIncome);

        // Cash flow projections
        if (!string.IsNullOrEmpty(calc?.CashFlowProjectionsJson))
        {
            AddRow(sd, StyleNormal, "");
            AddRow(sd, StyleSectionHeader, "PROJECTED CASH FLOWS");
            RenderJsonProjections(sd, calc.CashFlowProjectionsJson);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    // COMMERCIAL SHEETS
    // ══════════════════════════════════════════════════════════════════

    private void BuildCmLoanSizingSheet(SheetData sd, Deal deal, DetailedExpenses? expenses)
    {
        var calc = deal.CalculationResult;
        var prop = deal.Property;

        AddRow(sd, StyleBoldHeader, "LOAN SIZING SUMMARY");
        AddRow(sd, StyleNormal, "");

        // Property Information
        AddRow(sd, StyleSectionHeader, "PROPERTY INFORMATION");
        AddLabelValue(sd, "Property Name", deal.PropertyName);
        AddLabelValue(sd, "Address", deal.Address);
        AddLabelValue(sd, "Property Type", deal.PropertyType.ToString());
        AddLabelValue(sd, "Units / Keys", deal.UnitCount.ToString("N0"));
        if (prop?.YearBuilt is not null) AddLabelValue(sd, "Year Built", prop.YearBuilt.Value.ToString());
        if (prop?.SquareFootage is not null) AddLabelValue(sd, "Total SF", prop.SquareFootage.Value.ToString("N0"));
        AddRow(sd, StyleNormal, "");

        // Acquisition Pricing
        AddRow(sd, StyleSectionHeader, "ACQUISITION PRICING");
        AddLabelCurrency(sd, "Purchase Price", deal.PurchasePrice);
        AddLabelCurrency(sd, "Price Per Unit", calc?.PricePerUnit);
        AddLabelPercent(sd, "Going-In Cap Rate", calc?.GoingInCapRate);
        AddLabelPercent(sd, "Exit Cap Rate", calc?.ExitCapRate);
        AddRow(sd, StyleNormal, "");

        // Loan Sizing
        AddRow(sd, StyleSectionHeader, "LOAN SIZING");
        AddLabelPercent(sd, "LTV", deal.LoanLtv);
        AddLabelCurrency(sd, "Loan Amount", calc?.LoanAmount);
        AddLabelPercent(sd, "Interest Rate", deal.LoanRate);
        AddLabelValue(sd, "Amortization (Years)", deal.AmortizationYears?.ToString() ?? "N/A");
        AddLabelValue(sd, "Loan Term (Years)", deal.LoanTermYears?.ToString() ?? "N/A");
        AddLabelValue(sd, "Interest Only", deal.IsInterestOnly ? "Yes" : "No");
        AddLabelCurrency(sd, "Annual Debt Service", calc?.AnnualDebtService);
        AddRow(sd, StyleNormal, "");

        // Debt Metrics
        AddRow(sd, StyleSectionHeader, "DEBT METRICS");
        AddLabelDecimal(sd, "DSCR", calc?.DebtServiceCoverageRatio, "0.00x");
        var debtYield = (calc?.NetOperatingIncome is > 0 && calc?.LoanAmount is > 0)
            ? calc.NetOperatingIncome / calc.LoanAmount * 100
            : null;
        AddLabelPercent(sd, "Debt Yield", debtYield);
        AddRow(sd, StyleNormal, "");

        // Return Metrics
        AddRow(sd, StyleSectionHeader, "RETURN METRICS");
        AddLabelPercent(sd, "Cash-on-Cash Return", calc?.CashOnCashReturn);
        AddLabelPercent(sd, "IRR", calc?.InternalRateOfReturn);
        AddLabelDecimal(sd, "Equity Multiple", calc?.EquityMultiple, "0.00x");
        AddRow(sd, StyleNormal, "");

        // NOI Summary
        AddRow(sd, StyleSectionHeader, "NOI SUMMARY");
        AddLabelCurrency(sd, "Gross Potential Rent", calc?.GrossPotentialRent);
        AddLabelCurrency(sd, "Vacancy Loss", calc?.VacancyLoss);
        AddLabelCurrency(sd, "Effective Gross Income", calc?.EffectiveGrossIncome);
        AddLabelCurrency(sd, "Operating Expenses", calc?.OperatingExpenses ?? expenses?.Total);
        AddLabelCurrency(sd, "Net Operating Income", calc?.NetOperatingIncome);
        AddRow(sd, StyleNormal, "");

        // Capital Stack
        if (deal.CapitalStackItems.Count > 0)
        {
            AddRow(sd, StyleSectionHeader, "CAPITAL STACK");
            AddRow(sd, StyleColumnHeader, "Source", "Amount", "Rate", "Term");
            foreach (var item in deal.CapitalStackItems.OrderBy(c => c.SortOrder))
            {
                AddRow(sd, StyleNormal,
                    item.Source.ToString(),
                    FormatCurrency(item.Amount),
                    item.Rate.HasValue ? $"{item.Rate:F2}%" : "",
                    item.TermYears.HasValue ? $"{item.TermYears} yrs" : "");
            }
        }
    }

    private void BuildCmUnderwritingSheet(SheetData sd, Deal deal, DetailedExpenses? expenses)
    {
        var calc = deal.CalculationResult;
        var units = deal.UnitCount > 0 ? deal.UnitCount : 1;

        AddRow(sd, StyleBoldHeader, "UNDERWRITING - OPERATING STATEMENT");
        AddRow(sd, StyleNormal, "");

        AddRow(sd, StyleColumnHeader, "", "Annual ($)", "Per Unit ($)", "% of EGI");
        AddRow(sd, StyleNormal, "");

        // Revenue
        AddRow(sd, StyleSectionHeader, "REVENUE");
        AddUwLine(sd, "Gross Potential Rent", calc?.GrossPotentialRent, units, calc?.EffectiveGrossIncome);
        AddUwLine(sd, "Less: Vacancy & Credit Loss", NegateNullable(calc?.VacancyLoss), units, calc?.EffectiveGrossIncome);
        AddUwLine(sd, "Other Income", calc?.OtherIncome, units, calc?.EffectiveGrossIncome);
        AddRow(sd, StyleNormal, "");
        AddUwLine(sd, "Effective Gross Income", calc?.EffectiveGrossIncome, units, calc?.EffectiveGrossIncome);
        AddRow(sd, StyleNormal, "");

        // Expenses
        AddRow(sd, StyleSectionHeader, "OPERATING EXPENSES");
        if (expenses is not null)
        {
            AddUwLine(sd, "Real Estate Taxes", expenses.RealEstateTaxes, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Insurance", expenses.Insurance, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Utilities", expenses.Utilities, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Repairs & Maintenance", expenses.RepairsAndMaintenance, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Payroll", expenses.Payroll, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Marketing", expenses.Marketing, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "General & Administrative", expenses.GeneralAndAdmin, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Management Fee", expenses.ManagementFee, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Replacement Reserves", expenses.ReplacementReserves, units, calc?.EffectiveGrossIncome);
            AddUwLine(sd, "Other Expenses", expenses.OtherExpenses, units, calc?.EffectiveGrossIncome);
            AddRow(sd, StyleNormal, "");
            AddUwLine(sd, "Total Operating Expenses", expenses.Total, units, calc?.EffectiveGrossIncome);
        }
        else
        {
            AddUwLine(sd, "Total Operating Expenses", calc?.OperatingExpenses, units, calc?.EffectiveGrossIncome);
        }
        AddRow(sd, StyleNormal, "");

        // NOI
        AddRow(sd, StyleSectionHeader, "NET OPERATING INCOME");
        AddUwLine(sd, "NOI", calc?.NetOperatingIncome, units, calc?.EffectiveGrossIncome);
        AddRow(sd, StyleNormal, "");

        // Below the line
        AddRow(sd, StyleSectionHeader, "BELOW THE LINE");
        AddUwLine(sd, "Annual Debt Service", NegateNullable(calc?.AnnualDebtService), units, calc?.EffectiveGrossIncome);
        var cashAfterDebt = (calc?.NetOperatingIncome ?? 0) - (calc?.AnnualDebtService ?? 0);
        AddUwLine(sd, "Cash Flow After Debt Service", cashAfterDebt, units, calc?.EffectiveGrossIncome);
    }

    // ══════════════════════════════════════════════════════════════════
    // SHARED SHEETS
    // ══════════════════════════════════════════════════════════════════

    private void BuildCashFlowSheet(SheetData sd, Deal deal, List<MonthlyActual> actuals)
    {
        AddRow(sd, StyleBoldHeader, "CASH FLOW ANALYSIS");
        AddRow(sd, StyleNormal, "");

        if (actuals.Count > 0)
        {
            var years = actuals.GroupBy(a => a.Year).OrderBy(g => g.Key).ToList();

            var headers = new List<string> { "Line Item" };
            headers.AddRange(years.Select(y => y.Key.ToString()));
            AddRow(sd, StyleColumnHeader, headers.ToArray());
            AddRow(sd, StyleNormal, "");

            AddRow(sd, StyleSectionHeader, "REVENUE");
            AddActualsRow(sd, "Gross Rental Income", years, g => g.Sum(a => a.GrossRentalIncome));
            AddActualsRow(sd, "Less: Vacancy Loss", years, g => -g.Sum(a => a.VacancyLoss));
            AddActualsRow(sd, "Other Income", years, g => g.Sum(a => a.OtherIncome));
            AddActualsRow(sd, "Effective Gross Income", years, g => g.Sum(a => a.EffectiveGrossIncome));
            AddRow(sd, StyleNormal, "");

            AddRow(sd, StyleSectionHeader, "OPERATING EXPENSES");
            AddActualsRow(sd, "Property Taxes", years, g => g.Sum(a => a.PropertyTaxes));
            AddActualsRow(sd, "Insurance", years, g => g.Sum(a => a.Insurance));
            AddActualsRow(sd, "Utilities", years, g => g.Sum(a => a.Utilities));
            AddActualsRow(sd, "Repairs & Maintenance", years, g => g.Sum(a => a.Repairs));
            AddActualsRow(sd, "Management", years, g => g.Sum(a => a.Management));
            AddActualsRow(sd, "Payroll", years, g => g.Sum(a => a.Payroll));
            AddActualsRow(sd, "Marketing", years, g => g.Sum(a => a.Marketing));
            AddActualsRow(sd, "Administrative", years, g => g.Sum(a => a.Administrative));
            AddActualsRow(sd, "Other Expenses", years, g => g.Sum(a => a.OtherExpenses));
            AddActualsRow(sd, "Total Operating Expenses", years, g => g.Sum(a => a.TotalOperatingExpenses));
            AddRow(sd, StyleNormal, "");

            AddRow(sd, StyleSectionHeader, "NET OPERATING INCOME");
            AddActualsRow(sd, "NOI", years, g => g.Sum(a => a.NetOperatingIncome));
            AddActualsRow(sd, "Debt Service", years, g => -g.Sum(a => a.DebtService));
            AddActualsRow(sd, "Capital Expenditures", years, g => -g.Sum(a => a.CapitalExpenditures));
            AddActualsRow(sd, "Cash Flow", years, g => g.Sum(a => a.CashFlow));
            AddRow(sd, StyleNormal, "");

            AddRow(sd, StyleSectionHeader, "OCCUPANCY");
            var occupancyRow = new List<string> { "Average Occupancy" };
            foreach (var yr in years)
                occupancyRow.Add($"{yr.Average(a => a.OccupancyPercent):F1}%");
            AddRow(sd, StyleNormal, occupancyRow.ToArray());
        }
        else
        {
            AddRow(sd, StyleNormal, "No monthly actuals data available.");
        }

        if (!string.IsNullOrEmpty(deal.CalculationResult?.CashFlowProjectionsJson))
        {
            AddRow(sd, StyleNormal, "");
            AddRow(sd, StyleSectionHeader, "PROJECTED CASH FLOWS");
            RenderJsonProjections(sd, deal.CalculationResult.CashFlowProjectionsJson);
        }
    }

    private static void BuildRentRollSheet(SheetData sd, List<RentRollUnit> units)
    {
        AddRow(sd, StyleBoldHeader, "RENT ROLL");
        AddRow(sd, StyleNormal, "");

        AddRow(sd, StyleColumnHeader,
            "Unit #", "Beds", "Baths", "SF", "Market Rent", "Actual Rent",
            "Status", "Tenant", "Lease Start", "Lease End");

        decimal totalMarket = 0, totalActual = 0;
        int occupied = 0;

        foreach (var u in units)
        {
            AddRow(sd, StyleNormal,
                u.UnitNumber,
                u.Bedrooms.ToString(),
                u.Bathrooms.ToString(),
                u.SquareFeet?.ToString("N0") ?? "",
                FormatCurrency(u.MarketRent),
                u.ActualRent.HasValue ? FormatCurrency(u.ActualRent.Value) : "",
                u.Status.ToString(),
                u.TenantName ?? "",
                u.LeaseStart?.ToString("MM/dd/yyyy") ?? "",
                u.LeaseEnd?.ToString("MM/dd/yyyy") ?? "");

            totalMarket += u.MarketRent;
            totalActual += u.ActualRent ?? 0;
            if (u.Status == UnitStatus.Occupied) occupied++;
        }

        AddRow(sd, StyleNormal, "");
        AddRow(sd, StyleSectionHeader, "SUMMARY");
        AddRow(sd, StyleNormal, "Total Units", units.Count.ToString());
        AddRow(sd, StyleNormal, "Occupied", occupied.ToString());
        AddRow(sd, StyleNormal, "Vacant", (units.Count - occupied).ToString());
        AddRow(sd, StyleNormal, "Occupancy", units.Count > 0 ? $"{(decimal)occupied / units.Count * 100:F1}%" : "N/A");
        AddRow(sd, StyleNormal, "Total Market Rent (Monthly)", FormatCurrency(totalMarket));
        AddRow(sd, StyleNormal, "Total Actual Rent (Monthly)", FormatCurrency(totalActual));
        AddRow(sd, StyleNormal, "Average Market Rent", units.Count > 0 ? FormatCurrency(totalMarket / units.Count) : "N/A");
    }

    private static void BuildSourcesUsesSheet(SheetData sd, Deal deal)
    {
        var calc = deal.CalculationResult;

        AddRow(sd, StyleBoldHeader, "SOURCES & USES");
        AddRow(sd, StyleNormal, "");

        // Sources
        AddRow(sd, StyleSectionHeader, "SOURCES");
        AddRow(sd, StyleColumnHeader, "Source", "Amount", "% of Total");

        decimal totalSources = deal.CapitalStackItems.Sum(c => c.Amount);
        if (totalSources == 0 && calc?.LoanAmount is > 0)
        {
            var loan = calc.LoanAmount.Value;
            var equity = deal.PurchasePrice - loan + (deal.CapexBudget ?? 0);
            AddRow(sd, StyleNormal, "Senior Debt", FormatCurrency(loan),
                $"{(deal.PurchasePrice > 0 ? loan / deal.PurchasePrice * 100 : 0):F1}%");
            AddRow(sd, StyleNormal, "Sponsor Equity", FormatCurrency(equity > 0 ? equity : 0), "");
            totalSources = loan + (equity > 0 ? equity : 0);
        }
        else
        {
            foreach (var item in deal.CapitalStackItems.OrderBy(c => c.SortOrder))
            {
                var pct = totalSources > 0 ? item.Amount / totalSources * 100 : 0;
                AddRow(sd, StyleNormal, item.Source.ToString(), FormatCurrency(item.Amount), $"{pct:F1}%");
            }
        }

        AddRow(sd, StyleNormal, "Total Sources", FormatCurrency(totalSources), "100.0%");
        AddRow(sd, StyleNormal, "");

        // Uses
        AddRow(sd, StyleSectionHeader, "USES");
        AddRow(sd, StyleColumnHeader, "Use", "Amount", "% of Total");

        var closingCosts = deal.PurchasePrice * 0.02m;
        var totalUses = deal.PurchasePrice + closingCosts + (deal.CapexBudget ?? 0);

        AddRow(sd, StyleNormal, "Purchase Price", FormatCurrency(deal.PurchasePrice),
            totalUses > 0 ? $"{deal.PurchasePrice / totalUses * 100:F1}%" : "");
        AddRow(sd, StyleNormal, "Estimated Closing Costs", FormatCurrency(closingCosts),
            totalUses > 0 ? $"{closingCosts / totalUses * 100:F1}%" : "");
        if (deal.CapexBudget is > 0)
        {
            AddRow(sd, StyleNormal, "Capital Expenditures", FormatCurrency(deal.CapexBudget.Value),
                totalUses > 0 ? $"{deal.CapexBudget.Value / totalUses * 100:F1}%" : "");
        }

        AddRow(sd, StyleNormal, "Total Uses", FormatCurrency(totalUses), "100.0%");
    }

    private static void BuildCompsSheet(SheetData sd, List<SecuritizationComp> comps)
    {
        AddRow(sd, StyleBoldHeader, "SECURITIZATION COMPARABLES");
        AddRow(sd, StyleNormal, "");

        if (comps.Count == 0)
        {
            AddRow(sd, StyleNormal, "No comparable data available.");
            return;
        }

        AddRow(sd, StyleColumnHeader,
            "Deal Name", "State", "Units", "Loan Amount", "Rate",
            "DSCR", "LTV", "NOI", "Occupancy", "Cap Rate", "Origination");

        foreach (var c in comps)
        {
            AddRow(sd, StyleNormal,
                c.DealName ?? "",
                c.State ?? "",
                c.Units?.ToString("N0") ?? "",
                c.LoanAmount.HasValue ? FormatCurrency(c.LoanAmount.Value) : "",
                c.InterestRate.HasValue ? $"{c.InterestRate:F2}%" : "",
                c.DSCR.HasValue ? $"{c.DSCR:F2}x" : "",
                c.LTV.HasValue ? $"{c.LTV:F1}%" : "",
                c.NOI.HasValue ? FormatCurrency(c.NOI.Value) : "",
                c.Occupancy.HasValue ? $"{c.Occupancy:F1}%" : "",
                c.CapRate.HasValue ? $"{c.CapRate:F2}%" : "",
                c.OriginationDate?.ToString("MM/yyyy") ?? "");
        }

        AddRow(sd, StyleNormal, "");
        AddRow(sd, StyleSectionHeader, "MARKET SUMMARY");
        AddRow(sd, StyleColumnHeader, "Metric", "Median", "Min", "Max");

        var withDscr = comps.Where(c => c.DSCR.HasValue).Select(c => c.DSCR!.Value).ToList();
        var withLtv = comps.Where(c => c.LTV.HasValue).Select(c => c.LTV!.Value).ToList();
        var withCap = comps.Where(c => c.CapRate.HasValue).Select(c => c.CapRate!.Value).ToList();
        var withRate = comps.Where(c => c.InterestRate.HasValue).Select(c => c.InterestRate!.Value).ToList();
        var withOcc = comps.Where(c => c.Occupancy.HasValue).Select(c => c.Occupancy!.Value).ToList();

        if (withDscr.Count > 0)
            AddRow(sd, StyleNormal, "DSCR", $"{Median(withDscr):F2}x", $"{withDscr.Min():F2}x", $"{withDscr.Max():F2}x");
        if (withLtv.Count > 0)
            AddRow(sd, StyleNormal, "LTV", $"{Median(withLtv):F1}%", $"{withLtv.Min():F1}%", $"{withLtv.Max():F1}%");
        if (withCap.Count > 0)
            AddRow(sd, StyleNormal, "Cap Rate", $"{Median(withCap):F2}%", $"{withCap.Min():F2}%", $"{withCap.Max():F2}%");
        if (withRate.Count > 0)
            AddRow(sd, StyleNormal, "Interest Rate", $"{Median(withRate):F2}%", $"{withRate.Min():F2}%", $"{withRate.Max():F2}%");
        if (withOcc.Count > 0)
            AddRow(sd, StyleNormal, "Occupancy", $"{Median(withOcc):F1}%", $"{withOcc.Min():F1}%", $"{withOcc.Max():F1}%");
    }

    // ══════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════

    private static void RenderJsonProjections(SheetData sd, string json)
    {
        try
        {
            using var cfDoc = JsonDocument.Parse(json);
            var root = cfDoc.RootElement;
            if (root.ValueKind != JsonValueKind.Array) return;

            var first = root.EnumerateArray().FirstOrDefault();
            if (first.ValueKind != JsonValueKind.Object) return;

            var colNames = first.EnumerateObject().Select(p => p.Name).ToList();
            AddRow(sd, StyleColumnHeader, colNames.ToArray());
            foreach (var item in root.EnumerateArray())
            {
                var vals = colNames.Select(c =>
                    item.TryGetProperty(c, out var v) ? v.ToString() : "").ToArray();
                AddRow(sd, StyleNormal, vals);
            }
        }
        catch
        {
            // Skip malformed JSON
        }
    }

    private static void AddSheet(WorkbookPart wbPart, Sheets sheets, uint sheetId,
        string name, Action<SheetData> builder)
    {
        var wsPart = wbPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();

        builder(sheetData);

        var columns = new Columns();
        columns.Append(new Column { Min = 1, Max = 1, Width = 32, CustomWidth = true });
        columns.Append(new Column { Min = 2, Max = 4, Width = 18, CustomWidth = true });
        for (uint i = 5; i <= 16; i++)
            columns.Append(new Column { Min = i, Max = i, Width = 16, CustomWidth = true });

        wsPart.Worksheet = new Worksheet(columns, sheetData);

        var sheetViews = new SheetViews(
            new SheetView(
                new Pane { VerticalSplit = 3, TopLeftCell = "A4", ActivePane = PaneValues.BottomLeft, State = PaneStateValues.Frozen }
            )
            { WorkbookViewId = 0 });
        wsPart.Worksheet.InsertBefore(sheetViews, sheetData);

        sheets.Append(new Sheet
        {
            Id = wbPart.GetIdOfPart(wsPart),
            SheetId = sheetId,
            Name = name
        });
    }

    // Style indices
    private const uint StyleNormal = 0;
    private const uint StyleBoldHeader = 1;
    private const uint StyleSectionHeader = 2;
    private const uint StyleColumnHeader = 3;

    private static Stylesheet CreateStylesheet()
    {
        return new Stylesheet(
            new Fonts(
                new Font(new FontSize { Val = 10 }, new FontName { Val = "Calibri" }),
                new Font(new Bold(), new FontSize { Val = 14 }, new FontName { Val = "Calibri" }),
                new Font(new Bold(), new FontSize { Val = 11 }, new FontName { Val = "Calibri" }),
                new Font(new Bold(), new FontSize { Val = 10 }, new FontName { Val = "Calibri" },
                    new Color { Rgb = new HexBinaryValue("FFFFFF") })
            ),
            new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 }),
                new Fill(new PatternFill(new ForegroundColor { Rgb = new HexBinaryValue("1A2332") })
                    { PatternType = PatternValues.Solid })
            ),
            new Borders(
                new Border(
                    new LeftBorder(), new RightBorder(), new TopBorder(), new BottomBorder(), new DiagonalBorder()),
                new Border(
                    new LeftBorder(), new RightBorder(),
                    new TopBorder(),
                    new BottomBorder(new Color { Rgb = new HexBinaryValue("CCCCCC") }) { Style = BorderStyleValues.Thin },
                    new DiagonalBorder())
            ),
            new CellFormats(
                new CellFormat { FontId = 0, FillId = 0, BorderId = 0 },
                new CellFormat { FontId = 1, FillId = 0, BorderId = 0 },
                new CellFormat { FontId = 2, FillId = 0, BorderId = 1 },
                new CellFormat { FontId = 3, FillId = 2, BorderId = 0 }
            )
        );
    }

    private static void AddRow(SheetData sd, uint styleIndex, params string[] values)
    {
        var row = new Row();
        foreach (var v in values)
        {
            row.Append(new Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(v ?? ""),
                StyleIndex = styleIndex
            });
        }
        sd.Append(row);
    }

    private static void AddMonthlyLine(SheetData sd, string label, List<MonthlyActual> months,
        Func<MonthlyActual, decimal> selector, int units)
    {
        var values = new List<string> { label };
        decimal total = 0;
        foreach (var m in months)
        {
            var val = selector(m);
            total += val;
            values.Add(FormatCurrency(val));
        }
        values.Add(FormatCurrency(total));
        values.Add(units > 0 ? FormatCurrency(total / units) : "");
        AddRow(sd, StyleNormal, values.ToArray());
    }

    private static void AddLabelValue(SheetData sd, string label, string value) =>
        AddRow(sd, StyleNormal, label, value);

    private static void AddLabelCurrency(SheetData sd, string label, decimal? value) =>
        AddRow(sd, StyleNormal, label, value.HasValue ? FormatCurrency(value.Value) : "N/A");

    private static void AddLabelPercent(SheetData sd, string label, decimal? value) =>
        AddRow(sd, StyleNormal, label, value.HasValue ? $"{value:F2}%" : "N/A");

    private static void AddLabelDecimal(SheetData sd, string label, decimal? value, string format) =>
        AddRow(sd, StyleNormal, label, value.HasValue ? value.Value.ToString(format.Replace("x", "")) + "x" : "N/A");

    private static void AddUwLine(SheetData sd, string label, decimal? annual, int units, decimal? egi)
    {
        var perUnit = annual.HasValue && units > 0 ? annual.Value / units : (decimal?)null;
        var pctEgi = annual.HasValue && egi is > 0 ? annual.Value / egi.Value * 100 : (decimal?)null;

        AddRow(sd, StyleNormal,
            label,
            annual.HasValue ? FormatCurrency(annual.Value) : "",
            perUnit.HasValue ? FormatCurrency(perUnit.Value) : "",
            pctEgi.HasValue ? $"{pctEgi:F1}%" : "");
    }

    private static void AddActualsRow(SheetData sd, string label,
        List<IGrouping<int, MonthlyActual>> years, Func<IGrouping<int, MonthlyActual>, decimal> selector)
    {
        var values = new List<string> { label };
        foreach (var yr in years)
            values.Add(FormatCurrency(selector(yr)));
        AddRow(sd, StyleNormal, values.ToArray());
    }

    private static string FormatCurrency(decimal value) =>
        value < 0 ? $"(${Math.Abs(value):#,##0})" : $"${value:#,##0}";

    private static decimal? NegateNullable(decimal? val) => val.HasValue ? -val.Value : null;

    private static decimal Median(List<decimal> values)
    {
        values.Sort();
        int n = values.Count;
        if (n == 0) return 0;
        return n % 2 == 0 ? (values[n / 2 - 1] + values[n / 2]) / 2 : values[n / 2];
    }

    private static string? ExtractState(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var parts = address.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part.Length == 2 && part.All(char.IsLetter) && part == part.ToUpperInvariant())
                return part;
        }
        return null;
    }
}
