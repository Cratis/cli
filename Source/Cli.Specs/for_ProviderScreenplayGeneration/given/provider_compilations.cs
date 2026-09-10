// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Cli.for_ProviderScreenplayGeneration.given;

public class provider_compilations : Specification
{
    protected static LoadedCompilation LoadedFrom(string source) =>
        LoadedFromProjects(("Application", source));

    protected static LoadedCompilation LoadedFromProjects(params (string Name, string Source)[] projects) => new(
        [.. projects.Select(project => CSharpCompilation.Create(
            project.Name,
            [CSharpSyntaxTree.ParseText(project.Source)],
            References(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))],
        [.. projects.Select(project => project.Name)],
        []);

    protected static IEnumerable<MetadataReference> References() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(_ => MetadataReference.CreateFromFile(_));
}
