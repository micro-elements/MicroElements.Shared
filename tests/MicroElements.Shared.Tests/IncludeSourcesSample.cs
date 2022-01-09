using System;

namespace MicroElements.Shared.Tests
{
    //MicroElements.Collections.Extensions.NotNull
    //MicroElements.Collections.Extensions.Iterate

    // namespace           MicroElements.Collections.Extensions
    // public|internal     internal
    // partial             partial


    [IncludeSources(Package = "MicroElements.Collections.Sources", Include = "MicroElements.Collections.Extensions.NotNull")]
    internal partial class CollectionsExtensions
    {
    }

    public class IncludeSourcesAttribute : Attribute
    {
        public string Package { get; set; }
        public string Include { get; set; }
    }
}
