// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading;

[Collection(CliSpecsCollection.Name)]
public class a_direct_multi_target_project_with_an_exact_requested_framework : given.a_multi_target_direct_workspace
{
    LoadedCompilation _result;

    async Task Because() => _result = await ScreenplayCompilationLoader.Load(
        TargetPath,
        includeAllProjects: true,
        targetFramework: "net9.0",
        CancellationToken.None);

    [Fact] void should_select_the_exact_root_variant() => _result.ProjectProvenance.Single(_ => _.Project == "Application").TargetFramework.ShouldEqual("net9.0");
    [Fact] void should_follow_the_matching_dependency_variant() => _result.ProjectProvenance.Single(_ => _.Project == "Dependency").TargetFramework.ShouldEqual("net9.0");
    [Fact] void should_retain_only_one_variant_of_each_project() => _result.ProjectNames.ShouldContainOnly(["Application", "Dependency"]);
    [Fact] void should_not_report_target_framework_diagnostics() => _result.Diagnostics.All(_ => _.Code is not ScreenplayDiagnosticCodes.AmbiguousTargetFramework and not ScreenplayDiagnosticCodes.UnavailableTargetFramework).ShouldBeTrue();
}
