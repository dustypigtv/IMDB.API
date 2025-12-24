using DustyPig.Utils;
using EFCore.BulkExtensions;
using IMDB.API.ApiService.Data;
using IMDB.API.ApiService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO.Compression;

namespace IMDB.API.ApiService;

public class DailyUpdater : IHostedService
{
    private const int ONE_SECOND = 1000;
    private const int ONE_DAY = ONE_SECOND * 60 * 60 * 24;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CancellationTokenSource _cts = new();
    private readonly Timer _timer;
    private readonly ILogger<DailyUpdater> _logger;

    private static readonly BulkConfig _bulkConfig = new()
    {
        BulkCopyTimeout = 0,
        UseUnlogged = true
    };


    public DailyUpdater(IServiceScopeFactory serviceScopeFactory, ILogger<DailyUpdater> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _timer = new(Tick);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            //Try to set the timer to the next time if possible
            using var scope = _serviceScopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var config = await db.Config
                .AsNoTracking()
                .Where(_ => _.Id == 1)
                .FirstAsync(_cts.Token);

#if DEBUG
            config.LastUpdate = DateTime.UtcNow.AddDays(-2);
#endif
            var nextUpdate = config.LastUpdate.AddDays(1);
            if (nextUpdate > DateTime.UtcNow.AddMinutes(1))
            {
                //Wait 24 hours from previous update
                _timer.Change(nextUpdate - DateTime.UtcNow, Timeout.InfiniteTimeSpan);
            }
            else
            {
                //It's been more than 24 hours since last update, so start now
                _timer.Change(ONE_SECOND, Timeout.Infinite);
            }
        }
        catch
        {
            //Something went wrong (like the config row doesn't exist), so just start now
            _timer.Change(ONE_SECOND, Timeout.Infinite);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();
        _cts.Cancel();
        return Task.CompletedTask;
    }

