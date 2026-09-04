using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheAccountant.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionImportHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImportHash",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImportHash",
                table: "Transactions");
        }
    }
}
