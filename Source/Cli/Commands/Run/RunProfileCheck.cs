// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;
using Cratis.Screenplay.Files;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Checks the <c language="screenplay">ui profile</c> of a model against the packages a Stage can render before Docker is started.
/// </summary>
/// <remarks>
/// A live Stage renders the packages it ships and the ones the application approved with <c language="shell">--package-set</c>. A
/// profile that names anything else would start a container that shows a placeholder for the missing package, so the same
/// resolution <c language="shell">cratis render</c> applies refuses it here, with the same code (<c language="shell">CLI-RENDER-007</c>)
/// and without starting Docker. Only a package outside the catalog is refused: a live Stage shows an unresolved layout or theme
/// on the page, as it always has, and a model that does not compile is left to the Stage, which reports its own diagnostics.
/// </remarks>
internal static class RunProfileCheck
{
    /// <summary>
    /// Resolves every profile of the model against the shipped packages and the approved ones.
    /// </summary>
    /// <param name="input">The Screenplay file or folder to run.</param>
    /// <param name="approvedPackages">The approved package set, empty for none.</param>
    /// <returns>One error for each package a profile names that is neither shipped nor approved; empty when every package resolves.</returns>
    public static IReadOnlyList<ScreenplayDiagnostic> Check(FileSystemInfo input, ApprovedPackageSet approvedPackages)
    {
        try
        {
            var compiler = new PlayFileCompiler();
            var result = input is DirectoryInfo
                ? compiler.CompileFolder(input.FullName).Result
                : compiler.CompileFile(input.FullName).Result;
            if (!result.Success)
            {
                return [];
            }

            var scene = new ScreenplaySceneVisitor().Visit(result.Value!);
            return [.. SceneProfileResolution.Check(scene, approvedPackages).Where(_ => string.Equals(_.Code, SceneProfileResolution.UnknownPackageCode, StringComparison.Ordinal))];
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Whatever cannot be read here is reported by the Stage, which reads the same files.
            return [];
        }
    }
}
