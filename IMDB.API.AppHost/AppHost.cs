using Aspire.Hosting;
using IMDB.API.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var env = builder.AddDockerComposeEnvironment("env");

var cache = builder.AddRedis("cache")
    .WithRedisInsight()
    .WithDataVolume("redis", false)
    .WithLifetime(ContainerLifetime.Persistent);


var seq = builder
    .AddSeq("seq")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithEnvironment("ACCEPT_EULA", "Y");


var postgresdb = builder
    .AddPostgres("postgres")
    .WithPgAdmin_MyVersion()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("imdb-dumps");


var privilegedApiKey = builder.AddParameter("privileged-api-key");

builder
    .AddProject<Projects.IMDB_API_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithReference(seq)
    .WaitFor(seq)
    .WithEnvironment("PRIVILEGED_API_KEY", privilegedApiKey);

builder.Build().Run();
