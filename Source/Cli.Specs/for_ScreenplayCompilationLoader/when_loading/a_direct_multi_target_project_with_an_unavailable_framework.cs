// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading;

[Collection(CliSpecsCollection.Name)]
public class a_direct_multi_target_project_with_an_unavailable_framework : given.a_multi_target_direct_workspace
{
    LoadedCompilation _result;

    async Task Because() => _result = await ScreenplayCompilationLoader.Load(
        TargetPath,
        includeAllProjects: true,
        targetFramework: "net8.0",
        CancellationToken.None);

    [Fact] void should_fail_with_the_unavailable_framework_code() => _result.Diagnostics.Single(_ => _.Severity == ScreenplayDiagnosticSeverity.Error).Code.ShouldEqual(ScreenplayDiagnosticCodes.UnavailableTargetFramework);
    [Fact] void should_name_only_the_targeted_root_project() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnavailableTargetFramework).Message.ShouldContain("Project 'Application'");
    [Fact] void should_list_the_net9_root_variant() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnavailableTargetFramework).Message.ShouldContain("net9.0");
    [Fact] void should_list_the_net10_root_variant() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnavailableTargetFramework).Message.ShouldContain("net10.0");
    [Fact] void should_use_the_logical_target_identity() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnavailableTargetFramework).Location.ShouldEqual("Application.csproj");
    [Fact] void should_not_leak_the_physical_target_path() => _result.Diagnostics.Single(_ => _.Code == ScreenplayDiagnosticCodes.UnavailableTargetFramework).Location.ShouldNotContain(TargetPath);
    [Fact] void should_create_no_compilations() => _result.Compilations.ShouldBeEmpty();
}
