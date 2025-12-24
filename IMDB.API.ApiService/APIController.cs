using IMDB.API.ApiService.Data;
using IMDB.API.ApiService.Data.Models;
using IMDB.API.ApiService.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace IMDB.API.ApiService;

[ApiController]
[Route("[controller]/[action]")]
[SwaggerResponse(StatusCodes.Status404NotFound)]
[SwaggerResponse(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
public class APIController(AppDbContext db) : ControllerBase
{
    private const int MAX_SEARCH_RESULTS = 1_000;

    [HttpGet("{ttId}")]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Title))]
    public async Task<ActionResult<Title>> GetTitle(string ttId)
    {
        if (!ttId.HasValue())
            return NotFound();

        ttId = ttId.ToLower();


        var titleBasic = await db.TitleBasics
            .AsNoTracking()
            .Where(_ => _.TConst == ttId)
            .FirstOrDefaultAsync();

        if (titleBasic is null)
            return NotFound();

        var akas = await db.TitleAkas
                .AsNoTracking()
                .Where(_ => _.TitleId == ttId)
                .OrderBy(_ => _.Ordering)
                .ToListAsync();
        if (akas.Count == 0)
            akas = null;

        var titleCrew = await db.TitleCrews
            .AsNoTracking()
            .Where(_ => _.TConst == ttId)
            .FirstOrDefaultAsync();

        var titleRating = await db.TitleRatings
            .AsNoTracking()
            .Where(_ => _.TConst == ttId)
            .FirstOrDefaultAsync();

        var principals = await db.TitlePrincipals
            .AsNoTracking()
            .Where(_ => _.TConst == ttId)
            .ToListAsync();
        if (principals.Count == 0)
            principals = null;

        var episodes = await db.TitleEpisodes
            .AsNoTracking()
            .Where(_ => _.ParentTConst == ttId)
            .ToListAsync();
        if (episodes.Count == 0)
            episodes = null;

        var ret = new Title
        {
            Akas = akas,
            Basic = titleBasic,
            Crew = titleCrew,
            Episodes = episodes,
            Principals = principals,
            Rating = titleRating
        };

        return Ok(ret);
    }


    [HttpGet("{nmId}")]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(NameBasic))]
    public async Task<ActionResult<NameBasic>> GetPerson(string nmId)
    {
        if (!nmId.HasValue())
            return NotFound();

        nmId = nmId.ToLower();

        var ret = await db.NameBasics
            .AsNoTracking()
            .Where(_ => _.NConst == nmId)
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
}
