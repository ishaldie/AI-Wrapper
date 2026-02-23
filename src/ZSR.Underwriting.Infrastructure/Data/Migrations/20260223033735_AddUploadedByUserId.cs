using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZSR.Underwriting.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadedByUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadedByUserId",
                table: "UploadedDocuments",
                type: "TEXT",
                maxLength: 36,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedByUserId",
                table: "UploadedDocuments");
        }
    }
}
