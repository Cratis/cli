// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Stage.Rendering.Cratis;

namespace Cratis.Cli.for_ScreenplayPlanning;

/// <summary>
/// Plans the shell fixture the integration scripts render and run: a layout with topbar and sidebar regions, three modules that
/// inherit, override and extend the sidebar, a toolbar button that names a State Change slice, and a sample data pack the profile selects.
/// </summary>
public class when_planning_the_shell_application : Specification
{
    ScreenplayRenderPlan _result = null!;
    JsonDocument _scene = null!;

    async Task Because()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "Shell", "model");
        _result = await new ScreenplayPlanning().Plan(new(folder, "Shell", CratisRendering.TargetId, null, null), CancellationToken.None);
        _scene = JsonDocument.Parse(Text("scene.json"));
    }

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_errors() => _result.Diagnostics.Where(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_emit_the_topbar_items_of_the_application() => Items(Regions("application", "topbar")).ShouldEqual(["Home", "Sales", "Support"]);
    [Fact] void should_emit_the_sidebar_items_of_the_application() => Items(Regions("application", "sidebar")).ShouldEqual(["Overview"]);
    [Fact] void should_emit_the_module_that_overrides_the_sidebar() => Mode("Sales", "sidebar").ShouldEqual("override");
    [Fact] void should_emit_the_module_that_extends_the_sidebar() => Mode("Support", "sidebar").ShouldEqual("extend");
    [Fact] void should_emit_the_slot_an_item_renders_into() => Text("scene.json").ShouldContain("\"slot\":\"side\"");
    [Fact] void should_emit_the_toolbar_button_that_names_the_slice() => Text("scene.json").ShouldContain("Sales.Orders.PlaceOrder");
    [Fact] void should_not_emit_a_sample_data_pack() => _scene.RootElement.TryGetProperty("sampleDataPacks", out _).ShouldBeFalse();
    [Fact] void should_not_emit_a_sample_data_selection_in_a_profile() => _scene.RootElement.GetProperty("uiProfiles").EnumerateArray().Any(_ => _.TryGetProperty("sampleData", out _)).ShouldBeFalse();
    [Fact] void should_not_carry_a_sample_row_in_any_artifact() => Encoding.UTF8.GetString([.. _result.Artifacts!.Artifacts.SelectMany(_ => _.Bytes)]).ShouldNotContain("must never ship");

    string Text(string path) => Encoding.UTF8.GetString(_result.Artifacts!.Artifacts.Single(_ => _.RelativePath == path).Bytes.AsSpan());

    IEnumerable<JsonElement> Regions(string scope, string region)
    {
        var navigation = _scene.RootElement.GetProperty("navigation");
        return navigation.GetProperty(scope).GetProperty("regions").EnumerateArray().Where(_ => _.GetProperty("region").GetString() == region);
    }

    string Mode(string module, string region) => _scene.RootElement.GetProperty("navigation").GetProperty("modules").EnumerateArray()
        .Single(_ => _.GetProperty("module").GetString() == module).GetProperty("regions").EnumerateArray()
        .Single(_ => _.GetProperty("region").GetString() == region).GetProperty("mode").GetString()!;

    static string[] Items(IEnumerable<JsonElement> regions) => [.. regions.SelectMany(_ => _.GetProperty("items").EnumerateArray()).Select(_ => _.GetProperty("title").GetString()!)];
}
