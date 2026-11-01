// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_changed_ui_profile;

public class with_an_approved_custom_package : given.an_approved_package
{
    Task Because() => Plan(_approved);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_no_unknown_package() => ErrorsWith(SceneProfileResolution.UnknownPackageCode).ShouldBeEmpty();
    [Fact] void should_plan_the_approved_package_set_document() => _result.Artifacts!.Artifacts.Select(_ => _.RelativePath).ShouldContain("approved-packages.json");
    [Fact] void should_plan_the_module_that_loads_it() => _result.Artifacts!.Artifacts.Select(_ => _.RelativePath).ShouldContain(".frontend/approvedPackages.ts");
}
