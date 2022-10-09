using FluentAssertions;
using MicroElements.Collections.Extensions.WildCard;
using Xunit;

namespace MicroElements.Shared.Tests.Collections;

public class WildcardTests
{
    [Fact]
    public void WildcardInclude()
    {
        string[] values = { "Microsoft.Extension.Logging", "Microsoft.AspNetCore.Hosting", "FluentAssertions", "System.Collections.Generic" };
        string[] includePatterns = { "Microsoft.AspNetCore.*", "*.Collections.*" };
        string[] result = { "Microsoft.AspNetCore.Hosting", "System.Collections.Generic" };
        values.IncludeByWildcardPatterns(includePatterns).Should().BeEquivalentTo(result);
    }

    [Fact]
    public void WildcardExclude()
    {
        string[] values = { "Microsoft.Extension.Logging", "Microsoft.AspNetCore.Hosting", "FluentAssertions", "System.Collections.Generic" };
        string[] excludePatterns =  { "Microsoft.AspNetCore.*", "*.Collections.*" };
        string[] result = { "Microsoft.Extension.Logging", "FluentAssertions" };
        values.ExcludeByWildcardPatterns(excludePatterns).Should().BeEquivalentTo(result);
    }
}