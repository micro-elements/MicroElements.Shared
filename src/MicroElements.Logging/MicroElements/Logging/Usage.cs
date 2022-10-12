using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MicroElements.Logging
{
    public static class Usage
    {
        public static void Use()
        {
            ILoggerFactory loggerFactory = NullLoggerFactory.Instance.WithThrottling();

            ILogger logger = NullLogger.Instance.WithThrottling();
        }
    }
}