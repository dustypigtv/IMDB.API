using System.ComponentModel.DataAnnotations;

namespace IMDB.API.ApiService.Data.Models;

public class UpdateHistory
{
    [Key]
    public required string TableName { get; set; }

    public DateTime? LastStarted { get; set; }

    public DateTime? LastFinished { get; set; }

    public bool? Success { get; set; }

    public string? LastError { get; set; }
}
