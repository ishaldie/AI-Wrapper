using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZSR.Underwriting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddActualPurchasePrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ActualPurchasePrice",
                table: "Deals",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ClosedDate",
                table: "Deals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PortfolioId",
                table: "Deals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AssetReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: true),
                    Quarter = table.Column<int>(type: "INTEGER", nullable: true),
                    PerformanceSummary = table.Column<string>(type: "TEXT", nullable: true),
                    VarianceAnalysis = table.Column<string>(type: "TEXT", nullable: true),
                    MarketUpdate = table.Column<string>(type: "TEXT", nullable: true),
                    OutlookAndRecommendations = table.Column<string>(type: "TEXT", nullable: true),
                    MetricsSnapshotJson = table.Column<string>(type: "TEXT", nullable: true),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetReports_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CapExProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    BudgetAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ActualSpend = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    StartDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TargetCompletionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActualCompletionDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UnitsAffected = table.Column<int>(type: "INTEGER", nullable: true),
                    ExpectedRentIncrease = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapExProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapExProjects_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClosingCostItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EstimatedAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ActualAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    IsPaid = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClosingCostItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClosingCostItems_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContractTimelines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LoiDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    PsaExecutedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    InspectionDeadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FinancingContingencyDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    AppraisalDeadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TitleDeadline = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosingDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ActualClosingDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EarnestMoneyDeposit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    AdditionalDeposit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    LenderName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    TitleCompany = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractTimelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractTimelines_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispositionAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BrokerOpinionOfValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    CurrentMarketCapRate = table.Column<decimal>(type: "TEXT", precision: 8, scale: 4, nullable: true),
                    TrailingTwelveMonthNoi = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ImpliedValue = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    HoldScenarioJson = table.Column<string>(type: "TEXT", nullable: true),
                    SellScenarioJson = table.Column<string>(type: "TEXT", nullable: true),
                    RefinanceScenarioJson = table.Column<string>(type: "TEXT", nullable: true),
                    Recommendation = table.Column<string>(type: "TEXT", nullable: true),
                    AnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispositionAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispositionAnalyses_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyActuals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Year = table.Column<int>(type: "INTEGER", nullable: false),
                    Month = table.Column<int>(type: "INTEGER", nullable: false),
                    GrossRentalIncome = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    VacancyLoss = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    OtherIncome = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    EffectiveGrossIncome = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PropertyTaxes = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Insurance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Utilities = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Repairs = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Management = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Payroll = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Marketing = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Administrative = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    OtherExpenses = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalOperatingExpenses = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    NetOperatingIncome = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DebtService = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CapitalExpenditures = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CashFlow = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    OccupiedUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalUnits = table.Column<int>(type: "INTEGER", nullable: false),
                    OccupancyPercent = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyActuals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyActuals_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Portfolios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Strategy = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    VintageYear = table.Column<int>(type: "INTEGER", nullable: true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Portfolios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RentRollUnits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnitNumber = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Bedrooms = table.Column<int>(type: "INTEGER", nullable: false),
                    Bathrooms = table.Column<int>(type: "INTEGER", nullable: false),
                    SquareFeet = table.Column<int>(type: "INTEGER", nullable: true),
                    MarketRent = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ActualRent = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TenantName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LeaseStart = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LeaseEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SecurityDeposit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    MonthlyCharges = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RentRollUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RentRollUnits_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CapExLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CapExProjectId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Vendor = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DateIncurred = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapExLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapExLineItems_CapExProjects_CapExProjectId",
                        column: x => x.CapExProjectId,
                        principalTable: "CapExProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_PortfolioId",
                table: "Deals",
                column: "PortfolioId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetReports_DealId",
                table: "AssetReports",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_CapExLineItems_CapExProjectId",
                table: "CapExLineItems",
                column: "CapExProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CapExProjects_DealId",
                table: "CapExProjects",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_ClosingCostItems_DealId",
                table: "ClosingCostItems",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_ContractTimelines_DealId",
                table: "ContractTimelines",
                column: "DealId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DispositionAnalyses_DealId",
                table: "DispositionAnalyses",
                column: "DealId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyActuals_DealId_Year_Month",
                table: "MonthlyActuals",
                columns: new[] { "DealId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Portfolios_UserId",
                table: "Portfolios",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RentRollUnits_DealId",
                table: "RentRollUnits",
                column: "DealId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deals_Portfolios_PortfolioId",
                table: "Deals",
                column: "PortfolioId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deals_Portfolios_PortfolioId",
                table: "Deals");

            migrationBuilder.DropTable(
                name: "AssetReports");

            migrationBuilder.DropTable(
                name: "CapExLineItems");

            migrationBuilder.DropTable(
                name: "ClosingCostItems");

            migrationBuilder.DropTable(
                name: "ContractTimelines");

            migrationBuilder.DropTable(
                name: "DispositionAnalyses");

            migrationBuilder.DropTable(
                name: "MonthlyActuals");

            migrationBuilder.DropTable(
                name: "Portfolios");

            migrationBuilder.DropTable(
                name: "RentRollUnits");

            migrationBuilder.DropTable(
                name: "CapExProjects");

            migrationBuilder.DropIndex(
                name: "IX_Deals_PortfolioId",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ActualPurchasePrice",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ClosedDate",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "PortfolioId",
                table: "Deals");
        }
    }
}
