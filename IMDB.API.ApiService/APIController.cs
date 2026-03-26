using IMDB.API.ApiService.Data;
using IMDB.API.ApiService.Data.Models;
using IMDB.API.ApiService.Responses;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;

namespace IMDB.API.ApiService;

[ApiController]
[Route("[controller]/[action]")]
[SwaggerResponse(StatusCodes.Status404NotFound)]
[SwaggerResponse(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
public class APIController(AppDbContext db) : ControllerBase
{
    private const int ONE_DAY = 86400;

    private const int MAX_SEARCH_RESULTS = 1_000;

    
    [HttpGet("{tConst}")]
    [OutputCache(Duration = ONE_DAY)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Title))]
    public async Task<ActionResult<Title>> GetTitle(string tConst)
    {
        if (!tConst.HasValue())
            return NotFound();

        tConst = tConst.ToLower();

        var q = from titleBasic in db.TitleBasics.Where(_ => _.TConst == tConst)

                join titleCrew in db.TitleCrews on titleBasic.TConst equals titleCrew.TConst into titleCrewLJ
                from titleCrew in titleCrewLJ.DefaultIfEmpty()

                join titleRating in db.TitleRatings on titleBasic.TConst equals titleRating.TConst into titleRatingLJ
                from titleRating in titleRatingLJ.DefaultIfEmpty()

                select new Title
                {
                    Basic = titleBasic,
                    Crew = titleCrew,
                    Rating = titleRating
                };


        var ret = await q.AsNoTracking().FirstOrDefaultAsync();
        if (ret == null || ret.Basic == null)
            return NotFound();

        ret.Basic.TConst = tConst;
        ret.Crew?.TConst = tConst;
        ret.Rating?.TConst = tConst;

        ret.Akas = await db.TitleAkas
            .AsNoTracking()
            .Where(_ => _.TConst == tConst)
            .ToListAsync();
        if (ret.Akas?.Count == 0)
            ret.Akas = null;


        ret.Episodes = await db.TitleEpisodes
            .AsNoTracking()
            .Where(_ => _.ParentTConst == tConst)
            .ToListAsync();
        if (ret.Episodes?.Count == 0)
            ret.Episodes = null;


        ret.Principals = await db.TitlePrincipals
            .AsNoTracking()
            .Where(_ => _.TConst == tConst)
            .ToListAsync();
        if (ret.Principals?.Count == 0)
            ret.Principals = null;

        return Ok(ret);
    }


    [HttpGet("{nConst}")]
    [OutputCache(Duration = ONE_DAY)]
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
    [OutputCache(Duration = ONE_DAY)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IEnumerable<TitleSearchResult>))]
    public async Task<ActionResult<IEnumerable<TitleSearchResult>>> SearchTitle(string query, string? titleType, int? year, bool? adult)
    {
        if (!query.HasValue())
            return NotFound();
        query = query.Trim();

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
            .OrderBy(_ => _.Basic.PrimaryTitle.ToLower() == query.ToLower() ? 0 : 1)
            .ThenBy(_ => _.Basic.OriginalTitle.ToLower() == query.ToLower() ? 0 : 1)
            .ThenByDescending(_ => _.Rank)
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
    [OutputCache(Duration = ONE_DAY)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(IEnumerable<NameBasic>))]
    public async Task<ActionResult<IEnumerable<NameBasic>>> SearchPerson(string query, string? primaryProfession)
    {
        if (!query.HasValue())
            return NotFound();
        query = query.Trim();

        var q1 = db.NameBasics.AsNoTracking();

        if (primaryProfession.HasValue())
            q1 = q1.Where(_ => _.PrimaryProfessions != null && _.PrimaryProfessions.Contains(primaryProfession));

        var q2 = q1.Where(_ => EF.Functions.ToTsVector("english", _.PrimaryName).Matches(query))
            .Select(_ => new
            {
                Val = _,
                Rank = EF.Functions.ToTsVector("english", _.PrimaryName).RankCoverDensity(EF.Functions.PhraseToTsQuery(query))
            })
            .OrderBy(_ => _.Val.PrimaryName.ToLower() == query.ToLower() ? 0 : 1)
            .ThenByDescending(_ => _.Rank)
            .Take(MAX_SEARCH_RESULTS);


        var ret = await q2.ToListAsync();

        if (ret.Count == 0)
            return NotFound();

        return Ok(ret.Select(_ => _.Val));
    }
}