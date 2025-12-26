using IMDB.API.ApiService.Data;
using IMDB.API.ApiService.Data.Models;
using IMDB.API.ApiService.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.Annotations;

namespace IMDB.API.ApiService;

[ApiController]
[Route("[controller]/[action]")]
[SwaggerResponse(StatusCodes.Status404NotFound)]
[SwaggerResponse(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
public class APIController(AppDbContext db) : ControllerBase
{
    private const int MAX_SEARCH_RESULTS = 1_000;

    public const string PRIVILEGED_KEY_NAME = "PRIVILEGED_API_KEY";

    static readonly string[] ExternalDataFilter = new string[] { "movie", "tvMovie", "tvSeries", "tvMiniSeries", "tvEpisode" };


    private readonly string _privilegedKey = Environment.GetEnvironmentVariable(PRIVILEGED_KEY_NAME) + string.Empty;

    [HttpGet("{tConst}")]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Title))]
    public async Task<ActionResult<Title>> GetTitle(string tConst)
    {
        if (!tConst.HasValue())
            return NotFound();

        tConst = tConst.ToLower();


        var titleBasic = await db.TitleBasics
            .AsNoTracking()
            .Where(_ => _.TConst == tConst)
            .FirstOrDefaultAsync();

        if (titleBasic is null)
            return NotFound();

        var akas = await db.TitleAkas
                .AsNoTracking()
                .Where(_ => _.TConst == tConst)
                .OrderBy(_ => _.Ordering)
                .ToListAsync();
        if (akas.Count == 0)
            akas = null;

        var titleCrew = await db.TitleCrews
            .AsNoTracking()
            .Where(_ => _.TConst == tConst)
            .FirstOrDefaultAsync();

        var titleRating = await db.TitleRatings
            .AsNoTracking()
            .Where(_ => _.TConst == tConst)
            .FirstOrDefaultAsync();

        var principals = await db.TitlePrincipals
            .AsNoTracking()
            .Where(_ => _.TConst == tConst)
            .ToListAsync();
        if (principals.Count == 0)
            principals = null;

        var episodes = await db.TitleEpisodes
            .AsNoTracking()
            .Where(_ => _.ParentTConst == tConst)
            .ToListAsync();
        if (episodes.Count == 0)
            episodes = null;


        var externalData = await db.ExternalData
            .AsNoTracking()
            .Where(_ => _.TConst == tConst)
            .FirstOrDefaultAsync();

        var ret = new Title
        {
            Akas = akas,
            Basic = titleBasic,
            Crew = titleCrew,
            Episodes = episodes,
            Principals = principals,
            Rating = titleRating,
            ExternalData = externalData
        };

        return Ok(ret);
    }


    [HttpGet("{nConst}")]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(NameBasic))]
    public async Task<ActionResult<NameBasic>> GetPerson(string nConst)
    {
        if (!nConst.HasValue())
            return NotFound();

        nConst = nConst.ToLower();

        var ret = await db.NameBasics
            .AsNoTracking()
            .Where(_ => _.NConst == nConst)
            .FirstOrDefaultAsync();

        if (ret == null)
            return NotFound();

        return Ok(ret);
    }


    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IEnumerable<TitleSearchResult>))]
    public async Task<ActionResult<IEnumerable<TitleSearchResult>>> SearchTitle(string query, string? titleType, int? year, bool? adult)
    {
        if (!query.HasValue())
            return NotFound();

        var q1 = db.TitleBasics.AsNoTracking();

        if (adult.HasValue)
            q1 = q1.Where(_ => _.IsAdult == adult.Value);

        if (titleType.HasValue())
            q1 = q1.Where(_ => _.TitleType == titleType);

        if (year.HasValue)
            q1 = q1.Where(_ => _.StartYear == year.Value);

        var q2 = q1.Where(_ => EF.Functions.ToTsVector("english", _.PrimaryTitle + " " + _.OriginalTitle).Matches(query))
            .Select(_ => new
            {
                Val = _,
                Rank = EF.Functions.ToTsVector("english", _.PrimaryTitle + " " + _.OriginalTitle).RankCoverDensity(EF.Functions.PhraseToTsQuery(query))
            });

        var q3 = from item in q2
                 join rating in db.TitleRatings on item.Val.TConst equals rating.TConst into lj
                 from rating in lj.DefaultIfEmpty()
                 select new TitleSearchResult
                 {
                     Basic = item.Val,
                     Rating = rating,
                     Rank = item.Rank
                 };

        var ret = await q3
            .OrderByDescending(_ => _.Rank)
            .ThenByDescending(_ => _.Rating == null ? 0 : _.Rating.AverageWeighting)
            .ThenByDescending(_ => _.Rating == null ? 0 : _.Rating.NumVotes)
            .ThenByDescending(_ => _.Basic.PrimaryTitle)
            .Take(MAX_SEARCH_RESULTS)
            .ToListAsync();
        if (ret.Count == 0)
            return NotFound();

        return Ok(ret);
    }


    [HttpGet]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IEnumerable<NameBasic>))]
    public async Task<ActionResult<IEnumerable<NameBasic>>> SearchPerson(string query, string? primaryProfession)
    {
        if (!query.HasValue())
            return NotFound();

        var q1 = db.NameBasics.AsNoTracking();

        if (primaryProfession.HasValue())
            q1 = q1.Where(_ => _.PrimaryProfessions != null && _.PrimaryProfessions.Contains(primaryProfession));

        var q2 = q1.Where(_ => EF.Functions.ToTsVector("english", _.PrimaryName).Matches(query))
            .Select(_ => new
            {
                Val = _,
                Rank = EF.Functions.ToTsVector("english", _.PrimaryName).RankCoverDensity(EF.Functions.PhraseToTsQuery(query))
            })
            .OrderByDescending(_ => _.Rank)
            .Take(MAX_SEARCH_RESULTS);


        var ret = await q2.ToListAsync();

        if (ret.Count == 0)
            return NotFound();

        return Ok(ret.Select(_ => _.Val));
    }


    
    /// <summary>
    /// This endpoint isn't for public use
    /// </summary>
    [HttpGet]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<ExternalData>> NextExternalToFind(string privilegedApiKey)
    {
        if (!_privilegedKey.HasValue())
            throw new Exception(PRIVILEGED_KEY_NAME + " environment variable not set");

        if (_privilegedKey != privilegedApiKey)
            return new StatusCodeResult(StatusCodes.Status401Unauthorized);

        var q = from tb in db.TitleBasics.Where(_ => ExternalDataFilter.Contains(_.TitleType))
                join tr in db.TitleRatings on tb.TConst equals tr.TConst into lj1
                from tr in lj1.DefaultIfEmpty()
                join ed in db.ExternalData on tb.TConst equals ed.TConst into lj2
                from ed in lj2.DefaultIfEmpty()
                select new
                {
                    TitleBasic = tb,
                    NumVotes = tr == null ? 0 : tr.NumVotes,
                    ExternalData = ed,
                    LastUpdated = ed == null ? DateTime.MinValue : ed.LastUpdated
                };

        var queryResponse = await q
            .OrderBy(_ => _.LastUpdated)
            .ThenByDescending(_ => _.NumVotes)
            .ThenBy(_ => _.TitleBasic.TConst)
            .FirstOrDefaultAsync();

        if (queryResponse == null)
            return NotFound();

        var ret = queryResponse.ExternalData ?? new ExternalData { TConst = queryResponse.TitleBasic.TConst };
        return Ok(ret);
    }


    /// <summary>
    /// This endpoint isn't for public use
    /// </summary>
    [HttpPost]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult> UpdateExternalData(string privilegedApiKey, ExternalData externalData)
    {
        if (!_privilegedKey.HasValue())
            throw new Exception(PRIVILEGED_KEY_NAME + " environment variable not set");

        if (_privilegedKey != privilegedApiKey)
            return new StatusCodeResult(StatusCodes.Status401Unauthorized);


        if (!externalData.TConst.HasValue())
            throw new ArgumentNullException(nameof(externalData.TConst));

        var entity = await db.ExternalData
            .Where(_ => _.TConst == externalData.TConst)
            .FirstOrDefaultAsync();

        if (entity == null)
        {
            entity = db.ExternalData.Add(externalData).Entity;
        }
        else
        {
            entity.Date = externalData.Date ?? entity.Date;
            entity.ImageUrl = externalData.ImageUrl.HasValue() ? externalData.ImageUrl : entity.ImageUrl;
            entity.MPAA_Rating = externalData.MPAA_Rating.HasValue() ? externalData.MPAA_Rating : entity.MPAA_Rating;
            entity.Plot = externalData.Plot.HasValue() ? externalData.Plot : entity.Plot;
        }

        entity.LastUpdated = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok();
    }


}
