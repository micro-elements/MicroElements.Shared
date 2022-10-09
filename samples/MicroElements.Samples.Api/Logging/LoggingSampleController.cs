using MicroElements.Logging;
using Microsoft.AspNetCore.Mvc;

namespace MicroElements.Samples.Api.Logging
{
    [ApiController]
    [Route("[controller]")]
    public class LoggingSampleController : ControllerBase
    {
        [HttpGet("[action]")]
        public string GetData(string data, [FromServices] ILogger<LoggingSampleController> logger)
        {
            logger.LogInformation($"GetData {data}");
            return data;
        }
        
        [HttpGet("[action]")]
        public string GetData2(string data, [FromServices] ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger("NotConfiguredCategory");
            logger.LogInformation($"GetData2 {data}");
            return data;
        }
        
        [HttpGet("[action]")]
        public string GetData3(string data, [FromServices] ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory
                .WithThrottling(options =>
                {
                    options.AppendMetricsToMessage = true;
                    options.ThrottleCategory("*");
                })
                .CreateLogger(typeof(LoggingSampleController));
            logger.LogInformation($"GetData3 {data}");
            return data;
        }
        
        [HttpGet("[action]")]
        public string GetData4(string data, [FromServices] ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.WithThrottlingInPlace().CreateLogger<LoggingSampleController>();
            logger.LogInformation($"GetData3 {data}");
            return data;
        }      
        
    }
}