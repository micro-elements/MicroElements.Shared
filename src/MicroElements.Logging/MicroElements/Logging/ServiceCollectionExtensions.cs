using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MicroElements.Logging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddThrottlingLogging(this IServiceCollection services, Action<ThrottlingOptions>? configure = null)
        {
            services.ConfigureThrottling(configure ?? (options => options.CategoryName = "*"));
            services.Decorate<ILoggerFactory, ThrottlingLoggerFactory>();
            
            return services;
        }

        public static IServiceCollection ConfigureThrottling(this IServiceCollection services, Action<ThrottlingOptions> configure)
        {
            return services.Configure<ThrottlingOptions>(configure);
        }
        
        public static IServiceCollection ConfigureThrottling(this IServiceCollection services, string categoryFilter, Action<ThrottlingLoggerOptions>? configure = null)
        {
            services.ConfigureThrottling(options => options.ConfigureCategory(categoryFilter, configure));
            return services;
        }
    }
}