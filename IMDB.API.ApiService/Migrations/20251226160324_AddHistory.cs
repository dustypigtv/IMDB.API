using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IMDB.API.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class AddHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UpdateHistories",
                columns: table => new
                {
                    TableName = table.Column<string>(type: "text", nullable: false),
                    LastStarted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastFinished = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateHistories", x => x.TableName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UpdateHistories");
        }
    }
}
