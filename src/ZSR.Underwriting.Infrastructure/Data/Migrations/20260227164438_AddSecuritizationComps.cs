using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZSR.Underwriting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSecuritizationComps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SecuritizationComps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    PropertyType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    State = table.Column<string>(type: "TEXT", maxLength: 2, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    MSA = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Units = table.Column<int>(type: "INTEGER", nullable: true),
                    LoanAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    InterestRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    DSCR = table.Column<decimal>(type: "TEXT", precision: 8, scale: 4, nullable: true),
                    LTV = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    NOI = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Occupancy = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    CapRate = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: true),
                    MaturityDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OriginationDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DealName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    SecuritizationId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecuritizationComps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SecuritizationComps_OriginationDate",
                table: "SecuritizationComps",
                column: "OriginationDate");

            migrationBuilder.CreateIndex(
                name: "IX_SecuritizationComps_PropertyType",
                table: "SecuritizationComps",
                column: "PropertyType");

            migrationBuilder.CreateIndex(
                name: "IX_SecuritizationComps_Source",
                table: "SecuritizationComps",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_SecuritizationComps_State",
                table: "SecuritizationComps",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SecuritizationComps");
        }
    }
}
