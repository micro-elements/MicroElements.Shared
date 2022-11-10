using MicroElements.Logging;
using Microsoft.AspNetCore.Mvc;

namespace MicroElements.Api
{
    [ApiController]
    [Route("[controller]")]
    public class LoggingSampleController : ControllerBase
    {
        [HttpGet("[action]")]
        public string GetThrottlingMessage(string data, [FromServices] ILogger<LoggingSampleController> logger)
        {
            logger.LogInformation($"Throttling message {data}");
            return data;
        }
        
        [HttpGet("[action]")]
        public string GetNotThrottledMessage(string data, [FromServices] ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("NotConfiguredCategory");
            logger.LogInformation($"NotConfiguredCategory message {data}");
            return data;
        }
        
        [HttpGet("[action]")]
        public string GetWithThrottlingInPlaceByFactory(string data, [FromServices] ILoggerFactory loggerFactory)
        {
            var loggerFactoryWithThrottling = loggerFactory
                .WithThrottling(throttlingOptions =>
                {
                    throttlingOptions.CategoryName = "*";
                    throttlingOptions.AppendMetricsToMessage = true;
                });
            
            var logger = loggerFactoryWithThrottling.CreateLogger(typeof(LoggingSampleController));
            logger.LogInformation($"GetWithThrottlingInPlaceByFactory {data}");
            return data;
        }
        
        [HttpGet("[action]")]
        public string GetWithThrottlingInPlaceByLogger(string data, [FromServices] ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger(typeof(LoggingSampleController));
            var loggerWithThrottling = logger.WithThrottling();
            loggerWithThrottling.LogInformation($"GetWithThrottlingInPlaceByLogger {data}");
            return data;
        }      
        
    }
}