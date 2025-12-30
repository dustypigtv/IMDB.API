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

    public const string PRIVILEGED_KEY_NAME = "PRIVILEGED_API_KEY";



    //The ParentTConstId needs to be nullable for the TitleEpisodes part of the NextExternalToFind query,
    //but can't do that with an anonymous type in LINQ.
    //So this wrapper exists
    class DTO { public ulong? ParentTConstId { get; set; } };




    private readonly string _privilegedKey = Environment.GetEnvironmentVariable(PRIVILEGED_KEY_NAME) + string.Empty;

    [HttpGet("{tConst}")]
    [OutputCache(Duration = ONE_DAY)]
    [SwaggerResponse(StatusCodes.Status200OK, Type = typeof(Title))]
    public async Task<ActionResult<Title>> GetTitle(string tConst)
    {
        if (!tConst.HasValue())
            return NotFound();

        tConst = tConst.ToLower();
        var tConstId = tConst.ToNumId();

        var q = from titleBasic in db.TitleBasics.Where(_ => _.TConstId == tConstId)

                join titleCrew in db.TitleCrews on titleBasic.TConstId equals titleCrew.TConstId into titleCrewLJ
                from titleCrew in titleCrewLJ.DefaultIfEmpty()

                join titleRating in db.TitleRatings on titleBasic.TConstId equals titleRating.TConstId into titleRatingLJ
                from titleRating in titleRatingLJ.DefaultIfEmpty()

                join externalData in db.ExternalData on titleBasic.TConstId equals externalData.TConstId into externalDataLJ
                from externalData in externalDataLJ.DefaultIfEmpty()

                select new Title
                {
                    Basic = titleBasic,
                    Crew = titleCrew,
                    Rating = titleRating,
                    ExternalData = externalData
                };


        var ret = await q.AsNoTracking().FirstOrDefaultAsync();
        if (ret == null || ret.Basic == null)
            return NotFound();

        ret.Basic.TConst = tConst;
        ret.Crew?.TConst = tConst;
        ret.Rating?.TConst = tConst;
        ret.ExternalData?.TConst = tConst;

        ret.Akas = await db.TitleAkas
            .AsNoTracking()
            .Where(_ => _.TConstId == tConstId)
            .ToListAsync();
        if (ret.Akas?.Count > 0)
            ret.Akas.ForEach(_ => _.TConst = tConst);
        else
            ret.Akas = null;


        ret.Episodes = await db.TitleEpisodes
            .AsNoTracking()
            .Where(_ => _.ParentTConstId == tConstId)
            .ToListAsync();
        if (ret.Episodes?.Count > 0)
        {
            ret.Episodes.ForEach(_ =>
            {
                _.TConst = _.TConstId.ToTConst();
                _.ParentTConst = tConst;
            });
        }
        else
        {
            ret.Episodes = null;
        }


        ret.Principals = await db.TitlePrincipals
            .AsNoTracking()
            .Where(_ => _.TConstId == tConstId)
            .ToListAsync();
        if (ret.Principals?.Count > 0)
        {
            ret.Principals.ForEach(_ =>
            {
                _.TConst = tConst;
                _.NConst = _.NConstId.ToNConst();
            });
        }
        else
        {
            ret.Principals = null;
        }

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
        var nConstId = nConst.ToNumId();

        var ret = await db.NameBasics
            .AsNoTracking()
            .Where(_ => _.NConstId == nConstId)
            .FirstOrDefaultAsync();

        if (ret == null)
            return NotFound();

        ret.NConst = nConst;

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
                 join rating in db.TitleRatings on item.Val.TConstId equals rating.TConstId into lj
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

        ret.ForEach(_ =>
        {
            _.Basic.TConst = _.Basic.TConstId.ToTConst();
            _.Rating?.TConst = _.Rating.TConstId.ToTConst();
        });

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

        ret.ForEach(_ => _.Val.NConst = _.Val.NConstId.ToNConst());

        return Ok(ret.Select(_ => _.Val));
    }



    /// <summary>
    /// This endpoint isn't for public use
    /// </summary>
    [HttpGet]
    [ApiExplorerSettings(IgnoreApi = true)]
    [DisableRequestTimeout]
    public async Task<ActionResult<IEnumerable<UpdateExternalData>>> NextExternalToFind(string privilegedApiKey, ushort? count)
    {
        if (!_privilegedKey.HasValue())
            throw new Exception(PRIVILEGED_KEY_NAME + " environment variable not set");

        if (_privilegedKey != privilegedApiKey)
            return new StatusCodeResult(StatusCodes.Status401Unauthorized);

        //Default of 1
        if (count == null || count == 0)
            count = 1;

        //No more than 1k
        if (count > 1000)
            count = 1000;


        
        var q = from tb in db.TitleBasics.Where(_ => _.TitleType != "videoGame")
                join tr in db.TitleRatings on tb.TConstId equals tr.TConstId into lj1
                from tr in lj1.DefaultIfEmpty()
                join ed in db.ExternalData on tb.TConstId equals ed.TConstId into lj2
                from ed in lj2.DefaultIfEmpty()
                join ec in db.TitleEpisodes.Select(_ => _.ParentTConstId).Distinct().Select(_ => new DTO() { ParentTConstId = _ }) on tb.TConstId equals ec.ParentTConstId into lj3
                from ec in lj3.DefaultIfEmpty()
                select new
                {
                    TitleBasic = tb,
                    NumVotes = tr == null ? 0 : tr.NumVotes,
                    ExternalData = ed,
                    LastUpdated = ed == null ? DateTime.MinValue : ed.LastUpdated,
                    HasEpisodes = ec.ParentTConstId.HasValue
                };

        var queryResponse = await q
            .OrderBy(_ => _.LastUpdated)
            .ThenByDescending(_ => _.NumVotes)
            .ThenBy(_ => _.TitleBasic.TConstId)
            .Take(count.Value)
            .ToListAsync();

        if (queryResponse == null || queryResponse.Count == 0)
            return NotFound();

        var ret = new List<UpdateExternalData>();
        foreach(var item in queryResponse)
        {
            var ued = new UpdateExternalData
            {
                TConst = item.TitleBasic.TConstId.ToTConst(),
                TitleType = item.TitleBasic.TitleType,
                HasEpisodes = item.HasEpisodes
            };

            if(item.ExternalData != null)
            {
                ued.Date = item.ExternalData.Date;
                ued.ImageUrl = item.ExternalData.ImageUrl;
                ued.MPAA_Rating = item.ExternalData.MPAA_Rating;
                ued.Plot = item.ExternalData.Plot;
            }

            ret.Add(ued);
        }

        return Ok(ret);
    }


    /// <summary>
    /// This endpoint isn't for public use
    /// </summary>
    [HttpPost]
    [ApiExplorerSettings(IgnoreApi = true)]
    [DisableRequestTimeout]
    [RequestSizeLimit(104857600)] // Handle requests up to 100 MB
    public async Task<ActionResult> UpdateExternalData(string privilegedApiKey, List<ExternalData> externalDatas)
    {
        if (!_privilegedKey.HasValue())
            throw new Exception(PRIVILEGED_KEY_NAME + " environment variable not set");

        if (_privilegedKey != privilegedApiKey)
            return new StatusCodeResult(StatusCodes.Status401Unauthorized);

        if (externalDatas.Count == 0)
            return Ok();

        foreach (var externalData in externalDatas)
        {
            if (!externalData.TConst.HasValue())
                return BadRequest();
            externalData.TConstId = externalData.TConst.ToNumId();
        }

        while (externalDatas.Count > 0)
        {
            List<ExternalData> batch = [.. externalDatas.Take(1000)];
            List<ulong> batchIds = [.. batch.Select(_ => _.TConstId).Distinct()];
            externalDatas.RemoveAll(_ => batchIds.Contains(_.TConstId));

            var entities = await db.ExternalData
                .Where(_ => batchIds.Contains(_.TConstId))
                .ToListAsync();

            foreach (var item in batch)
            {
                var entity = entities.FirstOrDefault(_ => _.TConstId == item.TConstId);
                if (entity == null)
                {
                    entity = db.ExternalData.Add(item).Entity;
                }
                else
                {
                    entity.Date = item.Date ?? entity.Date;
                    entity.ImageUrl = item.ImageUrl.HasValue() ? item.ImageUrl : entity.ImageUrl;
                    entity.MPAA_Rating = item.MPAA_Rating.HasValue() ? item.MPAA_Rating : entity.MPAA_Rating;
                    entity.Plot = item.Plot.HasValue() ? item.Plot : entity.Plot;
                }

                entity.LastUpdated = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
        }

        return Ok();
    }
}


