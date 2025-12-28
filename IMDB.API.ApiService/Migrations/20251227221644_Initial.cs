using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IMDB.API.ApiService.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Config",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LastUpdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Config", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalData",
                columns: table => new
                {
                    TConstId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: true),
                    Plot = table.Column<string>(type: "text", nullable: true),
                    ImageUrl = table.Column<string>(type: "text", nullable: true),
                    MPAA_Rating = table.Column<string>(type: "text", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalData", x => x.TConstId);
                });

            migrationBuilder.CreateTable(
                name: "NameBasics",
                columns: table => new
                {
                    NConstId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    PrimaryName = table.Column<string>(type: "text", nullable: false),
                    BirthYear = table.Column<int>(type: "integer", nullable: true),
                    DeathYear = table.Column<int>(type: "integer", nullable: true),
                    PrimaryProfessions = table.Column<List<string>>(type: "text[]", nullable: true),
                    KnownForTitles = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameBasics", x => x.NConstId);
                });

            migrationBuilder.CreateTable(
                name: "TitleAkas",
                columns: table => new
                {
                    TConstId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Ordering = table.Column<int>(type: "integer", nullable: false),
                    TitleHashId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    Types = table.Column<List<string>>(type: "text[]", nullable: true),
                    Attributes = table.Column<List<string>>(type: "text[]", nullable: true),
                    IsOriginalTitle = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleAkas", x => new { x.TConstId, x.Ordering, x.TitleHashId });
                });

            migrationBuilder.CreateTable(
                name: "TitleBasics",
                columns: table => new
                {
                    TConstId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    TitleType = table.Column<string>(type: "text", nullable: false),
                    PrimaryTitle = table.Column<string>(type: "text", nullable: false),
                    OriginalTitle = table.Column<string>(type: "text", nullable: false),
                    IsAdult = table.Column<bool>(type: "boolean", nullable: false),
                    StartYear = table.Column<int>(type: "integer", nullable: true),
                    EndYear = table.Column<int>(type: "integer", nullable: true),
                    RuntimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    Genres = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleBasics", x => x.TConstId);
                });

            migrationBuilder.CreateTable(
                name: "TitleCrews",
                columns: table => new
                {
                    TConstId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Directors = table.Column<List<string>>(type: "text[]", nullable: true),
                    Writers = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleCrews", x => x.TConstId);
                });

            migrationBuilder.CreateTable(
                name: "TitleEpisodes",
                columns: table => new
                {
                    TConstId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ParentTConstId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    SeasonNumber = table.Column<int>(type: "integer", nullable: true),
                    EpisodeNumber = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleEpisodes", x => x.TConstId);
                });

            migrationBuilder.CreateTable(
                name: "TitlePrincipals",
                columns: table => new
                {
                    TConstId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Ordering = table.Column<int>(type: "integer", nullable: false),
                    NConstId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Job = table.Column<string>(type: "text", nullable: true),
                    Character = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitlePrincipals", x => new { x.TConstId, x.Ordering, x.NConstId });
                });

            migrationBuilder.CreateTable(
                name: "TitleRatings",
                columns: table => new
                {
                    TConstId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    AverageWeighting = table.Column<float>(type: "real", nullable: false),
                    NumVotes = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleRatings", x => x.TConstId);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_NameBasics_PrimaryName",
                table: "NameBasics",
                column: "PrimaryName")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");

            migrationBuilder.CreateIndex(
                name: "IX_TitleBasics_PrimaryTitle_OriginalTitle",
                table: "TitleBasics",
                columns: new[] { "PrimaryTitle", "OriginalTitle" })
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:TsVectorConfig", "english");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Config");

            migrationBuilder.DropTable(
                name: "ExternalData");

            migrationBuilder.DropTable(
                name: "NameBasics");

            migrationBuilder.DropTable(
                name: "TitleAkas");

            migrationBuilder.DropTable(
                name: "TitleBasics");

            migrationBuilder.DropTable(
                name: "TitleCrews");

            migrationBuilder.DropTable(
                name: "TitleEpisodes");

            migrationBuilder.DropTable(
                name: "TitlePrincipals");

            migrationBuilder.DropTable(
                name: "TitleRatings");

            migrationBuilder.DropTable(
                name: "UpdateHistories");
        }
    }
}
