using DustyPig.Utils;
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
    private const int ONE_DAY = ONE_SECOND * 60 * 60 * 24;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CancellationTokenSource _cts = new();
    private readonly CancellationToken _cancellationToken;
    private readonly Timer _timer;
    private readonly ILogger<DailyUpdater> _logger;

    
    public DailyUpdater(IServiceScopeFactory serviceScopeFactory, ILogger<DailyUpdater> logger)
    {
        _cancellationToken = _cts.Token;
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
                .FirstAsync(_cancellationToken);


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
        if (!_cancellationToken.IsCancellationRequested)
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


        if (!_cancellationToken.IsCancellationRequested)
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



        if (!_cancellationToken.IsCancellationRequested)
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




    private Task UpdateTitleBasics()
    {
        const string URL = "https://datasets.imdbws.com/title.basics.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleBasics));

        return ImportFile(URL, fields => new TitleBasic
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
        });
    }



    private Task UpdateTitleAkas()
    {
        const string URL = "https://datasets.imdbws.com/title.akas.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitleAkas));

        return ImportFile(URL, fields => new TitleAka
        {
            TConst = fields[0],
            Ordering = int.Parse(fields[1]),
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
                TConst = fields[0],
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
            TConst = fields[0],
            ParentTConst = fields[1],
            SeasonNumber = fields[2].TryGetInt(),
            EpisodeNumber = fields[3].TryGetInt()
        });
    }



    private Task UpdateTitlePrincipals()
    {
        const string URL = "https://datasets.imdbws.com/title.principals.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateTitlePrincipals));

        return ImportFile(URL, fields => new TitlePrincipal
        {
            TConst = fields[0],
            Ordering = int.Parse(fields[1]),
            NConst = fields[2],
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
            TConst = fields[0],
            AverageWeighting = float.Parse(fields[1]),
            NumVotes = int.Parse(fields[2])
        });
    }



    private Task UpdateNameBasics()
    {
        const string URL = "https://datasets.imdbws.com/name.basics.tsv.gz";

        _logger.LogInformation("Starting: " + nameof(UpdateNameBasics));

        return ImportFile(URL, fields => new NameBasic
        {
            NConst = fields[0],
            PrimaryName = fields[1],
            BirthYear = fields[2].TryGetInt(),
            DeathYear = fields[3].TryGetInt(),
            PrimaryProfessions = fields[4].ToStringList(),
            KnownForTitles = fields[5].ToStringList(),
        });
    }









    private async Task ImportFile<T>(string url, Func<string[], T?> createEntity) where T : class, ICSV
    {
        var tmpFile = new FileInfo(Path.Combine("/tmp", Path.ChangeExtension(Path.GetFileName(url), null)));
        await DownloadFile(url, tmpFile);

        var formattedFile = new FileInfo(Path.Combine("/tsvdata",  tmpFile.Name));
        formattedFile.TryDelete();


        using (TextReader tr = new StreamReader(tmpFile.FullName))
        {
            //Ignore source headers
            tr.ReadLine();

            using var dstStream = new StreamWriter(formattedFile.FullName);

            bool headersDone = false;
            string? line;
            while ((line = tr.ReadLine()) != null)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var entity = createEntity(line.Split('\t'));
                if (entity != null)
                {
                    if (!headersDone)
                    {
                        dstStream.WriteLine(entity.ToHeaders());
                        headersDone = true;
                    }

                    dstStream.WriteLine(entity.ToCSV());
                }
            }
        }


#if !DEBUG
        file.TryDelete();    
#endif


        using var scope = _serviceScopeFactory.CreateScope();
        using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tn = db.GetTableName<T>();
        var cols = db.GetColumnNames<T>();
        string colNames = string.Join(", ", cols.Select(_ => $"\"{_}\""));


        var pkCols = db.GetPrimaryKeyColumnNames<T>();
        var pkNames = string.Join(", ", pkCols.Select(_ => $"\"{_}\""));

        var nkCols = cols.ToList();
        nkCols.RemoveAll(_ => pkCols.Contains(_));

        StringBuilder sb = new();
        sb.AppendLine("BEGIN;");
        sb.AppendLine();

        sb.AppendLine(@$"DROP TABLE IF EXISTS ""STAGING"";");
        sb.AppendLine();

        sb.AppendLine(@$"CREATE TEMP TABLE ""STAGING"" (LIKE ""{tn}"");");
        sb.AppendLine();

        sb.AppendLine(@$"COPY ""STAGING"" FROM '{formattedFile.FullName}' DELIMITER ',' CSV HEADER;");
        sb.AppendLine();

        sb.AppendLine(@$"DELETE FROM ""{tn}"" WHERE ({pkNames}) NOT IN (SELECT {pkNames} FROM ""{tn}"");");
        sb.AppendLine();

        sb.AppendLine(@$"INSERT INTO ""{tn}"" ({colNames})");
        sb.AppendLine(@$"(SELECT {colNames} FROM ""STAGING"")");
        sb.AppendLine($@"ON CONFLICT ({pkNames}) DO UPDATE SET");
        sb.AppendLine(string.Join(", ", nkCols.Select(_ => $@"""{_}"" = EXCLUDED.""{_}""")) + ";");
        sb.AppendLine();

        sb.AppendLine(@$"DROP TABLE IF EXISTS ""STAGING"";");
        sb.AppendLine();

        sb.AppendLine("COMMIT;");

        await db.Database.ExecuteSqlRawAsync(sb.ToString(), _cancellationToken);
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
