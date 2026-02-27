using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZSR.Underwriting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFannieProductType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlBeds",
                table: "Deals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AverageDailyRate",
                table: "Deals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AverageLengthOfStayMonths",
                table: "Deals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CmsData",
                table: "Deals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FannieProductType",
                table: "Deals",
                type: "TEXT",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LicenseType",
                table: "Deals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LicensedBeds",
                table: "Deals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MedicaidPct",
                table: "Deals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MedicarePct",
                table: "Deals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemoryCareBeds",
                table: "Deals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PrivatePayPct",
                table: "Deals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PropertyType",
                table: "Deals",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "RevenuePerOccupiedBed",
                table: "Deals",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SnfBeds",
                table: "Deals",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "StaffingRatio",
                table: "Deals",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlBeds",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "AverageDailyRate",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "AverageLengthOfStayMonths",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "CmsData",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "FannieProductType",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LicenseType",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "LicensedBeds",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "MedicaidPct",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "MedicarePct",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "MemoryCareBeds",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "PrivatePayPct",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "PropertyType",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "RevenuePerOccupiedBed",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "SnfBeds",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "StaffingRatio",
                table: "Deals");
        }
    }
}
