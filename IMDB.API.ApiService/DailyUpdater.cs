using DustyPig.Utils;
using EFCore.BulkExtensions;
using IMDB.API.ApiService.Data;
using IMDB.API.ApiService.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;

namespace IMDB.API.ApiService;

public class DailyUpdater : IHostedService
{
    /*
        Ok. When all is said and done, either there is a huge memory read from the db, or a huge memory write to the db.
        And my little $5/mo server just can't handle it. So back to the original idea of postgres importing the tsv files,
        BUT since it's so sensitive to the format, pre-process the tsv files here first.
    */

    private const int ONE_SECOND = 1_000;
    private const int ONE_MINUTE = ONE_SECOND * 60;
    private const int DEFAULT_CHUNK_SIZE = 100_000;

    private readonly int _chunkSize;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _cancellationToken;
    private readonly Timer _timer;
    private readonly ILogger<DailyUpdater> _logger;

    
    public DailyUpdater(IServiceScopeFactory serviceScopeFactory, ILogger<DailyUpdater> logger)
    {
        _cancellationToken = _cts.Token;

        try { _chunkSize = int.Parse(Environment.GetEnvironmentVariable("DB_CHUNK_SIZE")!); }
        catch { }
        if (_chunkSize <= 0)
            _chunkSize = DEFAULT_CHUNK_SIZE;

        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
        _timer = new(Tick);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer.Change(ONE_SECOND, Timeout.Infinite);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.Dispose();
        _cts.Cancel();
        return Task.CompletedTask;
    }

    private async void Tick(object? state)
    {
        _logger.LogDebug("Tick");

        bool shouldDoWork = false;

        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var config = await db.Config
                .AsNoTracking()
                .Where(_ => _.Id == 1)
                .FirstOrDefaultAsync(_cancellationToken) ?? new();


            //Schedule for same time daily - I'm picking 2 pm UTC out of thin air
            var nextRunTime = new DateTime(config.LastUpdate.Year, config.LastUpdate.Month, config.LastUpdate.Day, 14, 0, 0, DateTimeKind.Utc);
            if (config.LastUpdate > nextRunTime)
                nextRunTime = nextRunTime.AddDays(1);

            shouldDoWork = DateTime.UtcNow >= nextRunTime;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tick");
        }


#if DEBUG
        shouldDoWork = true;
#endif

        if (shouldDoWork)
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var config = await db.Config
                    .Where(_ => _.Id == 1)
                    .FirstOrDefaultAsync(_cancellationToken);

                config ??= db.Config.Add(new Config { Id = 1 }).Entity;
                config.LastUpdate = DateTime.UtcNow;
                await db.SaveChangesAsync(_cancellationToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed updating LastUpdate in database");
            }


