using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZSR.Underwriting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddByokApiKeyColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AffordabilityDataJson",
                table: "CalculationResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AffordabilityPercentAmi",
                table: "CalculationResults",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AffordabilityTier",
                table: "CalculationResults",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EncryptedAnthropicApiKey",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredModel",
                table: "AspNetUsers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TokenUsageRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OperationType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    InputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    OutputTokens = table.Column<int>(type: "INTEGER", nullable: false),
                    Model = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenUsageRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TokenUsageRecords_CreatedAt",
                table: "TokenUsageRecords",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TokenUsageRecords_DealId",
                table: "TokenUsageRecords",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenUsageRecords_UserId",
                table: "TokenUsageRecords",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenUsageRecords");

            migrationBuilder.DropColumn(
                name: "AffordabilityDataJson",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "AffordabilityPercentAmi",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "AffordabilityTier",
                table: "CalculationResults");

            migrationBuilder.DropColumn(
                name: "EncryptedAnthropicApiKey",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredModel",
                table: "AspNetUsers");
        }
    }
}
