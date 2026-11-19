// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayPlanning;

public class when_planning_a_profile_with_a_design_system : given.a_screenplay_planning
{
    ScreenplayRenderPlan _result = null!;
    JsonDocument _scene = null!;

    void Establish()
    {
        var designSystem = ScreenCompositionCorpus.V1.TypedSourceCases.Single(_ => _.Name == "design-system-positive").Document.Text;
        File.WriteAllText(_file, $"{Source}\n      screen ProjectLookupScreen\n        title \"Projects\"\n\n{designSystem}");
    }

    async Task Because()
    {
        _result = await Plan();
        _scene = JsonDocument.Parse(Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(_ => _.RelativePath == "scene.json").Bytes.AsSpan()));
    }

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_carry_the_design_system_on_the_default_theme_profile() => DesignSystem("Office").GetProperty("name").GetString().ShouldEqual("Contoso");
    [Fact] void should_carry_the_design_system_on_the_selected_theme_profile() => DesignSystem("Floor").GetProperty("name").GetString().ShouldEqual("Contoso");
    [Fact] void should_carry_the_resolved_default_theme() => DesignSystem("Office").GetProperty("themes").EnumerateArray().Single(_ => _.GetProperty("isDefault").GetBoolean()).GetProperty("name").GetString().ShouldEqual("Aurora");

    JsonElement DesignSystem(string profile) => _scene.RootElement.GetProperty("uiProfiles").EnumerateArray()
        .Single(_ => _.GetProperty("name").GetString() == profile).GetProperty("designSystem");

    void Destroy() => _scene?.Dispose();
}
