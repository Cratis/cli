// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.Fixtures;

/// <summary>
/// A repository with a library, its test project and a coverage map committed at the first commit.
/// </summary>
public sealed class PlannerRepository : IDisposable
{
    public const string Library = "Source/Foo/Foo.csproj";

    public PlannerRepository()
    {
        Repository = new TemporaryRepository();
        Repository.Write("Source/Foo/Foo.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Repository.Write("Source/Foo/A.cs", "namespace Foo; public class A { }");
        Repository.Write("Source/Foo/B.cs", "namespace Foo; public class B { }");
        Repository.Write(Maps.TestProject, "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"..\\Foo\\Foo.csproj\" /><PackageReference Include=\"xunit.v3\" /></ItemGroup></Project>");
        Repository.Write("Source/Foo.Specs/A.cs", "namespace Foo.Specs; public class when_a { }");
        Repository.Write("Source/Foo.Specs/B.cs", "namespace Foo.Specs; public class when_b { }");
        Repository.Write("Source/Other/Other.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\" />");
        Repository.Write("Source/Other.Specs/Other.Specs.csproj", "<Project Sdk=\"Microsoft.NET.Sdk\"><ItemGroup><ProjectReference Include=\"..\\Other\\Other.csproj\" /><PackageReference Include=\"xunit.v3\" /></ItemGroup></Project>");
        BaseCommit = Repository.Commit("base");
    }

    public TemporaryRepository Repository { get; }

    public string BaseCommit { get; }

    public IReadOnlyList<string> Units { get; } = ["Foo.Specs.when_a", "Foo.Specs.when_b"];

    public string Branch(string name, Action change)
    {
        Repository.Git("checkout", "-q", "-b", name, BaseCommit);
        change();
        return Repository.Commit(name);
    }

    /// <summary>
    /// Writes a coverage map built at the given commit, or the given text in its place.
    /// </summary>
    /// <param name="builtAt">The commit the map claims to be built at.</param>
    /// <param name="text">Text to write instead of a valid map.</param>
    public void WriteMap(string builtAt, string? text = null)
    {
        var map = Maps.For(
            builtAt,
            ("Foo.Specs.when_a", ["Source/Foo/A.cs"]),
            ("Foo.Specs.when_b", ["Source/Foo/B.cs"]));
        Repository.Write(CoverageMapFile.PathFor(Maps.TestProject), text ?? CoverageMapFile.Serialize(map));
    }

    public void Dispose() => Repository.Dispose();
}
