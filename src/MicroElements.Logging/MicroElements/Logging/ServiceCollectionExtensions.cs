using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MicroElements.Logging
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddThrottlingLogging(this IServiceCollection services, Action<ThrottlingOptions>? configure = null)
        {
            if (configure != null)
                services.Configure<ThrottlingOptions>(configure);
            
            services.Decorate<ILoggerFactory, ThrottlingLoggerFactory>();
            
            //services.Decorate<ILoggerFactory>(factory => factory.WithThrottling(configure));
            
            return services;
        }

        public static IServiceCollection ConfigureThrottling(this IServiceCollection services, Action<ThrottlingOptions>? configure = null)
        {
            if (configure != null)
                services.Configure<ThrottlingOptions>(configure);
            return services;
        }
        
        public static IServiceCollection ConfigureThrottling(this IServiceCollection services, string categoryFilter, Action<ThrottlingLoggerOptions>? configure = null)
        {
            services.ConfigureThrottling(options => options.ConfigureCategory(categoryFilter, configure));
            return services;
        }
    }
}