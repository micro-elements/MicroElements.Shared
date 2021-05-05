#region Suppressions

#pragma warning disable SA1633 // File should have header
#pragma warning disable SA1649 // File name should match first type name

// ReSharper disable once CheckNamespace
#endregion

// ****************************************************************//
// This is very limited subset of JetBrains Annotation attributes. //
// ****************************************************************//
namespace JetBrains.Annotations
{
    using System;

    /// <summary>
    /// Indicates that the function argument should be a string literal and match one
    /// of the parameters of the caller function. For example, ReSharper annotates
    /// the parameter of <see cref="ArgumentNullException"/>.
    /// </summary>
    /// <example><code>
    /// void Foo(string param) {
    ///   if (param == null)
    ///     throw new ArgumentNullException("par"); // Warning: Cannot resolve symbol
    /// }
    /// </code></example>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class InvokerParameterNameAttribute : Attribute { }

    /// <summary>
    /// Indicates that method is pure LINQ method, with postponed enumeration (like Enumerable.Select,
    /// .Where). This annotation allows inference of [InstantHandle] annotation for parameters
    /// of delegate type by analyzing LINQ method chains.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class LinqTunnelAttribute : Attribute { }
    
    /// <summary>
    /// Indicates that IEnumerable passed as a parameter is not enumerated.
    /// Use this annotation to suppress the 'Possible multiple enumeration of IEnumerable' inspection.
    /// </summary>
    /// <example><code>
    /// static void ThrowIfNull&lt;T&gt;([NoEnumeration] T v, string n) where T : class
    /// {
    ///   // custom check for null but no enumeration
    /// }
    /// 
    /// void Foo(IEnumerable&lt;string&gt; values)
    /// {
    ///   ThrowIfNull(values, nameof(values));
    ///   var x = values.ToList(); // No warnings about multiple enumeration
    /// }
    /// </code></example>
    [AttributeUsage(AttributeTargets.Parameter)]
    internal sealed class NoEnumerationAttribute : Attribute { }
}