    private async void Tick(object? state)
    {
        if (!_cts.IsCancellationRequested)
            try
            {
                _logger.LogDebug("Tick");
                await DoWork();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tick");
            }


        if (!_cts.IsCancellationRequested)
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var config = await db.Config
                    .Where(_ => _.Id == 1)
                    .FirstOrDefaultAsync(_cts.Token);
                config ??= db.Config.Add(new Config { Id = 1 }).Entity;
                config.LastUpdate = DateTime.UtcNow;
                await db.SaveChangesAsync(_cts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed updating LastUpdate in database");
            }



        if (!_cts.IsCancellationRequested)
            try { _timer.Change(ONE_DAY, Timeout.Infinite); }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed restarting timer");
            }
    }


    private async Task DoWork()
    {
        try
        {
            await UpdateTitleBasics();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitleBasics) + " Failed");
        }

        try
        {
            await UpdateTitleAkas();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitleAkas) + " Failed");
        }

        try
        {
            await UpdateTitleCrew();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitleCrew) + " Failed");
        }

        try
        {
            await UpdateTitleEpisodes();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitleEpisodes) + " Failed");
        }

        try
        {
            await UpdateTitlePrincipals();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitlePrincipals) + " Failed");
        }

        try
        {
            await UpdateTitleRatings();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitleRatings) + " Failed");
        }

        try
        {
            await UpdateNameBasics();
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateNameBasics) + " Failed");
        }
    }




    private async Task UpdateTitleBasics()
    {
        const string URL = "https://datasets.imdbws.com/title.basics.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleBasics));

        Dictionary<string, TitleBasic> existingEntities;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            existingEntities = await db.TitleBasics
                .AsNoTracking()
                .ToDictionaryAsync(_ => _.TConst, cancellationToken: _cts.Token);
        }

        await ImportFile(URL, createEntity: fields =>
        {
            var entity = new TitleBasic
            {
                TConst = fields[0],
                TitleType = fields[1],
                PrimaryTitle = fields[2],
                OriginalTitle = fields[3],
                IsAdult = fields[4] != "0",
                StartYear = fields[5].TryGetUShort(),
                EndYear = fields[6].TryGetUShort(),
                RuntimeMinutes = fields[7].TryGetUInt(),
                Genres = fields[8].ToStringList()
            };

            return CompareToExisting(entity, entity.TConst, existingEntities);
        },
        getEntitiesToDelete: () => [.. existingEntities.Values]);
    }



    private async Task UpdateTitleAkas()
    {
        const string URL = "https://datasets.imdbws.com/title.akas.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleAkas));

        Dictionary<string, TitleAka> existingEntities;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            existingEntities = await db.TitleAkas
                .AsNoTracking()
                .ToDictionaryAsync(_ => _.DictKey(), cancellationToken: _cts.Token);
        }

        await ImportFile(URL, createEntity: fields =>
        {
            var entity = new TitleAka
            {
                TitleId = fields[0],
                Ordering = int.Parse(fields[1]),
                Title = fields[2],
                Region = fields[3] == "\\N" ? null : fields[3],
                Language = fields[4] == "\\N" ? null : fields[4],
                Types = fields[5].ToStringList(),
                Attributes = fields[6].ToStringList(),
                IsOriginalTitle = fields[7] == "1"
            };

            return CompareToExisting(entity, entity.DictKey(), existingEntities);
        },
       getEntitiesToDelete: () => [.. existingEntities.Values]);
    }



    private async Task UpdateTitleCrew()
    {
        const string URL = "https://datasets.imdbws.com/title.crew.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleCrew));

        Dictionary<string, TitleCrew> existingEntities;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            existingEntities = await db.TitleCrews
                .AsNoTracking()
                .ToDictionaryAsync(_ => _.TConst, cancellationToken: _cts.Token);
        }

        await ImportFile(URL, createEntity: fields =>
        {
            var entity = new TitleCrew
            {
                TConst = fields[0],
                Directors = fields[1].ToStringList(),
                Writers = fields[2].ToStringList(),
            };

            if (entity.Directors.HasItems() || entity.Writers.HasItems())
            {
                return CompareToExisting(entity, entity.TConst, existingEntities);
            }
            else
            {
                existingEntities.Remove(entity.TConst);
                return null;
            }
        },
        getEntitiesToDelete: () => [.. existingEntities.Values]);
    }



    private async Task UpdateTitleEpisodes()
    {
        const string URL = "https://datasets.imdbws.com/title.episode.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleEpisodes));

        Dictionary<string, TitleEpisode> existingEntities;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            existingEntities = await db.TitleEpisodes
                .AsNoTracking()
                .ToDictionaryAsync(_ => _.TConst, cancellationToken: _cts.Token);
        }

        await ImportFile(URL, createEntity: fields =>
        {
            var entity = new TitleEpisode
            {
                TConst = fields[0],
                ParentTConst = fields[1],
                SeasonNumber = fields[2].TryGetInt(),
                EpisodeNumber = fields[3].TryGetInt()
            };

            return CompareToExisting(entity, entity.TConst, existingEntities);
        },
        getEntitiesToDelete: () => [.. existingEntities.Values]);
    }



    private async Task UpdateTitlePrincipals()
    {
        const string URL = "https://datasets.imdbws.com/title.principals.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitlePrincipals));

        Dictionary<string, TitlePrincipal> existingEntities;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            existingEntities = await db.TitlePrincipals
                .AsNoTracking()
                .ToDictionaryAsync(_ => _.TConst, cancellationToken: _cts.Token);
        }

        await ImportFile(URL, createEntity: fields =>
        {
            var entity = new TitlePrincipal
            {
                TConst = fields[0],
                Ordering = int.Parse(fields[1]),
                NConst = fields[2],
                Category = fields[3],
                Job = fields[4] == "\\N" ? null : fields[4],
                Character = fields[5] == "\\N" ? null : fields[5]
            };

            return CompareToExisting(entity, entity.TConst, existingEntities);
        },
        getEntitiesToDelete: () => [.. existingEntities.Values]);
    }



    private async Task UpdateTitleRatings()
    {
        const string URL = "https://datasets.imdbws.com/title.ratings.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleRatings));

        Dictionary<string, TitleRating> existingEntities;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            existingEntities = await db.TitleRatings
                .AsNoTracking()
                .ToDictionaryAsync(_ => _.TConst, cancellationToken: _cts.Token);
        }

        await ImportFile(URL, createEntity: fields =>
        {
            var entity = new TitleRating
            {
                TConst = fields[0],
                AverageWeighting = float.Parse(fields[1]),
                NumVotes = int.Parse(fields[2])
            };

            return CompareToExisting(entity, entity.TConst, existingEntities);
        },
        getEntitiesToDelete: () => [.. existingEntities.Values]);
    }



    private async Task UpdateNameBasics()
    {
        const string URL = "https://datasets.imdbws.com/name.basics.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateNameBasics));

        Dictionary<string, NameBasic> existingEntities;
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            existingEntities = await db.NameBasics
                .AsNoTracking()
                .ToDictionaryAsync(_ => _.NConst, cancellationToken: _cts.Token);
        }

        await ImportFile(URL, createEntity: fields =>
        {
            var entity = new NameBasic
            {
                NConst = fields[0],
                PrimaryName = fields[1],
                BirthYear = fields[2].TryGetInt(),
                DeathYear = fields[3].TryGetInt(),
                PrimaryProfessions = fields[4].ToStringList(),
                KnownForTitles = fields[5].ToStringList(),
            };

            return CompareToExisting(entity, entity.NConst, existingEntities);
        },
        getEntitiesToDelete: () => [.. existingEntities.Values]);
    }








    private async Task ImportFile<T>(string url, Func<string[], T?> createEntity, Func<List<T>> getEntitiesToDelete) where T : class, IEquatable<T>
    {
        var file = new FileInfo(Path.Combine("/tmp", Path.ChangeExtension(Path.GetFileName(url), null)));
        await DownloadFile(url, file);

        var upsertList = new List<T>();
        using (TextReader tr = new StreamReader(file.FullName))
        {
            //Headers
            tr.ReadLine();

            string? line;
            while ((line = tr.ReadLine()) != null)
            {
                var entity = createEntity(line.Split('\t'));
                if (entity != null)
                    upsertList.Add(entity);

                //If list gets too big it fails no matter what config I try, so chunk it
                if(upsertList.Count >= 100_000)
                {
                    await UpdateDB(upsertList, []);
                    upsertList.Clear();
                }
            }
        }
#if !DEBUG
        file.TryDelete();    
#endif

        var deleteList = getEntitiesToDelete();
        await UpdateDB(upsertList, deleteList);
    }


    private async Task UpdateDB<T>(List<T> upsertList, List<T> deleteList) where T : class, IEquatable<T>
    {
        using var scope = _serviceScopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (deleteList.Count > 0)
            await db.BulkDeleteAsync(deleteList, _bulkConfig, cancellationToken: _cts.Token);

        if (upsertList.Count > 0)
            await db.BulkInsertOrUpdateAsync(upsertList, bulkConfig: _bulkConfig, cancellationToken: _cts.Token);
    }



    private static T? CompareToExisting<T>(T entity, string key, Dictionary<string, T> existingEntities) where T : class, IEquatable<T>
    {
        if (existingEntities.TryGetValue(key, out var existing))
        {
            existingEntities.Remove(key);
            if (!entity.Equals(existing))
                return entity;
        }
        else
        {
            return entity;
        }

        return null;
    }



    private async Task DownloadFile(string url, FileInfo outputFile)
    {
#if DEBUG
        if (outputFile.Exists)
            return;
#endif
        outputFile.TryDelete();

        var gzFile = new FileInfo("/tmp/tmp.gz");
        gzFile.TryDelete();

        using var scope = _serviceScopeFactory.CreateScope();
        {
            using var client = scope.ServiceProvider.GetRequiredService<HttpClient>();
            await client.DownloadFileAsync(url, gzFile, cancellationToken: _cts.Token);
        }

        using var inputStream = File.OpenRead(gzFile.FullName);
        {
            using var outputStream = File.Create(outputFile.FullName);
            using var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
            await decompressionStream.CopyToAsync(outputStream, _cts.Token);
        }

        gzFile.TryDelete();
    }

}
