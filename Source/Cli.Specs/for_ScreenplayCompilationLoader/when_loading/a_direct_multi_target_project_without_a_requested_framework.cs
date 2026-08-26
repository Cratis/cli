// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading;

[Collection(CliSpecsCollection.Name)]
public class a_direct_multi_target_project_without_a_requested_framework : given.a_multi_target_direct_workspace
{
    LoadedCompilation _result;

    async Task Because() => _result = await ScreenplayCompilationLoader.Load(
        TargetPath,
        includeAllProjects: true,
        targetFramework: null,
        CancellationToken.None);

    [Fact] void should_fail_with_the_ambiguous_framework_code() => _result.Diagnostics.Single(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).Code.ShouldEqual(ScreenplayDiagnosticCodes.AmbiguousTargetFramework);
    [Fact] void should_name_only_the_targeted_root_project() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.AmbiguousTargetFramework).Message.ShouldContain("Project 'Application'");
    [Fact] void should_create_no_compilations() => _result.Compilations.ShouldBeEmpty();
    [Fact] void should_create_no_project_sources() => _result.ProjectSources.ShouldBeEmpty();
}
