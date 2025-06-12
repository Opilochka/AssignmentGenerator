using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Opilochka.API.Migrations
{
    /// <inheritdoc />
    public partial class AddTask : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TemlateCallpy",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TemplateCallcs",
                table: "Tasks");

            migrationBuilder.RenameColumn(
                name: "Templatepy",
                table: "Tasks",
                newName: "Output");

            migrationBuilder.RenameColumn(
                name: "Templatecs",
                table: "Tasks",
                newName: "Input");

            migrationBuilder.AddColumn<int>(
                name: "Icon",
                table: "Lessons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "UserId",
                table: "Lessons",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_Lessons_UserId",
                table: "Lessons",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Lessons_Users_UserId",
                table: "Lessons",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Lessons_Users_UserId",
                table: "Lessons");

            migrationBuilder.DropIndex(
                name: "IX_Lessons_UserId",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Lessons");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Lessons");

            migrationBuilder.RenameColumn(
                name: "Output",
                table: "Tasks",
                newName: "Templatepy");

            migrationBuilder.RenameColumn(
                name: "Input",
                table: "Tasks",
                newName: "Templatecs");

            migrationBuilder.AddColumn<string>(
                name: "TemlateCallpy",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TemplateCallcs",
                table: "Tasks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
