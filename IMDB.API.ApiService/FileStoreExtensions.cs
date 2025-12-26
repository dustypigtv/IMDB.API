using Microsoft.EntityFrameworkCore;

namespace IMDB.API.ApiService;

public static class FileStoreExtensions
{
    public static void AddFileStore(this IHostApplicationBuilder builder, string connectionName)
    {
        if(builder.Configuration.GetConnectionString(connectionName) is string path)
            builder.Services.AddKeyedSingleton(connectionName, new FileStore { Path = path });
    }
}
