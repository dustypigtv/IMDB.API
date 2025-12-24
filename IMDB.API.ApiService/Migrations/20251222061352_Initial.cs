using Microsoft.EntityFrameworkCore.Migrations;

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
                name: "NameBasics",
                columns: table => new
                {
                    NConst = table.Column<string>(type: "text", nullable: false),
                    PrimaryName = table.Column<string>(type: "text", nullable: false),
                    BirthYear = table.Column<int>(type: "integer", nullable: true),
                    DeathYear = table.Column<int>(type: "integer", nullable: true),
                    PrimaryProfessions = table.Column<List<string>>(type: "text[]", nullable: true),
                    KnownForTitles = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NameBasics", x => x.NConst);
                });

            migrationBuilder.CreateTable(
                name: "TitleAkas",
                columns: table => new
                {
                    TitleId = table.Column<string>(type: "text", nullable: false),
                    Ordering = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Region = table.Column<string>(type: "text", nullable: true),
                    Language = table.Column<string>(type: "text", nullable: true),
                    Types = table.Column<List<string>>(type: "text[]", nullable: true),
                    Attributes = table.Column<List<string>>(type: "text[]", nullable: true),
                    IsOriginalTitle = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleAkas", x => new { x.TitleId, x.Ordering, x.Title });
                });

            migrationBuilder.CreateTable(
                name: "TitleBasics",
                columns: table => new
                {
                    TConst = table.Column<string>(type: "text", nullable: false),
                    TitleType = table.Column<string>(type: "text", nullable: false),
                    PrimaryTitle = table.Column<string>(type: "text", nullable: false),
                    OriginalTitle = table.Column<string>(type: "text", nullable: false),
                    IsAdult = table.Column<bool>(type: "boolean", nullable: false),
                    StartYear = table.Column<int>(type: "integer", nullable: true),
                    EndYear = table.Column<int>(type: "integer", nullable: true),
                    RuntimeMinutes = table.Column<long>(type: "bigint", nullable: true),
                    Genres = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleBasics", x => x.TConst);
                });

            migrationBuilder.CreateTable(
                name: "TitleCrews",
                columns: table => new
                {
                    TConst = table.Column<string>(type: "text", nullable: false),
                    Directors = table.Column<List<string>>(type: "text[]", nullable: true),
                    Writers = table.Column<List<string>>(type: "text[]", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleCrews", x => x.TConst);
                });

            migrationBuilder.CreateTable(
                name: "TitleEpisodes",
                columns: table => new
                {
                    TConst = table.Column<string>(type: "text", nullable: false),
                    ParentTConst = table.Column<string>(type: "text", nullable: false),
                    SeasonNumber = table.Column<int>(type: "integer", nullable: true),
                    EpisodeNumber = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleEpisodes", x => x.TConst);
                });

            migrationBuilder.CreateTable(
                name: "TitlePrincipals",
                columns: table => new
                {
                    TConst = table.Column<string>(type: "text", nullable: false),
                    Ordering = table.Column<int>(type: "integer", nullable: false),
                    NConst = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Job = table.Column<string>(type: "text", nullable: true),
                    Character = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitlePrincipals", x => new { x.TConst, x.Ordering, x.NConst });
                });

            migrationBuilder.CreateTable(
                name: "TitleRatings",
                columns: table => new
                {
                    TConst = table.Column<string>(type: "text", nullable: false),
                    AverageWeighting = table.Column<float>(type: "real", nullable: false),
                    NumVotes = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TitleRatings", x => x.TConst);
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
        }
    }
}