            try
            {
                await DoWork();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DoWork failed");
            }
        }

       
        //Reset timer to tick again in 1 minute
        try { _timer.Change(ONE_MINUTE, Timeout.Infinite); }
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
            await StartUpdate<TitleBasic>();
            await UpdateTitleBasics();
            await UpdateSuccess<TitleBasic>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitleBasics) + " Failed");
            await UpdateError<TitleBasic>(ex);
        }


        try
        {
            await StartUpdate<TitleAka>();
            await UpdateTitleAkas();
            await UpdateSuccess<TitleAka>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitleAkas) + " Failed");
            await UpdateError<TitleAka>(ex);
        }

        try
        {
            await StartUpdate<TitleCrew>();
            await UpdateTitleCrew();
            await UpdateSuccess<TitleCrew>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitleCrew) + " Failed");
            await UpdateError<TitleCrew>(ex);
        }

        try
        {
            await StartUpdate<TitleEpisode>();
            await UpdateTitleEpisodes();
            await UpdateSuccess<TitleEpisode>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitleEpisodes) + " Failed");
            await UpdateError<TitleEpisode>(ex);
        }

        try
        {
            await StartUpdate<TitlePrincipal>();
            await UpdateTitlePrincipals();
            await UpdateSuccess<TitlePrincipal>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitlePrincipals) + " Failed");
            await UpdateError<TitlePrincipal>(ex);
        }

        try
        {
            await StartUpdate<TitleRating>();
            await UpdateTitleRatings();
            await UpdateSuccess<TitleRating>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateTitleRatings) + " Failed");
            await UpdateError<TitleRating>(ex);
        }

        try
        {
            await StartUpdate<NameBasic>();
            await UpdateNameBasics();
            await UpdateSuccess<NameBasic>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateNameBasics) + " Failed");
            await UpdateError<NameBasic>(ex);
        }
    }




    private Task UpdateTitleBasics()
    {
        const string URL = "https://datasets.imdbws.com/title.basics.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleBasics));

        return ImportFile(URL, fields => new TitleBasic
        {
            TConstId = fields[0].ToNumId(),
            TitleType = fields[1],
            PrimaryTitle = fields[2],
            OriginalTitle = fields[3],
            IsAdult = fields[4] != "0",
            StartYear = fields[5].TryGetUShort(),
            EndYear = fields[6].TryGetUShort(),
            RuntimeMinutes = fields[7].TryGetUShort(),
            Genres = fields[8].ToStringList()
        });
    }



    private Task UpdateTitleAkas()
    {
        const string URL = "https://datasets.imdbws.com/title.akas.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleAkas));

        return ImportFile(URL, fields => new TitleAka
        {
            TConstId = fields[0].ToNumId(),
            Ordering = ushort.Parse(fields[1]),
            TitleHashId = fields[2].Hash(),
            Title = fields[2],
            Region = fields[3] == "\\N" ? null : fields[3],
            Language = fields[4] == "\\N" ? null : fields[4],
            Types = fields[5].ToStringList(),
            Attributes = fields[6].ToStringList(),
            IsOriginalTitle = fields[7] == "1"
        });
    }



    private Task UpdateTitleCrew()
    {
        const string URL = "https://datasets.imdbws.com/title.crew.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleCrew));

        return ImportFile(URL, fields =>
        {
            var ret = new TitleCrew
            {
                TConstId = fields[0].ToNumId(),
                Directors = fields[1].ToStringList(),
                Writers = fields[2].ToStringList(),
            };
            if (ret.Directors.HasItems() || ret.Writers.HasItems())
                return ret;
            return null;
        });
    }



    private Task UpdateTitleEpisodes()
    {
        const string URL = "https://datasets.imdbws.com/title.episode.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleEpisodes));

        return ImportFile(URL, fields => new TitleEpisode
        {
            TConstId = fields[0].ToNumId(),
            ParentTConstId = fields[1].ToNumId(),
            SeasonNumber = fields[2].TryGetUShort(),
            EpisodeNumber = fields[3].TryGetUShort()
        });
    }



    private Task UpdateTitlePrincipals()
    {
        const string URL = "https://datasets.imdbws.com/title.principals.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitlePrincipals));

        return ImportFile(URL, fields => new TitlePrincipal
        {
            TConstId = fields[0].ToNumId(),
            Ordering = ushort.Parse(fields[1]),
            NConstId = fields[2].ToNumId(),
            Category = fields[3],
            Job = fields[4] == "\\N" ? null : fields[4],
            Character = fields[5] == "\\N" ? null : fields[5]
        });
    }



    private Task UpdateTitleRatings()
    {
        const string URL = "https://datasets.imdbws.com/title.ratings.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleRatings));

        return ImportFile(URL, fields => new TitleRating
        {
            TConstId = fields[0].ToNumId(),
            AverageWeighting = float.Parse(fields[1]),
            NumVotes = uint.Parse(fields[2])
        });
    }



    private Task UpdateNameBasics()
    {
        const string URL = "https://datasets.imdbws.com/name.basics.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateNameBasics));

        return ImportFile(URL, fields => new NameBasic
        {
            NConstId = fields[0].ToNumId(),
            PrimaryName = fields[1],
            BirthYear = fields[2].TryGetUShort(),
            DeathYear = fields[3].TryGetUShort(),
            PrimaryProfessions = fields[4].ToStringList(),
            KnownForTitles = fields[5].ToStringList(),
        });
    }







    private async Task StartUpdate<T>() where T : class
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tn = db.GetTableName<T>();
            var history = await db.UpdateHistories
                .Where(_ => _.TableName == tn)
                .FirstOrDefaultAsync(_cancellationToken) ??
                db.UpdateHistories.Add(new UpdateHistory { TableName = tn }).Entity;

            history.LastStarted = DateTime.UtcNow;
            history.LastFinished = null;
            history.Success = null;
            history.LastError = null;

            await db.SaveChangesAsync(_cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(StartUpdate) + ": " + typeof(T).Name);
        }
    }



    private async Task UpdateSuccess<T>() where T : class
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tn = db.GetTableName<T>();
            var history = await db.UpdateHistories
                .Where(_ => _.TableName == tn)
                .FirstOrDefaultAsync(_cancellationToken) ??
                db.UpdateHistories.Add(new UpdateHistory { TableName = tn }).Entity;

            history.LastStarted ??= DateTime.UtcNow;
            history.LastFinished = DateTime.UtcNow;
            history.Success = true;
            history.LastError = null;

            await db.SaveChangesAsync(_cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateSuccess) + ": " + typeof(T).Name);
        }
    }



    private async Task UpdateError<T>(Exception err) where T : class
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tn = db.GetTableName<T>();
            var history = await db.UpdateHistories
                .Where(_ => _.TableName == tn)
                .FirstOrDefaultAsync(_cancellationToken) ??
                db.UpdateHistories.Add(new UpdateHistory { TableName = tn }).Entity;

            history.LastStarted ??= DateTime.UtcNow;
            history.LastFinished = DateTime.UtcNow;
            history.Success = false;
            history.LastError = err.ToString();

            await db.SaveChangesAsync(_cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(UpdateError) + ": " + typeof(T).Name);
        }
    }



    private async Task ImportFile<T>(string url, Func<string[], T?> createEntity) where T : class
    {
        /*
            Quick note: The BulkInsert extension doesn't work with uint, so made id's ulong 
        */

        //Download
        var tmpFile = new FileInfo(Path.Combine("/tmp", Path.ChangeExtension(Path.GetFileName(url), null)));
        await DownloadFile(url, tmpFile);

        //Get the db context
        using var scope = _serviceScopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        //These are LONG running queries. Hence the scoped context
        db.Database.SetCommandTimeout(TimeSpan.FromDays(1));

        //Get info
        var tableName = db.GetTableName<T>();
        
        string sql = $"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}' ORDER BY ORDINAL_POSITION";
        var cols = db.Database.SqlQueryRaw<string>(sql).ToList();

        var pkCols = db.GetPrimaryKeyColumnNames<T>();
        var nkCols = cols.ToList();
        nkCols.RemoveAll(_ => pkCols.Contains(_));
        string pkColsStr = string.Join(", ", pkCols.Select(_ => $"\"{_}\""));


        //Create the staging table
        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS _staging", _cancellationToken);

        sql = @$"CREATE UNLOGGED TABLE _staging (LIKE ""{tableName}"")";
        await db.Database.ExecuteSqlRawAsync(sql, _cancellationToken);

        sql = $"ALTER TABLE _staging ADD PRIMARY KEY ({pkColsStr})";
        await db.Database.ExecuteSqlRawAsync(sql, _cancellationToken);


        //Upload to staging with no timeout
        var bc = new BulkConfig
        {
            BulkCopyTimeout = 0,
            CustomDestinationTableName = "_staging"
        };

        List<T> inserts = [];

        //Open the file to read
        using TextReader tr = new StreamReader(tmpFile.FullName);
        tr.ReadLine();
        string? line;
        while ((line = tr.ReadLine()) != null)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var entity = createEntity(line.Split('\t'));
            if (entity != null)
                inserts.Add(entity);

            //Don't use too much memory
            if(inserts.Count >= _chunkSize)
            {
                await db.BulkInsertAsync(inserts, bc, cancellationToken: _cancellationToken);
                inserts.Clear();
            }
        }

