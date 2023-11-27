using idempotency_filter.Idempotency;

internal static class StartupExtension
{
    internal static WebApplicationBuilder AddConfigurations(this WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();
        return builder;
    }

    internal static IServiceCollection AddConfigurationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CachingOptions>().BindConfiguration(nameof(CachingOptions)).ValidateDataAnnotations();
        services.AddDistributedMemoryCache();
        services.AddTransient<IdempotencyFilter>();
        return services;
    }
}