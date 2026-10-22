// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_the_component_library;

/// <summary>
/// The unchanged corpus lists no PrimeReact, so the plan is the React Aria scaffold and is admitted.
/// </summary>
public class with_no_library_listed : when_planning_a_changed_ui_profile.given.the_screen_composition_corpus
{
    Task Because() => Plan();

    [Fact] void should_be_admitted() => _result.Success.ShouldBeTrue();
    [Fact] void should_plan_a_package_json_without_primereact() => TextOf("package.json").ShouldNotContain("primereact");
    [Fact] void should_plan_a_main_without_the_primereact_provider() => TextOf("main.tsx").ShouldNotContain("PrimeReact");

    string TextOf(string fileName) => Encoding.UTF8.GetString(
        _result.Artifacts!.Artifacts.Single(_ => _.RelativePath.EndsWith(fileName, StringComparison.Ordinal) && !_.RelativePath.Contains("node_modules")).Bytes.AsSpan());
}
