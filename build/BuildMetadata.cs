using System.Collections.Generic;
using System.Xml.Linq;

namespace MicroElements.Build
{
    /// <summary>
    /// Represents xml documentation for project.
    /// </summary>
    public interface IProjectXmlDocumentation
    {
        /// <summary>
        /// Xml documentation for project.
        /// </summary>
        XDocument Document { get; }
    }

    /// <summary>
    /// Represents project properties.
    /// </summary>
    public interface IProjectProperties
    {
        IReadOnlyDictionary<string, string> Properties { get; }
    }

    /// <summary>
    /// Represents xml documentation for project.
    /// </summary>
    public record ProjectXmlDocumentation(XDocument Document) : IProjectXmlDocumentation;

    /// <summary>
    /// Represents project properties.
    /// </summary>
    public record ProjectProperties(IReadOnlyDictionary<string, string> Properties) : IProjectProperties;
}
