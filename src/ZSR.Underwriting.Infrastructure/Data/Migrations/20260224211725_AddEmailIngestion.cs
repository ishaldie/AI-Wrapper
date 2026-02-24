using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZSR.Underwriting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailIngestion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShortCode",
                table: "Deals",
                type: "TEXT",
                maxLength: 8,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "EmailIngestionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DealId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SenderEmail = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AttachmentCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailIngestionLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailIngestionLogs_Deals_DealId",
                        column: x => x.DealId,
                        principalTable: "Deals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Deals_ShortCode",
                table: "Deals",
                column: "ShortCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailIngestionLogs_DealId",
                table: "EmailIngestionLogs",
                column: "DealId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailIngestionLogs");

            migrationBuilder.DropIndex(
                name: "IX_Deals_ShortCode",
                table: "Deals");

            migrationBuilder.DropColumn(
                name: "ShortCode",
                table: "Deals");
        }
    }
}
