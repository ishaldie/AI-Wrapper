using ZSR.Underwriting.Application.DTOs;
using ZSR.Underwriting.Application.Services;
using ZSR.Underwriting.Domain.Enums;

namespace ZSR.Underwriting.Tests.Services;

public class DocumentMatchingServiceTests
{
    private readonly DocumentMatchingService _sut = new();

    private static List<ChecklistMatchCandidate> GetStandardCandidates()
    {
        return new List<ChecklistMatchCandidate>
        {
            new(Guid.NewGuid(), "Current Months Rent Roll"),
            new(Guid.NewGuid(), "Commercial Rent Roll"),
            new(Guid.NewGuid(), "Trailing 12 Month Operating Statement"),
            new(Guid.NewGuid(), "3 Year End Operating Statements"),
            new(Guid.NewGuid(), "Real Estate Tax Bill(s)"),
            new(Guid.NewGuid(), "Title Policy"),
            new(Guid.NewGuid(), "Existing Survey"),
            new(Guid.NewGuid(), "Management Agreement"),
            new(Guid.NewGuid(), "Vendor Service Contracts"),
            new(Guid.NewGuid(), "Purchase & Sale Agreement (including all Amendments)"),
        };
    }

    // --- Exact / strong match tests ---

    [Fact]
    public void FindBestMatch_RentRollFile_MatchesRentRollItem()
    {
        var candidates = GetStandardCandidates();

        var result = _sut.FindBestMatch("rent_roll.xlsx", DocumentType.RentRoll, candidates);

        Assert.NotNull(result);
        Assert.Contains("Rent Roll", result.ItemName);
        Assert.True(result.Score > 0);
    }

    [Fact]
    public void FindBestMatch_T12File_MatchesOperatingStatement()
    {
        var candidates = GetStandardCandidates();

        var result = _sut.FindBestMatch("T12_PnL_2024.xlsx", DocumentType.T12PAndL, candidates);

        Assert.NotNull(result);
        Assert.Contains("Operating Statement", result.ItemName);
    }

    [Fact]
    public void FindBestMatch_TaxBillFile_MatchesTaxBillItem()
    {
        var candidates = GetStandardCandidates();

        var result = _sut.FindBestMatch("real_estate_tax_bill.pdf", DocumentType.RentRoll, candidates);

        Assert.NotNull(result);
        Assert.Contains("Tax Bill", result.ItemName);
    }

    [Fact]
    public void FindBestMatch_SurveyFile_MatchesSurveyItem()
    {
        var candidates = GetStandardCandidates();

        var result = _sut.FindBestMatch("existing_survey.pdf", DocumentType.Appraisal, candidates);

        Assert.NotNull(result);
        Assert.Contains("Survey", result.ItemName);
    }

    // --- DocumentType helps resolve ambiguity ---

    [Fact]
    public void FindBestMatch_DocumentType_HelpsNarrowMatch()
    {
        var candidates = GetStandardCandidates();

        // "rent_roll" filename with RentRoll type → should match rent roll item
        var result = _sut.FindBestMatch("rent_roll_jan2024.xlsx", DocumentType.RentRoll, candidates);

        Assert.NotNull(result);
        Assert.Contains("Rent Roll", result.ItemName);
    }

    // --- No match tests ---

    [Fact]
    public void FindBestMatch_UnrelatedFile_ReturnsNull()
    {
        var candidates = GetStandardCandidates();

        var result = _sut.FindBestMatch("photo_001.jpg", DocumentType.Appraisal, candidates);

        Assert.Null(result);
    }

    [Fact]
    public void FindBestMatch_EmptyCandidates_ReturnsNull()
    {
        var result = _sut.FindBestMatch("rent_roll.xlsx", DocumentType.RentRoll, new List<ChecklistMatchCandidate>());

        Assert.Null(result);
    }

    [Fact]
    public void FindBestMatch_EmptyFileName_ReturnsNull()
    {
        var candidates = GetStandardCandidates();

        var result = _sut.FindBestMatch("", DocumentType.RentRoll, candidates);

        Assert.Null(result);
    }

    // --- Partial match / fuzzy tests ---

    [Fact]
    public void FindBestMatch_CamelCaseFilename_Matches()
    {
        var candidates = GetStandardCandidates();

        var result = _sut.FindBestMatch("RentRoll.xlsx", DocumentType.RentRoll, candidates);

        Assert.NotNull(result);
        Assert.Contains("Rent Roll", result.ItemName);
    }

    [Fact]
    public void FindBestMatch_HyphenatedFilename_Matches()
    {
        var candidates = GetStandardCandidates();

        var result = _sut.FindBestMatch("rent-roll-current.xlsx", DocumentType.RentRoll, candidates);

        Assert.NotNull(result);
        Assert.Contains("Rent Roll", result.ItemName);
    }

    [Fact]
    public void FindBestMatch_ManagementAgreement_Matches()
    {
        var candidates = GetStandardCandidates();

        var result = _sut.FindBestMatch("management_agreement_signed.pdf", DocumentType.OfferingMemorandum, candidates);

        Assert.NotNull(result);
        Assert.Equal("Management Agreement", result.ItemName);
    }

    // --- Score ordering ---

    [Fact]
    public void FindBestMatch_ReturnsHighestScoringMatch()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var candidates = new List<ChecklistMatchCandidate>
        {
            new(id1, "Rent Roll"),
            new(id2, "Current Months Rent Roll"),
        };

        // "current_rent_roll" should match "Current Months Rent Roll" better
        var result = _sut.FindBestMatch("current_rent_roll.xlsx", DocumentType.RentRoll, candidates);

        Assert.NotNull(result);
        Assert.Equal(id2, result.ChecklistItemId);
    }

    // --- Purchase & Sale Agreement ---

    [Fact]
    public void FindBestMatch_PSA_MatchesPurchaseAndSale()
    {
        var candidates = GetStandardCandidates();

        var result = _sut.FindBestMatch("purchase_sale_agreement.pdf", DocumentType.OfferingMemorandum, candidates);

        Assert.NotNull(result);
        Assert.Contains("Purchase", result.ItemName);
    }

    // --- Loan Term Sheet via DocumentType ---

    [Fact]
    public void FindBestMatch_LoanTermSheet_MatchesViaDocType()
    {
        var loanId = Guid.NewGuid();
        var candidates = new List<ChecklistMatchCandidate>
        {
            new(Guid.NewGuid(), "Current Months Rent Roll"),
            new(loanId, "Loan Term Sheet Summary"),
        };

        var result = _sut.FindBestMatch("loan_terms.xlsx", DocumentType.LoanTermSheet, candidates);

        Assert.NotNull(result);
        Assert.Equal(loanId, result.ChecklistItemId);
    }
}
