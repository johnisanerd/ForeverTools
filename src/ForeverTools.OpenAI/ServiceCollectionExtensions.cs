using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ForeverTools.OpenAI;

/// <summary>Extension methods for registering the OpenAI client with dependency injection.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the OpenAI client to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="apiKey">Your OpenAI API key from https://platform.openai.com/api-keys</param>
    public static IServiceCollection AddForeverToolsOpenAI(this IServiceCollection services, string apiKey) =>
        services.AddForeverToolsOpenAI(options => options.ApiKey = apiKey);

    /// <summary>
    /// Adds the OpenAI client to the service collection using a configuration action.
    /// </summary>
    public static IServiceCollection AddForeverToolsOpenAI(
        this IServiceCollection services,
        Action<OpenAiOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<OpenAiClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            return new OpenAiClient(options);
        });
        return services;
    }

    /// <summary>
    /// Adds the OpenAI client to the service collection using IConfiguration.
    /// Reads from the "OpenAI" section of appsettings.json.
    /// </summary>
    public static IServiceCollection AddForeverToolsOpenAI(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OpenAiOptions>(configuration.GetSection(OpenAiOptions.SectionName));
        services.AddSingleton<OpenAiClient>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            return new OpenAiClient(options);
        });
        return services;
    }
}
