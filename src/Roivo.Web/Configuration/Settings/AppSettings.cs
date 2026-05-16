using System.ComponentModel.DataAnnotations;

namespace Roivo.Web.Configuration.Settings;

public class AppSettings
{
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string LogFilePath { get; init; }
}
