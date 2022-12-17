using System.Threading.Tasks;
using DisclosureParser.Api;
using MicroElements.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace MicroElements.Logging.Tests;

public class ThrottlingLoggerTests
{
    [Fact]
    public async Task ThrottlingLoggerTest()
    {
        var application = new WebApplicationFactory<SamplesProgram>()
            .WithWebHostBuilder(builder =>
            {
                // ... Configure test services
            });

        var client = application.CreateClient();

        var stringAsync = await client.GetStringAsync($"/LoggingSample/{nameof(LoggingSampleController.GetThrottlingMessage)}?data=123");
        
        //...
    }
}