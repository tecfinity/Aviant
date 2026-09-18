using Microsoft.Extensions.DependencyInjection;

namespace Aviant.Presentation.AspNetCore;

public static class ProblemDetailsServiceCollectionExtensions
{
    /// <summary>
    ///     Registers problem details and <see cref="AviantExceptionHandler" />. Add <c>app.UseExceptionHandler()</c>
    ///     to the pipeline.
    /// </summary>
    public static IServiceCollection AddAviantProblemDetails(
        this IServiceCollection                 services,
        Action<AviantProblemDetailsOptions>? configure = null)
    {
        services.AddProblemDetails();
        services.AddOptions<AviantProblemDetailsOptions>().Configure(options => configure?.Invoke(options));
        services.AddExceptionHandler<AviantExceptionHandler>();

        return services;
    }
}
