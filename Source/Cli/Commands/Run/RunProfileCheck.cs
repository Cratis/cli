// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;
using Cratis.Cli.Commands.Screenplay;
using Cratis.Scene.Engine.Packages;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Cli.Commands.Run;

/// <summary>
/// Checks the <c language="screenplay">ui profile</c> of a model against the packages a Stage can render, and the application shell it authors,
/// before Docker is started.
/// </summary>
/// <remarks>
/// A live Stage renders the packages it ships and the ones the application approved with <c language="shell">--package-set</c>. A
/// profile that names anything else would start a container that shows a placeholder for the missing package, so the same
/// resolution <c language="shell">cratis render</c> applies refuses it here, with the same code (<c language="shell">CLI-RENDER-007</c>)
/// and without starting Docker. Only a package outside the catalog is refused: a live Stage shows an unresolved layout or theme
/// on the page, as it always has, and a model that does not compile is left to the Stage, which reports its own diagnostics. The one
/// exception is the application shell: a navigation region, a toolbar button or a sample data selection the compiler refuses makes the Stage
/// abort with a stack trace that lists the messages without their codes, so those errors (<c language="shell">PLAY0709</c>-<c language="shell">PLAY0715</c>, <c language="shell">PLAY0740</c>-<c language="shell">PLAY0752</c>,
/// <c language="shell">PLAY0760</c>-<c language="shell">PLAY0765</c>) and the root template's (<c language="shell">PLAY0790</c>-<c language="shell">PLAY0799</c>: an unknown root template or
/// blueprint, a version pin no installed blueprint satisfies, a template that cannot be a shell, an unknown shell option or appearance value) are reported here with their code and <c language="shell">file(line,column)</c> location, exactly as
/// <c language="shell">cratis render</c> reports them, and nothing is started.
/// </remarks>
internal static class RunProfileCheck
{
    /// <summary>
    /// Resolves every profile of the model against the shipped packages and the approved ones, and reports the compiler's errors about the
    /// application shell it authors.
    /// </summary>
    /// <param name="input">The Screenplay file or folder to run.</param>
    /// <param name="approvedPackages">The approved package set, empty for none.</param>
    /// <returns>The profile and shell errors that refuse the run before Docker starts.</returns>
    public static IReadOnlyList<ScreenplayDiagnostic> Check(FileSystemInfo input, ApprovedPackageSet approvedPackages) =>
        CompileAndCheck(input, approvedPackages).Findings;

    internal static (IReadOnlyList<ScreenplayDiagnostic> Findings, ApplicationSyntax? Application, SceneApplication? Scene) CompileAndCheck(FileSystemInfo input, ApprovedPackageSet approvedPackages, bool sampleData = false)
    {
        try
        {
            var compiler = new PlayFileCompiler();
            var result = input is DirectoryInfo
                ? compiler.CompileFolder(input.FullName).Result
                : compiler.CompileFile(input.FullName).Result;
            if (!result.Success)
            {
                return ([.. result.Diagnostics.Where(diagnostic => IsShellError(diagnostic) || sampleData && diagnostic.Severity == DiagnosticSeverity.Error).Select(Map)], null, null);
            }

            var scene = new ScreenplaySceneVisitor().Visit(result.Value!);
            return ([.. SceneProfileResolution.Check(scene, approvedPackages).Where(_ => IsRefusedBeforeStart(_.Code))], result.Value, scene);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Whatever cannot be read here is reported by the Stage, which reads the same files.
            return (sampleData ? [new(ScreenplayDiagnosticSeverity.Error, "CLI-RUN-SAMPLE-DATA", exception.Message, input.FullName)] : [], null, null);
        }
    }

    /// <summary>
    /// Whether a profile resolution finding stops the run: a package outside the catalog or a root template that cannot be the shell. A live Stage shows an unresolved layout or theme on the page.
    /// </summary>
    /// <param name="code">The finding's diagnostic code.</param>
    /// <returns>True when the run is refused.</returns>
    static bool IsRefusedBeforeStart(string code) =>
        string.Equals(code, SceneProfileResolution.UnknownPackageCode, StringComparison.Ordinal) ||
        string.Equals(code, SceneProfileResolution.RootTemplateCode, StringComparison.Ordinal) ||
        string.Equals(code, SceneProfileResolution.UnresolvedTemplateCode, StringComparison.Ordinal);

    static bool IsShellError(Diagnostic diagnostic) =>
        diagnostic.Severity == DiagnosticSeverity.Error &&
        diagnostic.Code.StartsWith("PLAY", StringComparison.Ordinal) &&
        int.TryParse(diagnostic.Code.AsSpan(4), out var number) &&
        number is (>= 709 and <= 715) or (>= 740 and <= 752) or (>= 760 and <= 765) or (>= 790 and <= 799);

    static ScreenplayDiagnostic Map(Diagnostic diagnostic) =>
        new(
            ScreenplayDiagnosticSeverity.Error,
            diagnostic.Code,
            diagnostic.Message,
            $"{diagnostic.Location.Path ?? "Screenplay"}({diagnostic.Location.Line},{diagnostic.Location.Column})");
}
