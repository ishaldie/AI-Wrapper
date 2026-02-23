using ZSR.Underwriting.Domain.Entities;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Infrastructure.Data;

public static class ChecklistTemplateSeed
{
    public static List<ChecklistTemplate> GetTemplates()
    {
        var templates = new List<ChecklistTemplate>();
        int sort = 0;

        // Section 1: Historical & Proforma Property Operations
        var s1 = "Historical & Proforma Property Operations";
        templates.Add(T(s1, 1, ++sort, "Current Months Rent Roll", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "Commercial Rent Roll", ExecutionType.All, "Commercial"));
        templates.Add(T(s1, 1, ++sort, "Trailing 12 Month Operating Statement", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "3 Year End Operating Statements", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "Collection & Vacancy History Form", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "Current Aged Receivables / Tenant Delinquency Report", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "At least 3 months of bank statements confirming rent deposits", ExecutionType.FannieMae, "All"));
        templates.Add(T(s1, 1, ++sort, "Borrower's Year-1 Operating Budget", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "Payroll & Benefits Schedule", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "CapEx Detail - Historical (3 Years)", ExecutionType.FreddieMac, "All"));
        templates.Add(T(s1, 1, ++sort, "CapEx Detail - Historical (3 Years) + invoices for significant projects", ExecutionType.FannieMae, "All"));
        templates.Add(T(s1, 1, ++sort, "CapEx Detail - Planned (12 months)", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "Real Estate Tax Bill(s)", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "Tax Abatement / Exemption / PILOT Documentation", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "Borrower Expense Comparables", ExecutionType.All, "All"));
        templates.Add(T(s1, 1, ++sort, "Freddie Mac Form 1112 (Borrower Blanket Certification)", ExecutionType.FreddieMac, "All"));

        // Section 2: Property Title & Survey
        var s2 = "Property Title & Survey";
        templates.Add(T(s2, 2, ++sort, "Existing Survey", ExecutionType.All, "All"));
        templates.Add(T(s2, 2, ++sort, "Title Policy", ExecutionType.All, "All"));
        templates.Add(T(s2, 2, ++sort, "All Existing Easements", ExecutionType.All, "All"));

        // Section 3: Zoning Status and Building Code Compliance
        var s3 = "Zoning Status and Building Code Compliance";
        templates.Add(T(s3, 3, ++sort, "Certificates of Occupancy (or Final Building Permit)", ExecutionType.All, "All"));

        // Section 4: Termite Inspection Documentation
        var s4 = "Termite Inspection Documentation";
        templates.Add(T(s4, 4, ++sort, "Wood Destroying Insect Inspection Report or Termite Bond", ExecutionType.FreddieMac, "All"));
        templates.Add(T(s4, 4, ++sort, "Termite Report", ExecutionType.FannieMae, "All"));

        // Section 5: Miscellaneous Property Due Diligence
        var s5 = "Miscellaneous Property Due Diligence";
        templates.Add(T(s5, 5, ++sort, "Vendor Service Contracts", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Ground Lease (if applicable)", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Master Lease (if applicable)", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Commercial Lease(s) (if applicable)", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Cooperative Agreement (if applicable)", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Condominium Agreement (if applicable)", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Reciprocal Use Agreement (if applicable)", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Registration of Rental Units (rent control/stabilization)", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "LURA, rent schedule, approval docs, and compliance confirmation", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "LIHTC, rent schedule, approval docs, and compliance confirmation", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Regulatory Agreement (RA), rent schedule, approval docs", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Summary of soft funding sources, project-based rental assistance / HAP contracts, and subordinations", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Summary of rent restrictions that survive foreclosure", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Preferred equity, subordinate or mezzanine debt", ExecutionType.All, "Pref Equity, Sub, Mezz"));
        templates.Add(T(s5, 5, ++sort, "Equity Conflict of Interest Statement", ExecutionType.FreddieMac, "Equity Conflict of Interest"));
        templates.Add(T(s5, 5, ++sort, "Shari'ah loan structure (if applicable)", ExecutionType.All, "All"));
        templates.Add(T(s5, 5, ++sort, "Shtar Iska (if applicable)", ExecutionType.All, "Meridian"));

        // Section 6: Inspection Due Diligence
        var s6 = "Inspection Due Diligence";
        templates.Add(T(s6, 6, ++sort, "Site Inspection Confirmation", ExecutionType.All, "All"));
        templates.Add(T(s6, 6, ++sort, "Lender Inspection / Questionnaire / Manager Interview", ExecutionType.All, "All"));
        templates.Add(T(s6, 6, ++sort, "Current Market Survey", ExecutionType.All, "All"));
        templates.Add(T(s6, 6, ++sort, "Current Box Score Report (vacant units + days vacant)", ExecutionType.All, "All"));
        templates.Add(T(s6, 6, ++sort, "Property Brochure", ExecutionType.All, "All"));
        templates.Add(T(s6, 6, ++sort, "Property Site Map", ExecutionType.All, "All"));
        templates.Add(T(s6, 6, ++sort, "Freddie Mac Inspection (if applicable)", ExecutionType.FreddieMac, "All"));
        templates.Add(T(s6, 6, ++sort, "Lease Audit Documentation (Fannie Mae)", ExecutionType.FannieMae, "All"));
        templates.Add(T(s6, 6, ++sort, "Lease Audit Documentation (3rd party review)", ExecutionType.FannieMae, "All"));
        templates.Add(T(s6, 6, ++sort, "Lease Audit Documentation (Freddie Mac)", ExecutionType.FreddieMac, "All"));
        templates.Add(T(s6, 6, ++sort, "Additional Lease Audit - Tenant Income Certification (HUD 50059)", ExecutionType.All, "HAP"));
        templates.Add(T(s6, 6, ++sort, "Additional Lease Audit - Tenant Income Certification Form", ExecutionType.All, "LIHTC"));
        templates.Add(T(s6, 6, ++sort, "Sample Residential Lease (including addendums)", ExecutionType.FreddieMac, "All"));

        // Section 7: Insurance Policy Review Due Diligence
        var s7 = "Insurance Policy Review Due Diligence";
        templates.Add(T(s7, 7, ++sort, "Final Insurance Review - Agency compliant policy", ExecutionType.All, "All"));
        templates.Add(T(s7, 7, ++sort, "Borrower Liability Insurance Acknowledgement", ExecutionType.All, "All"));
        templates.Add(T(s7, 7, ++sort, "Initial Insurance Questions (payroll employees? autos?)", ExecutionType.All, "All"));
        templates.Add(T(s7, 7, ++sort, "Insurance ACORD 25 & 28", ExecutionType.All, "Refinance"));

        // Section 8: Property Management
        var s8 = "Property Management";
        templates.Add(T(s8, 8, ++sort, "Management Company Resume (Fannie Mae)", ExecutionType.FannieMae, "All"));
        templates.Add(T(s8, 8, ++sort, "Management Company Resume (Freddie Mac)", ExecutionType.FreddieMac, "All"));
        templates.Add(T(s8, 8, ++sort, "Management Agreement", ExecutionType.All, "All"));

        // Section 9: Refinance Transaction
        var s9 = "Refinance Transaction";
        templates.Add(T(s9, 9, ++sort, "Preliminary Sources & Uses Form", ExecutionType.All, "Refinance"));
        templates.Add(T(s9, 9, ++sort, "Payoff Letter", ExecutionType.All, "Refinance"));
        templates.Add(T(s9, 9, ++sort, "Purchase & Sale Agreement (if purchased within 24 months)", ExecutionType.FannieMae, "Refinance"));

        // Section 10: Acquisition Transaction
        var s10 = "Acquisition Transaction";
        templates.Add(T(s10, 10, ++sort, "Preliminary Sources & Uses Form", ExecutionType.All, "Acquisition"));
        templates.Add(T(s10, 10, ++sort, "Purchase & Sale Agreement (including all Amendments)", ExecutionType.All, "Acquisition"));

        // Section 11: HAP Related Documents
        var s11 = "HAP Related Documents";
        templates.Add(T(s11, 11, ++sort, "Section 8 HAP Contract (original + all renewals)", ExecutionType.All, "HAP"));
        templates.Add(T(s11, 11, ++sort, "Assignment of HAP Contract to Current Owner", ExecutionType.All, "HAP"));
        templates.Add(T(s11, 11, ++sort, "Consent to the Assignment of HAP Contract to Lender", ExecutionType.All, "HAP"));
        templates.Add(T(s11, 11, ++sort, "Most Recent Real Estate Assessment Center Inspection Report", ExecutionType.All, "HAP"));
        templates.Add(T(s11, 11, ++sort, "Most Current Max Rent Schedule", ExecutionType.All, "HAP"));

        // Section 12: Tax Credit Related Documents
        var s12 = "Tax Credit Related Documents";
        templates.Add(T(s12, 12, ++sort, "Land Use Restriction Agreement", ExecutionType.All, "LIHTC"));
        templates.Add(T(s12, 12, ++sort, "LIHTC Rent Schedule, including Utility Allowance", ExecutionType.All, "LIHTC"));
        templates.Add(T(s12, 12, ++sort, "LIHTC Allocation & Certification Documentation (8609s)", ExecutionType.All, "LIHTC"));
        templates.Add(T(s12, 12, ++sort, "LIHTC Application to Housing Agency or Approval", ExecutionType.All, "LIHTC"));
        templates.Add(T(s12, 12, ++sort, "Annual Monitoring Report and Approval from Housing Agency", ExecutionType.All, "LIHTC"));

        // Section 13: New Construction / Substantial Rehab
        var s13 = "New Construction / Substantial Rehab";
        templates.Add(T(s13, 13, ++sort, "Construction Cost Breakdown & Timeline", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Plans and Specifications", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Construction Phase Sources & Uses", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Construction Loan Commitment Letter and Loan Documents", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Financing Timeline", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Project Description", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Site Plan / Elevations / Unit Layout / Pictures", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Development Team - background and experience", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Proposed Rent Schedule", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Proforma Net Operating Income (15-year cash flow projection)", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Building Permits / Site Plan Approval", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Entitlements / Zoning / Inclusionary or Affordable", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Fully executed Construction Contract (AIA form)", ExecutionType.All, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Architect's Errors and Omission Insurance Coverage", ExecutionType.FreddieMac, "New Construction / Rehab"));
        templates.Add(T(s13, 13, ++sort, "Architect Agreement and Assignment to Partnership", ExecutionType.FreddieMac, "New Construction / Rehab"));

        // Section 14: Student Property Due Diligence
        var s14 = "Student Property Due Diligence";
        templates.Add(T(s14, 14, ++sort, "Form 1120 (Student Housing Questionnaire)", ExecutionType.FreddieMac, "Student"));

        // Section 15: Seniors Housing Due Diligence
        var s15 = "Seniors Housing Due Diligence";
        templates.Add(T(s15, 15, ++sort, "Seniors Housing Licenses and Certificates", ExecutionType.All, "Seniors Housing"));
        templates.Add(T(s15, 15, ++sort, "Seniors Housing Payroll Schedule", ExecutionType.All, "Seniors Housing"));
        templates.Add(T(s15, 15, ++sort, "Seniors Housing Agreements and Contracts", ExecutionType.All, "Seniors Housing"));
        templates.Add(T(s15, 15, ++sort, "Seniors Housing List of FF&E and Motor Vehicles", ExecutionType.All, "Seniors Housing"));
        templates.Add(T(s15, 15, ++sort, "Seniors Housing kitchen/food/liquor/elevator/health/fire/safety/sprinkler licenses", ExecutionType.FreddieMac, "Seniors Housing"));
        templates.Add(T(s15, 15, ++sort, "Seniors Housing Covid Tracking Form", ExecutionType.FreddieMac, "Seniors Housing"));
        templates.Add(T(s15, 15, ++sort, "Seniors Housing Covid Borrower Certification Form", ExecutionType.FreddieMac, "Seniors Housing"));
        templates.Add(T(s15, 15, ++sort, "Seniors Housing Contract Borrower Certification Form", ExecutionType.FreddieMac, "Seniors Housing"));
        templates.Add(T(s15, 15, ++sort, "Seniors Housing FF&E Borrower Certification Form", ExecutionType.FreddieMac, "Seniors Housing"));

        // Section 16: Green Transactions
        var s16 = "Green Transactions";
        templates.Add(T(s16, 16, ++sort, "Green Financing: 12 Months of Owner Paid Utility Bills", ExecutionType.All, "Green"));
        templates.Add(T(s16, 16, ++sort, "Green Financing: Copies of any Green Certifications", ExecutionType.All, "Green"));

        // Section 17: Deal Specific Documentation
        var s17 = "Deal Specific Documentation";
        for (int i = 1; i <= 10; i++)
        {
            templates.Add(T(s17, 17, ++sort, $"Additional Item {i}", ExecutionType.All, "Deal Specific"));
        }

        return templates;
    }

    private static ChecklistTemplate T(
        string section, int sectionOrder,
        int sortOrder, string itemName,
        ExecutionType executionType, string transactionType)
    {
        return new ChecklistTemplate(section, sectionOrder, itemName, sortOrder, executionType, transactionType);
    }
}
