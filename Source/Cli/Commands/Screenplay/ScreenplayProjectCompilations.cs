// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Generation.DotNet;
using Microsoft.CodeAnalysis;

namespace Cratis.Cli.Commands.Screenplay;

/// <summary>
/// Maps CLI-loaded compilations to the shared project-aware .NET source-adapter contract.
/// </summary>
static class ScreenplayProjectCompilations
{
    /// <summary>
    /// Maps aligned loaded projects while preserving the context-free legacy fallback.
    /// </summary>
    /// <param name="loaded">The loaded compilations and optional workspace source metadata.</param>
    /// <param name="targetPath">The originally targeted solution or project path used by legacy inputs.</param>
    /// <returns>The complete project compilation contracts.</returns>
    internal static IReadOnlyList<DotNetProjectCompilation> From(LoadedCompilation loaded, string targetPath)
    {
        var legacySourceRoot = Path.GetDirectoryName(targetPath);
        var projectSources = loaded.ProjectSources.Count == 0 ? null : loaded.ProjectSources;
        return
        [
            .. loaded.Compilations.Select((compilation, index) =>
            {
                var source = projectSources?[index];
                return new DotNetProjectCompilation
                {
                    Name = loaded.ProjectNames[index],
                    Role = source?.Role ?? DotNetProjectRole.Application,
                    ProjectPath = source?.ProjectPath ?? targetPath,
                    SourceRoot = source?.SourceRoot ?? legacySourceRoot,
                    SourceContext = source?.SourceContext,
                    Compilation = compilation,
                    AuthoredSyntaxTrees = loaded.AuthoredSyntaxTrees.Count > index
                        ? loaded.AuthoredSyntaxTrees[index]
                        : new HashSet<SyntaxTree>()
                };
            })
        ];
    }
}
