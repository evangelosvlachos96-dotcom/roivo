using FluentAssertions;
using Roivo.Banking.Configuration;

namespace Roivo.Banking.Tests;

public sealed class DotEnvFileTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("roivo-dotenv-").FullName;

    [Fact]
    public void Read_MapsEnableBankingKeysToConfigurationPaths()
    {
        var path = WriteEnv(
            "ENABLE_BANKING_SANDBOX_URL=https://api.enablebanking.com",
            "ENABLE_BANKING_CLIENT_ID=app-123",
            "ENABLE_BANKING_PRIVATE_KEY_PATH=./enable-banking-key.pem");

        var values = DotEnvFile.Read(path);

        values["EnableBanking:BaseUrl"].Should().Be("https://api.enablebanking.com");
        values["EnableBanking:ApplicationId"].Should().Be("app-123");
        values["EnableBanking:PrivateKeyPath"].Should().Be("./enable-banking-key.pem");
    }

    [Fact]
    public void Read_IgnoresCommentsBlankLinesAndUnknownKeys()
    {
        var path = WriteEnv(
            "# a comment",
            "",
            "   ",
            "SOMETHING_ELSE=ignored",
            "ENABLE_BANKING_CLIENT_ID=app-123");

        var values = DotEnvFile.Read(path);

        values.Should().ContainSingle()
            .Which.Key.Should().Be("EnableBanking:ApplicationId");
    }

    [Theory]
    [InlineData("ENABLE_BANKING_CLIENT_ID=\"app-123\"")]
    [InlineData("ENABLE_BANKING_CLIENT_ID='app-123'")]
    [InlineData("ENABLE_BANKING_CLIENT_ID = app-123 ")]
    [InlineData("export ENABLE_BANKING_CLIENT_ID=app-123")]
    public void Read_StripsQuotesWhitespaceAndExportPrefix(string line)
    {
        var values = DotEnvFile.Read(WriteEnv(line));

        values["EnableBanking:ApplicationId"].Should().Be("app-123");
    }

    [Fact]
    public void Read_KeepsPrimaryKeyWhenAnAliasIsAlsoPresent()
    {
        var path = WriteEnv(
            "ENABLE_BANKING_SANDBOX_URL=https://sandbox.example",
            "ENABLE_BANKING_API_URL=https://production.example");

        DotEnvFile.Read(path)["EnableBanking:BaseUrl"].Should().Be("https://sandbox.example");
    }

    [Fact]
    public void Read_ReturnsEmptyWhenFileIsMissing()
    {
        DotEnvFile.Read(Path.Combine(_directory, "does-not-exist")).Should().BeEmpty();
    }

    [Fact]
    public void FindNearest_WalksUpFromANestedDirectory()
    {
        var envPath = WriteEnv("ENABLE_BANKING_CLIENT_ID=app-123");
        var nested = Directory.CreateDirectory(Path.Combine(_directory, "bin", "Debug", "net10.0")).FullName;

        DotEnvFile.FindNearest(nested).Should().Be(envPath);
    }

    [Fact]
    public void FindNearest_ReturnsNullWhenNothingIsFound()
    {
        DotEnvFile.FindNearest(_directory, ".env-that-does-not-exist").Should().BeNull();
    }

    private string WriteEnv(params string[] lines)
    {
        var path = Path.Combine(_directory, ".env");
        File.WriteAllLines(path, lines);
        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
