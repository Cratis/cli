// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Engine.Packages;
using Cratis.Scene.Screenplay.Forward;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_RunProfileCheck;

public class when_checking_a_profile_with_a_design_system : Specification
{
    string _folder = null!;
    IReadOnlyList<ScreenplayDiagnostic> _findings = null!;
    SceneApplication? _scene;

    void Establish()
    {
        _folder = Directory.CreateTempSubdirectory("cratis-run-design-system-").FullName;
        var source = ScreenCompositionCorpus.V1.TypedSourceCases.Single(_ => _.Name == "design-system-positive").Document.Text;
        File.WriteAllText(Path.Combine(_folder, "application.play"), source);
    }

    void Because() => (_findings, _, _scene) = RunProfileCheck.CompileAndCheck(new DirectoryInfo(_folder), ApprovedPackageSet.Empty);

    [Fact] void should_accept_the_profiles() => _findings.ShouldBeEmpty();
    [Fact] void should_carry_the_design_system_on_the_default_theme_profile() => _scene!.UiProfiles.Single(_ => _.Name == "Office").DesignSystem!.Name.ShouldEqual("Contoso");
    [Fact] void should_carry_the_design_system_on_the_selected_theme_profile() => _scene!.UiProfiles.Single(_ => _.Name == "Floor").DesignSystem!.Name.ShouldEqual("Contoso");
    [Fact] void should_carry_the_resolved_default_theme() => _scene!.UiProfiles.Single(_ => _.Name == "Office").DesignSystem!.Themes.Single(_ => _.IsDefault).Name.ShouldEqual("Aurora");

    void Destroy() => Directory.Delete(_folder, true);
}
