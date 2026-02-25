using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZSR.Underwriting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenUsageRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "IX_TokenUsageRecords_UserId",
                table: "TokenUsageRecords",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenUsageRecords_DealId",
                table: "TokenUsageRecords",
                column: "DealId");

            migrationBuilder.CreateIndex(
                name: "IX_TokenUsageRecords_CreatedAt",
                table: "TokenUsageRecords",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TokenUsageRecords");
        }
    }
}
