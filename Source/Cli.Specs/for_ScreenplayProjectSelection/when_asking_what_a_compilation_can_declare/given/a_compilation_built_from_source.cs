// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ScreenplayProjectSelection.when_asking_what_a_compilation_can_declare.given;

/// <summary>
/// Builds a compilation from source, so that what it can resolve is stated rather than restored.
/// </summary>
public class a_compilation_built_from_source : Specification
{
    /// <summary>
    /// Builds a compilation holding the given source.
    /// </summary>
    /// <param name="source">The source the compilation is built from.</param>
    /// <returns>The <see cref="Compilation"/>.</returns>
    protected static Compilation Holding(string source) =>
        CSharpCompilation.Create(
            "Project",
            [CSharpSyntaxTree.ParseText(source)],
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
