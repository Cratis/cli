// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ArcScreenplayGeneration.given;

/// <summary>
/// Builds an application from source, so that generating from it runs the real generator rather than a substitute.
/// </summary>
/// <remarks>
/// Everything the CLI knows about the <c>Cratis.Arc.Screenplay</c> generator and the <c>Cratis.Screenplay</c>
/// compiler it holds through package references, and a mismatched pair of the two compiles clean and specs green
/// and then throws <see cref="MissingMethodException"/> the first time a document is generated for real — the
/// generator is built against the compiler's syntax types, and those are positional records whose constructors
/// change shape between major versions. Nothing short of generating catches that.
/// <para>
/// The source declares the two attributes the generator looks for rather than referencing Arc and Chronicle to get
/// them. The generator resolves them by full name, so declaring them is enough to describe an application, and it
/// keeps the compilation to one syntax tree and no package restore.
/// </para>
/// </remarks>
public class an_application_built_from_source : Specification
{
    /// <summary>
    /// The name the compilation, and therefore the project the document is generated from, goes by.
    /// </summary>
    protected const string ProjectName = "Bookshop";

    const string Source =
        "namespace Cratis.Arc.Commands.ModelBound { public class CommandAttribute : System.Attribute { } }\n" +
        "namespace Cratis.Chronicle.Events { public class EventTypeAttribute : System.Attribute { } }\n" +
        "\n" +
        "namespace Bookshop.Lending.Reserving\n" +
        "{\n" +
        "    [Cratis.Chronicle.Events.EventType]\n" +
        "    public record BookReserved(string MemberId);\n" +
        "\n" +
        "    [Cratis.Arc.Commands.ModelBound.Command]\n" +
        "    public record ReserveBook(string BookId);\n" +
        "}\n";

    /// <summary>
    /// Gets what loading the application would have produced.
    /// </summary>
    protected LoadedCompilation Loaded { get; private set; }

    void Establish() => Loaded = new(
        [CSharpCompilation.Create(
            ProjectName,
            [CSharpSyntaxTree.ParseText(Source)],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))],
        [ProjectName],
        []);

    /// <summary>
    /// Gets the references the source needs to compile.
    /// </summary>
    /// <returns>The <see cref="MetadataReference"/> for every assembly this process was started with.</returns>
    /// <remarks>
    /// A compilation without references resolves nothing, not even <see cref="object"/>, and the generator reports
    /// source that did not compile rather than the application it describes. Taking what this process already runs
    /// against is enough, and keeps the fixture from naming individual framework assemblies.
    /// </remarks>
    static IEnumerable<MetadataReference> References() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(assembly => MetadataReference.CreateFromFile(assembly));
}
