using Microsoft.Extensions.Configuration;

namespace Roivo.Banking.Configuration;

/// <summary>
/// Reads the Enable Banking secrets out of a local <c>.env</c> file and maps
/// them onto configuration keys.
/// </summary>
/// <remarks>
/// Enable Banking hands out credentials as shell-style environment variables,
/// and <c>.env</c> is gitignored — so this is where they live in development.
/// In a deployed environment nothing needs the file: the same three keys can
/// be set as real environment variables
/// (<c>EnableBanking__ApplicationId</c> and friends) and this loader finds
/// nothing to add.
/// </remarks>
public static class DotEnvFile
{
    /// <summary>Maps <c>.env</c> variable names to configuration paths.</summary>
    private static readonly (string EnvKey, string ConfigKey)[] KeyMap =
    [
        ("ENABLE_BANKING_SANDBOX_URL", "EnableBanking:BaseUrl"),
        ("ENABLE_BANKING_API_URL", "EnableBanking:BaseUrl"),
        ("ENABLE_BANKING_CLIENT_ID", "EnableBanking:ApplicationId"),
        ("ENABLE_BANKING_APPLICATION_ID", "EnableBanking:ApplicationId"),
        ("ENABLE_BANKING_PRIVATE_KEY_PATH", "EnableBanking:PrivateKeyPath"),
        ("ENABLE_BANKING_REDIRECT_URL", "EnableBanking:RedirectUrl"),
    ];

    /// <summary>
    /// Adds the mapped values from <paramref name="path"/> to
    /// <paramref name="builder"/>. A missing file is not an error — the values
    /// may legitimately come from real environment variables instead.
    /// </summary>
    public static IConfigurationBuilder AddDotEnvFile(this IConfigurationBuilder builder, string path)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(path);

        var values = Read(path);
        if (values.Count > 0)
            builder.AddInMemoryCollection(values!);

        return builder;
    }

    /// <summary>
    /// Parses <paramref name="path"/> and returns the configuration entries it
    /// contributes. Returns an empty dictionary when the file is absent.
    /// </summary>
    public static IDictionary<string, string?> Read(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        var result = new Dictionary<string, string?>(StringComparer.Ordinal);
        if (!File.Exists(path))
            return result;

        var raw = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
                continue;

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = trimmed[..separator].Trim();
            // "export FOO=bar" is legal in a .env consumed by a shell.
            if (key.StartsWith("export ", StringComparison.Ordinal))
                key = key["export ".Length..].Trim();

            var value = trimmed[(separator + 1)..].Trim();
            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            if (key.Length > 0)
                raw[key] = value;
        }

        foreach (var (envKey, configKey) in KeyMap)
        {
            // First mapping wins, so an alias never overwrites the primary key.
            if (raw.TryGetValue(envKey, out var value)
                && !string.IsNullOrWhiteSpace(value)
                && !result.ContainsKey(configKey))
            {
                result[configKey] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> looking for a
    /// <c>.env</c>. Web runs from the project directory and tests from a
    /// <c>bin/Debug/netX</c> folder, so neither can hard-code the path.
    /// </summary>
    public static string? FindNearest(string startDirectory, string fileName = ".env", int maxDepth = 8)
    {
        ArgumentNullException.ThrowIfNull(startDirectory);
        ArgumentNullException.ThrowIfNull(fileName);

        var dir = new DirectoryInfo(startDirectory);
        for (var depth = 0; dir is not null && depth < maxDepth; depth++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, fileName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }
}
