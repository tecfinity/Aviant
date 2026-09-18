using Aviant.Infrastructure.CrossCutting;
using AwesomeAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.CommandLine;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.FileProviders;
using NetEscapades.Configuration.Yaml;
using Xunit;

namespace Aviant.Tests.Kernel.Unit.CrossCutting;

public sealed class DomainConfigurationOrderTests
{
    [Fact]
    public void DomainFileRanksAboveHostAppSettingsAndBelowEverythingAddedAfterThem()
    {
        List<IConfigurationSource> sources =
        [
            Json("appsettings.json"),
            Json("appsettings.Production.json"),
            Json("/home/app/.microsoft/usersecrets/abc/secrets.json"),
            new EnvironmentVariablesConfigurationSource(),
            new CommandLineConfigurationSource()
        ];
        var domain = Yaml("appsettings.blog.yaml");

        DependencyInjectionRegistry.InsertDomainSource(sources, domain);

        sources.IndexOf(domain).Should().Be(2);
        sources[3].Should().BeOfType<JsonConfigurationSource>().Which.Path.Should().EndWith("secrets.json");
        sources[4].Should().BeOfType<EnvironmentVariablesConfigurationSource>();
        sources[5].Should().BeOfType<CommandLineConfigurationSource>();
    }

    [Fact]
    public void SuccessiveDomainFilesKeepTheirRelativeOrder()
    {
        List<IConfigurationSource> sources = [Json("appsettings.json"), new EnvironmentVariablesConfigurationSource()];
        var general     = Yaml("appsettings.blog.yaml");
        var environment = Yaml("appsettings.blog.Production.yaml");

        DependencyInjectionRegistry.InsertDomainSource(sources, general);
        DependencyInjectionRegistry.InsertDomainSource(sources, environment);

        sources.Should().HaveCount(4);
        sources.IndexOf(general).Should().Be(1);
        sources.IndexOf(environment).Should().Be(2);
        sources[3].Should().BeOfType<EnvironmentVariablesConfigurationSource>();
    }

    [Fact]
    public void WithoutHostAppSettingsTheDomainFileHasTheLowestRank()
    {
        List<IConfigurationSource> sources = [new EnvironmentVariablesConfigurationSource()];
        var domain = Yaml("appsettings.blog.yaml");

        DependencyInjectionRegistry.InsertDomainSource(sources, domain);

        sources.IndexOf(domain).Should().Be(0);
    }

    [Fact]
    public void GetDomainConfigurationLetsLaterHostSourcesOverrideTheDomainFile()
    {
        var directory = AppContext.BaseDirectory;
        File.WriteAllText(Path.Combine(directory, "appsettings.orderingtest.yaml"), "Key: domain\nDomainOnly: from-domain\n");
        File.WriteAllText(Path.Combine(directory, "appsettings.json"), """{ "Key": "host-appsettings" }""");

        var host = new ConfigurationBuilder()
           .SetBasePath(directory)
           .AddJsonFile("appsettings.json", optional: false)
           .AddInMemoryCollection(new Dictionary<string, string?> { ["Key"] = "deployment" });

        DependencyInjectionRegistry.CurrentEnvironment   = new TestEnvironment(directory);
        DependencyInjectionRegistry.ConfigurationBuilder = host;

        var configuration = DependencyInjectionRegistry.GetDomainConfiguration("orderingtest");

        configuration["Key"].Should().Be("deployment", "configuration supplied after appsettings (secrets, environment) must win");
        configuration["DomainOnly"].Should().Be("from-domain");
    }

    private static JsonConfigurationSource Json(string path) => new() { Path = path, Optional = true };

    private static YamlConfigurationSource Yaml(string path) => new() { Path = path, Optional = true };

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = "Aviant.Tests";

        public string WebRootPath { get; set; } = root;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string ContentRootPath { get; set; } = root;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
