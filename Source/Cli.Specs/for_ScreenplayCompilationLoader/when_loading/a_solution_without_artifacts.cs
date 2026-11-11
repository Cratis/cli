// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading;

[Collection(CliSpecsCollection.Name)]
public class a_solution_without_artifacts : given.a_multi_target_direct_workspace
{
    LoadedCompilation _result;

    void Establish()
    {
        TargetPath = Path.Combine(Root, "WithoutArtifacts.slnx");
        File.WriteAllText(TargetPath, "<Solution><Project Path=\"Dependencies/Dependency.csproj\" /><Project Path=\"Host/Application.csproj\" /></Solution>");
    }

    async Task Because() => _result = await ScreenplayCompilationLoader.Load(TargetPath, "net10.0", CancellationToken.None);

    [Fact] void should_filter_out_every_unrelated_compilation() => _result.Compilations.ShouldBeEmpty();
    [Fact] void should_not_return_metadata_for_filtered_projects() => _result.ProjectSources.ShouldBeEmpty();
    [Fact] void should_not_return_names_for_filtered_projects() => _result.ProjectNames.ShouldBeEmpty();
    [Fact] void should_report_no_artifacts() => _result.Diagnostics.Last().Code.ShouldEqual(ScreenplayDiagnosticCodes.NoArtifacts);
    [Fact] void should_preserve_the_failure_message() => _result.Diagnostics.Last().Message.ShouldEqual("No project in 'WithoutArtifacts.slnx' can declare a command or an event type, so there is nothing to generate a Screenplay from");
    [Fact] void should_not_disclose_the_physical_location() => _result.Diagnostics.Last().Location.ShouldEqual("WithoutArtifacts.slnx");
}
