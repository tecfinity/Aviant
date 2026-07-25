using System.Globalization;
using System.Reflection;
using Aviant.Core.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration.Json;
using NetEscapades.Configuration.Yaml;

namespace Aviant.Infrastructure.CrossCutting;

public static class DependencyInjectionRegistry
{
    private static IConfiguration? _configuration;

    public static IConfiguration DefaultConfiguration =>
        _configuration
     ?? ServiceLocator.ServiceContainer.GetRequiredService<IConfiguration>(
            typeof(IConfiguration));

    private static IConfigurationBuilder ConfigurationWithDomainsBuilder { get; } = new ConfigurationBuilder();

    public static IConfiguration ConfigurationWithDomains =>
        ConfigurationWithDomainsBuilder.Build()
     ?? throw new NullReferenceException(typeof(DependencyInjectionRegistry).FullName);

    public static IWebHostEnvironment? CurrentEnvironment { get; set; }

    public static IConfigurationBuilder? ConfigurationBuilder { get; set; }

    public static IConfiguration SetConfiguration(IConfigurationBuilder configuration)
    {
        ((List<IConfigurationSource>)ConfigurationWithDomainsBuilder.Sources)
           .AddRange(configuration.Sources);

        return _configuration = configuration.Build();
    }

    public static IConfiguration GetDomainConfiguration(string domain)
    {
        if (CurrentEnvironment is null
         || ConfigurationBuilder is null)
            throw new InvalidOperationException(
                typeof(DependencyInjectionRegistry).FullName);

        ConfigurationBuilder configurationBuilder = new();

        ((List<IConfigurationSource>)configurationBuilder.Sources).AddRange(ConfigurationBuilder.Sources);

        // JSON does not override any other format
        LoadConfiguration(
            configurationBuilder,
            domain,
            CurrentEnvironment.EnvironmentName,
            ConfigurationFormat.Json);

        // YML overrides JSON
        LoadConfiguration(
            configurationBuilder,
            domain,
            CurrentEnvironment.EnvironmentName,
            ConfigurationFormat.Yml);

        // YAML overrides both YML and JSON
        LoadConfiguration(
            configurationBuilder,
            domain,
            CurrentEnvironment.EnvironmentName,
            ConfigurationFormat.Yaml);

        // Domain files are appended after the host's sources, which puts them ahead of
        // environment variables — so a connection string hardcoded in a domain YAML
        // would silently beat the one supplied to the container. Re-rank the
        // environment so deployment configuration wins, matching the host's own order.
        List<IConfigurationSource> sources = (List<IConfigurationSource>)configurationBuilder.Sources;

        List<IConfigurationSource> environmentSources =
            [.. sources.Where(source => source is EnvironmentVariablesConfigurationSource)];

        foreach (IConfigurationSource source in environmentSources)
        {
            sources.Remove(source);
            sources.Add(source);
        }

        return configurationBuilder.Build();
    }

    private static void LoadConfiguration(
        IConfigurationBuilder configurationBuilder,
        string                domain,
        string                environment,
        ConfigurationFormat   format)
    {
        string[] configFiles =
        {
            @$"appsettings.{domain}.{Enum
               .GetName(typeof(ConfigurationFormat), format)?.ToLower(CultureInfo.InvariantCulture)}",
            @$"appsettings.{domain}.{environment}.{Enum
               .GetName(typeof(ConfigurationFormat), format)?.ToLower(CultureInfo.InvariantCulture)}"
        };

        foreach (var configFile in configFiles.Where(ConfigurationExists))
            switch (format)
            {
                case ConfigurationFormat.Json:
                    ConfigurationWithDomainsBuilder?.Sources
                       .Add(GetSource<JsonConfigurationSource>(configFile));

                    configurationBuilder.Sources
                       .Add(GetSource<JsonConfigurationSource>(configFile));
                    break;

                case ConfigurationFormat.Yaml:
                case ConfigurationFormat.Yml:
                    ConfigurationWithDomainsBuilder?.Sources
                       .Add(GetSource<YamlConfigurationSource>(configFile));

                    configurationBuilder.Sources
                       .Add(GetSource<YamlConfigurationSource>(configFile));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
    }

    private static bool ConfigurationExists(string configFileName) =>
        File.Exists(
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
             ?? throw new NullReferenceException(Assembly.GetExecutingAssembly().FullName),
                configFileName));

    private static TConfigurationSource GetSource<TConfigurationSource>(string configFileName)
        where TConfigurationSource : FileConfigurationSource, new()
    {
        TConfigurationSource configurationSource = new()
        {
            Path           = configFileName,
            ReloadOnChange = true,
            Optional       = false
        };
        configurationSource.ResolveFileProvider();

        return configurationSource;
    }
}
