using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZSR.Underwriting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCoreEntitiesAndRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalculationResult_Deals_DealId",
                table: "CalculationResult");

            migrationBuilder.DropForeignKey(
                name: "FK_Property_Deals_DealId",
                table: "Property");

            migrationBuilder.DropForeignKey(
                name: "FK_RealAiData_Deals_DealId",
                table: "RealAiData");

            migrationBuilder.DropForeignKey(
                name: "FK_UnderwritingInput_Deals_DealId",
                table: "UnderwritingInput");

            migrationBuilder.DropForeignKey(
                name: "FK_UnderwritingReport_Deals_DealId",
                table: "UnderwritingReport");

            migrationBuilder.DropForeignKey(
                name: "FK_UploadedDocument_Deals_DealId",
                table: "UploadedDocument");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UploadedDocument",
                table: "UploadedDocument");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnderwritingReport",
                table: "UnderwritingReport");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnderwritingInput",
                table: "UnderwritingInput");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RealAiData",
                table: "RealAiData");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Property",
                table: "Property");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CalculationResult",
                table: "CalculationResult");

            migrationBuilder.RenameTable(
                name: "UploadedDocument",
                newName: "UploadedDocuments");

            migrationBuilder.RenameTable(
                name: "UnderwritingReport",
                newName: "UnderwritingReports");

            migrationBuilder.RenameTable(
                name: "UnderwritingInput",
                newName: "UnderwritingInputs");

            migrationBuilder.RenameTable(
                name: "RealAiData",
                newName: "RealAiDataSets");

            migrationBuilder.RenameTable(
                name: "Property",
                newName: "Properties");

            migrationBuilder.RenameTable(
                name: "CalculationResult",
                newName: "CalculationResults");

            migrationBuilder.RenameIndex(
                name: "IX_UploadedDocument_DealId",
                table: "UploadedDocuments",
                newName: "IX_UploadedDocuments_DealId");

            migrationBuilder.RenameIndex(
                name: "IX_UnderwritingReport_DealId",
                table: "UnderwritingReports",
                newName: "IX_UnderwritingReports_DealId");

            migrationBuilder.RenameIndex(
                name: "IX_UnderwritingInput_DealId",
                table: "UnderwritingInputs",
                newName: "IX_UnderwritingInputs_DealId");

            migrationBuilder.RenameIndex(
                name: "IX_RealAiData_DealId",
                table: "RealAiDataSets",
                newName: "IX_RealAiDataSets_DealId");

            migrationBuilder.RenameIndex(
                name: "IX_Property_DealId",
                table: "Properties",
                newName: "IX_Properties_DealId");

            migrationBuilder.RenameIndex(
                name: "IX_CalculationResult_DealId",
                table: "CalculationResults",
                newName: "IX_CalculationResults_DealId");

            migrationBuilder.AddColumn<string>(
                name: "DocumentType",
                table: "UploadedDocuments",
                type: "TEXT",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "UploadedDocuments",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "UploadedDocuments",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "StoredPath",
                table: "UploadedDocuments",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UploadedAt",
                table: "UploadedDocuments",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "DebtAnalysis",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecisionRationale",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExecutiveSummary",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExpenseAnalysis",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FinancialAnalysis",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedAt",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "InvestmentThesis",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGoDecision",
                table: "UnderwritingReports",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarketAnalysis",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PropertyOverview",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentAnalysis",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReturnAnalysis",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RiskAssessment",
                table: "UnderwritingReports",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AmortizationYears",
                table: "UnderwritingInputs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CapexBudget",
                table: "UnderwritingInputs",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HoldPeriodYears",
                table: "UnderwritingInputs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInterestOnly",
                table: "UnderwritingInputs",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "LoanLtv",
                table: "UnderwritingInputs",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LoanRate",
                table: "UnderwritingInputs",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LoanTermYears",
                table: "UnderwritingInputs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PurchasePrice",
                table: "UnderwritingInputs",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RentRollSummary",
                table: "UnderwritingInputs",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "T12Summary",
                table: "UnderwritingInputs",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetOccupancy",
                table: "UnderwritingInputs",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValueAddPlans",
                table: "UnderwritingInputs",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Acreage",
                table: "RealAiDataSets",
                type: "TEXT",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Amenities",
                table: "RealAiDataSets",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AverageFico",
                table: "RealAiDataSets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingType",
                table: "RealAiDataSets",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FetchedAt",
                table: "RealAiDataSets",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<decimal>(
                name: "InPlaceRent",
                table: "RealAiDataSets",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "JobGrowth",
                table: "RealAiDataSets",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MarketCapRate",
                table: "RealAiDataSets",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MedianHhi",
                table: "RealAiDataSets",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NetMigration",
                table: "RealAiDataSets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Occupancy",
                table: "RealAiDataSets",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OccupancyTrendJson",
                table: "RealAiDataSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Permits",
                table: "RealAiDataSets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentGrowth",
                table: "RealAiDataSets",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RentToIncomeRatio",
                table: "RealAiDataSets",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RentTrendJson",
                table: "RealAiDataSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalesCompsJson",
                table: "RealAiDataSets",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SquareFootage",
                table: "RealAiDataSets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearBuilt",
                table: "RealAiDataSets",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Acreage",
                table: "Properties",
                type: "TEXT",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Properties",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BuildingType",
                table: "Properties",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SquareFootage",
                table: "Properties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitCount",
                table: "Properties",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "YearBuilt",
                table: "Properties",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualDebtService",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalculatedAt",
                table: "CalculationResults",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "CashFlowProjectionsJson",
                table: "CalculationResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CashOnCashReturn",
                table: "CalculationResults",
                type: "TEXT",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DebtServiceCoverageRatio",
                table: "CalculationResults",
                type: "TEXT",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EffectiveGrossIncome",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EquityMultiple",
                table: "CalculationResults",
                type: "TEXT",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExitCapRate",
                table: "CalculationResults",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ExitValue",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GoingInCapRate",
                table: "CalculationResults",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossPotentialRent",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "InternalRateOfReturn",
                table: "CalculationResults",
                type: "TEXT",
                precision: 8,
                scale: 4,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LoanAmount",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetOperatingIncome",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NoiMargin",
                table: "CalculationResults",
                type: "TEXT",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OperatingExpenses",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherIncome",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PricePerUnit",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SensitivityAnalysisJson",
                table: "CalculationResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalProfit",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VacancyLoss",
                table: "CalculationResults",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UploadedDocuments",
                table: "UploadedDocuments",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnderwritingReports",
                table: "UnderwritingReports",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnderwritingInputs",
                table: "UnderwritingInputs",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RealAiDataSets",
                table: "RealAiDataSets",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Properties",
                table: "Properties",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CalculationResults",
                table: "CalculationResults",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_CreatedAt",
                table: "Deals",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Deals_Status",
                table: "Deals",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_CalculationResults_Deals_DealId",
                table: "CalculationResults",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Deals_DealId",
                table: "Properties",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RealAiDataSets_Deals_DealId",
                table: "RealAiDataSets",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnderwritingInputs_Deals_DealId",
                table: "UnderwritingInputs",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnderwritingReports_Deals_DealId",
                table: "UnderwritingReports",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UploadedDocuments_Deals_DealId",
                table: "UploadedDocuments",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CalculationResults_Deals_DealId",
                table: "CalculationResults");

            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Deals_DealId",
                table: "Properties");

            migrationBuilder.DropForeignKey(
                name: "FK_RealAiDataSets_Deals_DealId",
                table: "RealAiDataSets");

            migrationBuilder.DropForeignKey(
                name: "FK_UnderwritingInputs_Deals_DealId",
                table: "UnderwritingInputs");

            migrationBuilder.DropForeignKey(
                name: "FK_UnderwritingReports_Deals_DealId",
                table: "UnderwritingReports");

            migrationBuilder.DropForeignKey(
                name: "FK_UploadedDocuments_Deals_DealId",
                table: "UploadedDocuments");

            migrationBuilder.DropIndex(
                name: "IX_Deals_CreatedAt",
                table: "Deals");

            migrationBuilder.DropIndex(
                name: "IX_Deals_Status",
                table: "Deals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UploadedDocuments",
                table: "UploadedDocuments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnderwritingReports",
                table: "UnderwritingReports");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnderwritingInputs",
                table: "UnderwritingInputs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RealAiDataSets",
                table: "RealAiDataSets");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Properties",
                table: "Properties");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CalculationResults",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "DocumentType",
                table: "UploadedDocuments");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "UploadedDocuments");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "UploadedDocuments");

            migrationBuilder.DropColumn(
                name: "StoredPath",
                table: "UploadedDocuments");

            migrationBuilder.DropColumn(
                name: "UploadedAt",
                table: "UploadedDocuments");

            migrationBuilder.DropColumn(
                name: "DebtAnalysis",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "DecisionRationale",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "ExecutiveSummary",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "ExpenseAnalysis",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "FinancialAnalysis",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "GeneratedAt",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "InvestmentThesis",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "IsGoDecision",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "MarketAnalysis",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "PropertyOverview",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "RentAnalysis",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "ReturnAnalysis",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "RiskAssessment",
                table: "UnderwritingReports");

            migrationBuilder.DropColumn(
                name: "AmortizationYears",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "CapexBudget",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "HoldPeriodYears",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "IsInterestOnly",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "LoanLtv",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "LoanRate",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "LoanTermYears",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "PurchasePrice",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "RentRollSummary",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "T12Summary",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "TargetOccupancy",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "ValueAddPlans",
                table: "UnderwritingInputs");

            migrationBuilder.DropColumn(
                name: "Acreage",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "Amenities",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "AverageFico",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "BuildingType",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "FetchedAt",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "InPlaceRent",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "JobGrowth",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "MarketCapRate",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "MedianHhi",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "NetMigration",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "Occupancy",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "OccupancyTrendJson",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "Permits",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "RentGrowth",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "RentToIncomeRatio",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "RentTrendJson",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "SalesCompsJson",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "SquareFootage",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "YearBuilt",
                table: "RealAiDataSets");

            migrationBuilder.DropColumn(
                name: "Acreage",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "BuildingType",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "SquareFootage",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "UnitCount",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "YearBuilt",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "AnnualDebtService",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "CalculatedAt",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "CashFlowProjectionsJson",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "CashOnCashReturn",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "DebtServiceCoverageRatio",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "EffectiveGrossIncome",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "EquityMultiple",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "ExitCapRate",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "ExitValue",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "GoingInCapRate",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "GrossPotentialRent",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "InternalRateOfReturn",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "LoanAmount",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "NetOperatingIncome",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "NoiMargin",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "OperatingExpenses",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "OtherIncome",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "PricePerUnit",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "SensitivityAnalysisJson",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "TotalProfit",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "VacancyLoss",
                table: "CalculationResults");

            migrationBuilder.RenameTable(
                name: "UploadedDocuments",
                newName: "UploadedDocument");

            migrationBuilder.RenameTable(
                name: "UnderwritingReports",
                newName: "UnderwritingReport");

            migrationBuilder.RenameTable(
                name: "UnderwritingInputs",
                newName: "UnderwritingInput");

            migrationBuilder.RenameTable(
                name: "RealAiDataSets",
                newName: "RealAiData");

            migrationBuilder.RenameTable(
                name: "Properties",
                newName: "Property");

            migrationBuilder.RenameTable(
                name: "CalculationResults",
                newName: "CalculationResult");

            migrationBuilder.RenameIndex(
                name: "IX_UploadedDocuments_DealId",
                table: "UploadedDocument",
                newName: "IX_UploadedDocument_DealId");

            migrationBuilder.RenameIndex(
                name: "IX_UnderwritingReports_DealId",
                table: "UnderwritingReport",
                newName: "IX_UnderwritingReport_DealId");

            migrationBuilder.RenameIndex(
                name: "IX_UnderwritingInputs_DealId",
                table: "UnderwritingInput",
                newName: "IX_UnderwritingInput_DealId");

            migrationBuilder.RenameIndex(
                name: "IX_RealAiDataSets_DealId",
                table: "RealAiData",
                newName: "IX_RealAiData_DealId");

            migrationBuilder.RenameIndex(
                name: "IX_Properties_DealId",
                table: "Property",
                newName: "IX_Property_DealId");

            migrationBuilder.RenameIndex(
                name: "IX_CalculationResults_DealId",
                table: "CalculationResult",
                newName: "IX_CalculationResult_DealId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UploadedDocument",
                table: "UploadedDocument",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnderwritingReport",
                table: "UnderwritingReport",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnderwritingInput",
                table: "UnderwritingInput",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RealAiData",
                table: "RealAiData",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Property",
                table: "Property",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CalculationResult",
                table: "CalculationResult",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CalculationResult_Deals_DealId",
                table: "CalculationResult",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Property_Deals_DealId",
                table: "Property",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RealAiData_Deals_DealId",
                table: "RealAiData",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnderwritingInput_Deals_DealId",
                table: "UnderwritingInput",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnderwritingReport_Deals_DealId",
                table: "UnderwritingReport",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UploadedDocument_Deals_DealId",
                table: "UploadedDocument",
                column: "DealId",
                principalTable: "Deals",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
