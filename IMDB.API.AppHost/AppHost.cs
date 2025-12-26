using Aspire.Hosting;
using IMDB.API.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

var env = builder.AddDockerComposeEnvironment("env");

var seq = builder
    .AddSeq("seq")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithEnvironment("ACCEPT_EULA", "Y");

var fileStore = builder.AddFileStore("tsvdata", "tsvdata");


var postgresdb = builder
    .AddPostgres("postgres")
    .WithFileStore(fileStore, "/tsvdata")
    .WithPgAdmin_MyVersion()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("imdb-dumps");


var privilegedApiKey = builder.AddParameter("privileged-api-key");

var apiService = builder
    .AddProject<Projects.IMDB_API_ApiService>("apiservice")
    .WithFileStore(fileStore)
    .WithHttpHealthCheck("/health")
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithReference(seq)
    .WaitFor(seq)
    .WithEnvironment("PRIVILEGED_API_KEY", privilegedApiKey);

builder.Build().Run();
