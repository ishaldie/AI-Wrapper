using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZSR.Underwriting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDealUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RealAiDataSets");

            migrationBuilder.AddColumn<string>(
                name: "QuickAnalysisContent",
                table: "Deals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Deals",
                type: "TEXT",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "TosAcceptedAt",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TosVersion",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChatMessages_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FieldOverrides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FieldName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    OriginalValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    NewValue = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AppliedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldOverrides_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FieldOverrides_UploadedDocuments_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "UploadedDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    ConnectedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    DisconnectedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ActivityEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SessionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    EventType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PageUrl = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Metadata = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityEvents_UserSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "UserSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_UserId",
                table: "Deals",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_DealId",
                table: "ActivityEvents",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_EventType",
                table: "ActivityEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_OccurredAt",
                table: "ActivityEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_SessionId",
                table: "ActivityEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityEvents_UserId",
                table: "ActivityEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_CreatedAt",
                table: "ChatMessages",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatMessages_DealId",
                table: "ChatMessages",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldOverrides_DealId",
                table: "FieldOverrides",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_FieldOverrides_DocumentId",
                table: "FieldOverrides",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Deals_AspNetUsers_UserId",
                table: "Deals",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Deals_AspNetUsers_UserId",
                table: "Deals");

            migrationBuilder.DropTable(
                name: "ActivityEvents");

            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "FieldOverrides");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropIndex(
                name: "IX_Deals_UserId",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "QuickAnalysisContent",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "TosAcceptedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TosVersion",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "RealAiDataSets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Acreage = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    Amenities = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    AverageFico = table.Column<int>(type: "INTEGER", nullable: true),
                    BuildingType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    InPlaceRent = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    JobGrowth = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    MarketCapRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    MedianHhi = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    NetMigration = table.Column<int>(type: "INTEGER", nullable: true),
                    Occupancy = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    OccupancyTrendJson = table.Column<string>(type: "TEXT", nullable: true),
                    Permits = table.Column<int>(type: "INTEGER", nullable: true),
                    RentGrowth = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    RentToIncomeRatio = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    RentTrendJson = table.Column<string>(type: "TEXT", nullable: true),
                    SalesCompsJson = table.Column<string>(type: "TEXT", nullable: true),
                    SquareFootage = table.Column<int>(type: "INTEGER", nullable: true),
                    YearBuilt = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealAiDataSets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RealAiDataSets_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RealAiDataSets_DealId",
                table: "RealAiDataSets",
                column: "DealId",
                unique: true);
        }
    }
}
