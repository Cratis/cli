// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_CritterStackScreenplayGeneration.given;

public class a_vogen_critter_stack_application_built_from_source : Specification
{
    protected const string ProjectName = "Ordering";

    static readonly string VogenMetadataSource = string.Join(
        '\n',
        [
            "[assembly: System.Reflection.AssemblyVersion(\"8.0.7.0\")]",
            "namespace Vogen",
            "{",
            "    public sealed class ValueObjectAttribute : System.Attribute",
            "    {",
            "        public ValueObjectAttribute(System.Type underlyingType) { }",
            "    }",
            "    public sealed class ValueObjectAttribute<T> : System.Attribute;",
            "}"
        ]);

    static readonly string FrameworkSource = string.Join(
        '\n',
        [
            "namespace Wolverine",
            "{",
            "    public sealed class WolverineOptions;",
            "}",
            "namespace Marten",
            "{",
            "    public sealed class StoreOptions;",
            "    public interface IDocumentSession",
            "    {",
            "        void Delete<T>(object id);",
            "        void Store<T>(T document);",
            "    }",
            "}"
        ]);

    static readonly string ApplicationSource = string.Join(
        '\n',
        [
            "namespace Ordering;",
            "[Vogen.ValueObject<System.Guid>]",
            "public partial struct OrderId;",
            "[Vogen.ValueObject(typeof(string))]",
            "public partial struct CustomerCode;",
            "public sealed record PlaceOrder(OrderId Id, CustomerCode Code);",
            "public sealed record Order(OrderId Id, CustomerCode Code);",
            "public static class PlaceOrderHandler",
            "{",
            "    public static void Handle(PlaceOrder command, Marten.IDocumentSession session)",
            "    {",
            "        session.Store(new Order(command.Id, command.Code));",
            "        session.Delete<Order>(command.Id);",
            "    }",
            "}"
        ]);

    protected LoadedCompilation Loaded { get; private set; } = null!;

    void Establish()
    {
        var frameworkTree = CSharpSyntaxTree.ParseText(FrameworkSource, path: "/workspace/Framework.cs");
        var applicationTree = CSharpSyntaxTree.ParseText(ApplicationSource, path: "/workspace/Ordering/Application.cs");
        var compilation = CSharpCompilation.Create(
            ProjectName,
            [frameworkTree, applicationTree],
            [.. TrustedPlatformReferences(), VogenMetadataReference()],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        Loaded = new([compilation], [ProjectName], [])
        {
            AuthoredSyntaxTrees = [new HashSet<SyntaxTree> { frameworkTree, applicationTree }]
        };
    }

    static MetadataReference VogenMetadataReference()
    {
        var compilation = CSharpCompilation.Create(
            "Vogen",
            [CSharpSyntaxTree.ParseText(VogenMetadataSource, path: "/metadata/Vogen.cs")],
            TrustedPlatformReferences(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new MemoryStream();
        compilation.Emit(stream).Success.ShouldBeTrue();

        return MetadataReference.CreateFromImage(stream.ToArray());
    }

    static IEnumerable<MetadataReference> TrustedPlatformReferences() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path));
}
