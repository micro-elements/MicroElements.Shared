using Microsoft.AspNetCore.Mvc;

namespace MicroElements.Samples.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoggingSampleController : ControllerBase
    {
        private readonly ILogger<LoggingSampleController> _logger;

        public LoggingSampleController(ILogger<LoggingSampleController> logger)
        {
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<string> GetData(string data)
        {
            _logger.LogInformation($"GetData {data}");
            return data;
        }
    }
}