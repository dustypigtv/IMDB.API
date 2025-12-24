using IMDB.API.ApiService.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace IMDB.API.ApiService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TitleBasic> TitleBasics { get; set; }

    public DbSet<TitleAka> TitleAkas { get; set; }

    public DbSet<TitleCrew> TitleCrews { get; set; }

    public DbSet<TitleEpisode> TitleEpisodes { get; set; }

    public DbSet<TitlePrincipal> TitlePrincipals { get; set; }

    public DbSet<TitleRating> TitleRatings { get; set; }

    public DbSet<NameBasic> NameBasics { get; set; }

    public DbSet<ExternalData> ExternalData { get; set; }

    public DbSet<Config> Config { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        //Someday I'll figure out how to not make SearchVector either nullable or required while allowing null since it's computed
        //Until then... disable nullable for this
#nullable disable
        modelBuilder.Entity<TitleBasic>()
            .HasIndex(e => new { e.PrimaryTitle, e.OriginalTitle })
            .HasMethod("GIN")
            .IsTsVectorExpressionIndex("english");


        modelBuilder.Entity<NameBasic>()
            .HasIndex(e => e.PrimaryName)
            .HasMethod("GIN")
            .IsTsVectorExpressionIndex("english");

#nullable enable
    }
}
