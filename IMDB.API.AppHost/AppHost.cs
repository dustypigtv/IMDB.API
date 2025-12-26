using Aspire.Hosting;
using IMDB.API.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var env = builder.AddDockerComposeEnvironment("env");

var seq = builder
    .AddSeq("seq")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithEnvironment("ACCEPT_EULA", "Y");


var postgresdb = builder
    .AddPostgres("postgres")
    .WithPgAdmin_MyVersion()
    .WithDataVolume()
    .WithVolume("tsvdata", "/tsvdata", false)
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("imdb-dumps");


var privilegedApiKey = builder.AddParameter("privileged-api-key");

var apiService = builder
    .AddProject<Projects.IMDB_API_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithReference(seq)
    .WaitFor(seq)
    .WithEnvironment("PRIVILEGED_API_KEY", privilegedApiKey);

builder.Build().Run();
