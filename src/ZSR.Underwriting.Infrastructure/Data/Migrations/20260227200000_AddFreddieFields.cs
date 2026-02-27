using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZSR.Underwriting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFreddieFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FreddieProductType",
                table: "Deals",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FreddieComplianceJson",
                table: "CalculationResults",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FreddieProductType",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "FreddieComplianceJson",
                table: "CalculationResults");
        }
    }
}
