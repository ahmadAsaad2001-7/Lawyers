using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lawyers.InfraStructure.Migrations
{
    /// <inheritdoc />
    public partial class SecondryCreate3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AverageRating",
                table: "LawyerProfiles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Specialization",
                table: "LawyerProfiles",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AverageRating",
                table: "LawyerProfiles");

            migrationBuilder.DropColumn(
                name: "Specialization",
                table: "LawyerProfiles");
        }
    }
}
