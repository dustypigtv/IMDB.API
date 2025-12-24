var builder = DistributedApplication.CreateBuilder(args);

var env = builder.AddDockerComposeEnvironment("env");

var seq = builder
    .AddSeq("seq")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithEnvironment("ACCEPT_EULA", "Y");

var postgresdb = builder
    .AddPostgres("postgres")
    .WithPgAdmin()
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("imdb-dumps");

var apiService = builder
    .AddProject<Projects.IMDB_API_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(postgresdb)
    .WaitFor(postgresdb)
    .WithReference(seq)
    .WaitFor(seq);

builder.Build().Run();
