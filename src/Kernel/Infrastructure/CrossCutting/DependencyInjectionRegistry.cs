using System.Globalization;
using System.Reflection;
using Aviant.Core.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
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
        foreach (IConfigurationSource source in configuration.Sources)
            ConfigurationWithDomainsBuilder.Sources.Add(source);

        return _configuration = configuration.Build();
    }

    public static IConfiguration GetDomainConfiguration(string domain)
    {
        if (CurrentEnvironment is null
         || ConfigurationBuilder is null)
            throw new InvalidOperationException(
                typeof(DependencyInjectionRegistry).FullName);

        ConfigurationBuilder configurationBuilder = new();

        foreach (IConfigurationSource source in ConfigurationBuilder.Sources)
            configurationBuilder.Sources.Add(source);

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
                    InsertDomainSource(ConfigurationWithDomainsBuilder.Sources, GetSource<JsonConfigurationSource>(configFile));
                    InsertDomainSource(configurationBuilder.Sources, GetSource<JsonConfigurationSource>(configFile));
                    break;

                case ConfigurationFormat.Yaml:
                case ConfigurationFormat.Yml:
                    InsertDomainSource(ConfigurationWithDomainsBuilder.Sources, GetSource<YamlConfigurationSource>(configFile));
                    InsertDomainSource(configurationBuilder.Sources, GetSource<YamlConfigurationSource>(configFile));
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
    }

    /// <summary>
    ///     Inserts a domain configuration file right after the last <c>appsettings*</c> file
    ///     already in <paramref name="sources" />, host or domain.
    /// </summary>
    /// <remarks>
    ///     A domain file overrides the host's appsettings files and any domain file loaded
    ///     before it, but everything the host adds after its appsettings keeps precedence:
    ///     user secrets, environment variables, command-line arguments, key vaults. Appending
    ///     it instead let a value hardcoded in a domain file beat the one supplied to the
    ///     container, so a deployed service silently connected to localhost.
    /// </remarks>
    internal static void InsertDomainSource(IList<IConfigurationSource> sources, IConfigurationSource source)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(source);

        var index = 0;

        for (var i = 0; i < sources.Count; i++)
            if (sources[i] is FileConfigurationSource { Path: { } path }
             && Path.GetFileName(path).StartsWith("appsettings", StringComparison.OrdinalIgnoreCase))
                index = i + 1;

        sources.Insert(index, source);
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
