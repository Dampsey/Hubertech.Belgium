using System.Reflection;

namespace Hubertech.Belgium.Tests;

/// <summary>
/// Guards the promises made by the package itself, independently of any feature.
/// </summary>
public sealed class PackageConstraintsTests
{
    private static readonly Assembly Library = Assembly.Load("Hubertech.Belgium");

    [Fact]
    public void Library_references_only_the_base_class_library()
    {
        var foreignReferences = Library.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name is not "System" && name?.StartsWith("System.", StringComparison.Ordinal) != true);

        Assert.Empty(foreignReferences);
    }

    [Fact]
    public void Library_is_marked_as_trimmable()
    {
        var metadata = Library.GetCustomAttributes<AssemblyMetadataAttribute>();

        Assert.Contains(metadata, attribute => attribute is { Key: "IsTrimmable", Value: "True" });
    }
}
