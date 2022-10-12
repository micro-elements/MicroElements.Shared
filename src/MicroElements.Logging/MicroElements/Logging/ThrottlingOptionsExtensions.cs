using System;
using System.Linq;
using MicroElements.Collections.Extensions.WildCard;

namespace MicroElements.Logging
{
    public static class ThrottlingOptionsExtensions
    {
        public static ThrottlingOptions ConfigureDefault(this ThrottlingOptions throttlingOptions, Action<ThrottlingLoggerOptions>? configure = null)
        {
            var categoryOptions = throttlingOptions.Default;
            configure?.Invoke(categoryOptions);
            return throttlingOptions;
        }
        
        public static ThrottlingOptions ThrottleCategory(
            this ThrottlingOptions throttlingOptions, 
            string category,
            Action<ThrottlingLoggerOptions>? configure = null)
        {
            return ConfigureCategory(throttlingOptions, category, configure);
        }

        public static ThrottlingOptions ConfigureCategory(this ThrottlingOptions throttlingOptions, string category, Action<ThrottlingLoggerOptions>? configure = null)
        {
            var categoryOptions = throttlingOptions.GetOrAddByName(category, c => new ThrottlingLoggerOptions { CategoryName = c });
            configure?.Invoke(categoryOptions);
            return throttlingOptions;
        }
        
        public static ThrottlingLoggerOptions GetBestMatchDefined(this ThrottlingOptions throttlingOptions, string categoryName)
        {
            var bestMatch = throttlingOptions.GetBestMatch(categoryName);
            var defaultValues = ThrottlingLoggerOptions.GetDefaultValues();
            var defined = bestMatch.Combine(throttlingOptions.Default).Combine(defaultValues);
            return defined;
        }
        
        public static ThrottlingLoggerOptions? GetByName(this ThrottlingOptions throttlingOptions, string categoryName)
        {
            return throttlingOptions.Categories.FirstOrDefault(options => options.CategoryName == categoryName);
        }

        public static ThrottlingLoggerOptions GetOrAddByName(this ThrottlingOptions throttlingOptions, string categoryName, Func<string, ThrottlingLoggerOptions> factory)
        {
            var loggerOptions = throttlingOptions.GetByName(categoryName);
            if (loggerOptions == null)
            {
                loggerOptions = factory(categoryName);
                throttlingOptions.Categories.Add(loggerOptions);
            }

            return loggerOptions;
        }

        public static ThrottlingLoggerOptions? GetBestMatch(this ThrottlingOptions throttlingOptions, string categoryName)
        {
            ThrottlingLoggerOptions? loggerOptions = throttlingOptions.GetByName(categoryName);
            
            if (loggerOptions is null)
            {
                var matchedCategories = throttlingOptions.Categories
                    .Where(options => options.CategoryName != null && categoryName.IsMatchesWildcard(options.CategoryName))
                    .OrderByDescending(options => options.CategoryName!.Length)
                    .ToArray();

                if (matchedCategories.Length > 0)
                {
                    loggerOptions = matchedCategories[0];
                }
            }
            
            if (loggerOptions is null)
            {
                if (throttlingOptions.Default.CategoryName is {} defaultFilter && categoryName.IsMatchesWildcard(defaultFilter))
                {
                    loggerOptions = throttlingOptions.Default;
                }
            }
            
            return loggerOptions;
        }
        
        public static ThrottlingLoggerOptions Combine(this ThrottlingLoggerOptions options1, ThrottlingLoggerOptions options2)
        {
            return new ThrottlingLoggerOptions
            {
                CategoryName = options1.CategoryName ?? options2.CategoryName,
                GetMessageKey = options1.GetMessageKey ?? options2.GetMessageKey,
                ShouldWrite = options1.ShouldWrite ?? options2.ShouldWrite,
                MaxMessagesForCategory = options1.MaxMessagesForCategory ?? options2.MaxMessagesForCategory,
                ThrottlingPeriod = options1.ThrottlingPeriod ?? options2.ThrottlingPeriod,
                AppendMetricsToMessage = options1.AppendMetricsToMessage ?? options2.AppendMetricsToMessage,
                AppendMetricsToScope = options1.AppendMetricsToScope ?? options2.AppendMetricsToScope,
            };
        }
        
        public static ThrottlingLoggerOptions Set(this ThrottlingLoggerOptions options1, ThrottlingLoggerOptions options2)
        {
            options1.CategoryName = options2.CategoryName;
            options1.GetMessageKey = options2.GetMessageKey;
            options1.ShouldWrite = options2.ShouldWrite;
            options1.MaxMessagesForCategory = options2.MaxMessagesForCategory;
            options1.ThrottlingPeriod = options2.ThrottlingPeriod;
            options1.AppendMetricsToMessage = options2.AppendMetricsToMessage;
            options1.AppendMetricsToScope = options2.AppendMetricsToScope;

            return options1;
        }       
        
    }
}