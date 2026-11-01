// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Files;
using Cratis.Stage.Contracts.Scene;

namespace Cratis.Cli.for_UiProfileRootVector;

/// <summary>
/// The corpus' <c language="csharp">ui-profile-root-positive</c> document through the CLI's profile resolution - the one <c language="shell">cratis render</c> and the
/// <c language="shell">cratis run</c> pre-check share. The same document is held to the same shell resolution by Stage's .NET and TypeScript conformance specs
/// (<c language="csharp">uiprofile-root.shells.json</c>), so the four toolchains - Screenplay, Stage, the TypeScript engine and the CLI - agree: an own layout root and an
/// own application-scope screen template root start, and the blueprint root whose <c language="screenplay">version "^4.5"</c> pin the installed default blueprint does not satisfy is
/// <c language="shell">CLI-RENDER-019</c>.
/// </summary>
public class when_resolving_the_uiprofile_root_vector : Specification
{
    string _folder;
    IReadOnlyList<Cratis.Cli.Commands.Screenplay.ScreenplayDiagnostic> _diagnostics;

    void Establish()
    {
        var text = ScreenCompositionCorpus.V1.TypedSourceCases.Single(_ => _.Name == "ui-profile-root-positive").Document.Text
            .Replace("ui profile Docked\n", "ui profile Docked\n  target platform web\n", StringComparison.Ordinal)
            .Replace("ui profile FromBlueprint\n", "ui profile FromBlueprint\n  target platform web\n", StringComparison.Ordinal);
        _folder = Directory.CreateTempSubdirectory("cratis-cli-uiprofile-root-").FullName;
        File.WriteAllText(Path.Combine(_folder, "application.play"), text);
    }

    void Because()
    {
        var compilation = new PlayFileCompiler().CompileFolder(_folder).Result;
        compilation.Success.ShouldBeTrue();
        _diagnostics = SceneProfileResolution.Check(new ScreenplaySceneVisitor().Visit(compilation.Value!));
    }

    void Destroy() => Directory.Delete(_folder, true);

    [Fact] void should_refuse_only_the_unsatisfied_blueprint_pin() => _diagnostics.Select(_ => _.Code).ShouldContainOnly([SceneProfileResolution.RootTemplateCode]);
    [Fact] void should_name_the_profile_the_pin_belongs_to() => _diagnostics.Single().Message.ShouldContain("FromBlueprint (web)");
    [Fact] void should_name_the_blueprint_template() => _diagnostics.Single().Message.ShouldContain("Cratis.Blueprint.Default");
}