#if !DEBUG
        tmpFile.TryDelete();    
#endif


        //Any remaining inserts
        if (inserts.Count > 0)
            await db.BulkInsertAsync(inserts, bc, cancellationToken: _cancellationToken);

        
        /*
            MERGE INTO wines w
            USING new_wine_list s
            ON s.winename = w.winename
            WHEN NOT MATCHED BY TARGET THEN
                INSERT VALUES(s.winename, s.stock)
            WHEN MATCHED AND w.stock != s.stock THEN
                UPDATE SET stock = s.stock
            WHEN NOT MATCHED BY SOURCE THEN
                DELETE; 
        */

        //Merge
        StringBuilder sb = new();
        sb.AppendLine(@$"MERGE INTO ""{tableName}"" t");
        sb.AppendLine("USING _staging s");
        sb.AppendLine("ON " + string.Join(" AND ", pkCols.Select(_ => $"s.\"{_}\" = t.\"{_}\"")));
        sb.AppendLine("WHEN NOT MATCHED BY TARGET THEN");
        sb.AppendLine($"    INSERT VALUES({string.Join(", ", cols.Select(_ => $"s.\"{_}\""))})");
        sb.AppendLine("WHEN MATCHED THEN");
        sb.AppendLine($"    UPDATE SET {string.Join(", ", nkCols.Select(_ => $"\"{_}\" = s.\"{_}\""))}");
        sb.AppendLine("WHEN NOT MATCHED BY SOURCE THEN");
        sb.AppendLine("    DELETE;");

        await db.Database.ExecuteSqlRawAsync(sb.ToString(), _cancellationToken);

        await db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS _staging", _cancellationToken);
    }





    private async Task DownloadFile(string url, FileInfo outputFile)
    {
#if DEBUG
        if (outputFile.Exists)
            return;
#endif
        outputFile.TryDelete();

        var gzFile = new FileInfo(Path.Combine("/tmp", outputFile.Name + ".gz"));
        gzFile.TryDelete();

        using var scope = _serviceScopeFactory.CreateScope();
        {
            using var client = scope.ServiceProvider.GetRequiredService<HttpClient>();
            await client.DownloadFileAsync(url, gzFile, cancellationToken: _cancellationToken);
        }

        using var inputStream = File.OpenRead(gzFile.FullName);
        {
            using var outputStream = File.Create(outputFile.FullName);
            using var decompressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
            await decompressionStream.CopyToAsync(outputStream, _cancellationToken);
        }

        gzFile.TryDelete();
    }

}
