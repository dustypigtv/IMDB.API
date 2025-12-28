using IMDB.API.ApiService;
using IMDB.API.ApiService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.AddRedisOutputCache("cache");
builder.AddSeqEndpoint("seq");
builder.AddNpgsqlDbContext<AppDbContext>("imdb-dumps", settings => { }, options =>
{
#if DEBUG
    options.EnableSensitiveDataLogging(true);
    options.EnableDetailedErrors(true);
#endif   
});



// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddControllers();


// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


builder.Services.AddHostedService<DailyUpdater>();

var app = builder.Build();


//Apply any migrations
using (var scope = app.Services.CreateScope())
{
    using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}



// Configure the HTTP request pipeline.
app.UseExceptionHandler();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "IMDB Dumps API");
    options.RoutePrefix = string.Empty;
});



app.UseOutputCache();

app.MapDefaultEndpoints();
app.MapControllers();

app.Run();
