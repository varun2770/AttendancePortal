using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AttendancePortal.Migrations
{
    /// <inheritdoc />
    public partial class AddFaceEmbedding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceProviderPersonId",
                table: "Students");

            migrationBuilder.AddColumn<byte[]>(
                name: "FaceEmbedding",
                table: "Students",
                type: "BLOB",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FaceEmbedding",
                table: "Students");

            migrationBuilder.AddColumn<string>(
                name: "FaceProviderPersonId",
                table: "Students",
                type: "TEXT",
                maxLength: 100,
                nullable: true);
        }
    }
}
