using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheAccountant.Migrations
{
    /// <inheritdoc />
    public partial class AddRetirementAccountType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RetirementAccountType",
                table: "Accounts",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RetirementAccountType",
                table: "Accounts");
        }
    }
}
