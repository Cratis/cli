// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.given;

public class a_marten_application_built_from_source : Specification
{
    protected const string ProjectName = "Banking";

    static readonly string Source = string.Join(
        '\n',
        [
            "namespace Marten",
            "{",
            "    public interface IDocumentStore;",
            "    public class StoreOptions",
            "    {",
            "        public Marten.Events.Projections.ProjectionOptions Projections { get; } = new();",
            "    }",
            "}",
            "namespace Marten.Events.Projections",
            "{",
            "    public enum SnapshotLifecycle { Inline }",
            "    public class ProjectionOptions",
            "    {",
            "        public void Snapshot<T>(SnapshotLifecycle lifecycle) { }",
            "    }",
            "}",
            "namespace Banking",
            "{",
            "    public record AccountOpened(System.Guid AccountId);",
            "    public class Account",
            "    {",
            "        public System.Guid Id { get; set; }",
            "        public void Apply(AccountOpened opened) { }",
            "    }",
            "    public static class Configuration",
            "    {",
            "        public static void Configure(Marten.StoreOptions options) =>",
            "            options.Projections.Snapshot<Account>(Marten.Events.Projections.SnapshotLifecycle.Inline);",
            "    }",
            "}"
        ]);

    protected LoadedCompilation Loaded { get; private set; } = null!;

    void Establish()
    {
        var compilation = CSharpCompilation.Create(
            ProjectName,
            [CSharpSyntaxTree.ParseText(Source, path: "/workspace/Banking/Account.cs")],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Loaded = new([compilation], [ProjectName], [])
        {
            AuthoredSyntaxTrees = [compilation.SyntaxTrees.ToHashSet()]
        };
    }

    static IEnumerable<MetadataReference> References() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(_ => MetadataReference.CreateFromFile(_));
}
