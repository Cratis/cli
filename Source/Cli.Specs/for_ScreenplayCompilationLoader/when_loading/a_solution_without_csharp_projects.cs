// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayCompilationLoader.when_loading;

[Collection(CliSpecsCollection.Name)]
public class a_solution_without_csharp_projects : Specification
{
    string _root;
    string _targetPath;
    LoadedCompilation _result;

    void Establish()
    {
        _root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"screenplay-empty-solution-{Guid.NewGuid():N}")).FullName;
        _targetPath = Path.Combine(_root, "Empty.slnx");
        File.WriteAllText(_targetPath, "<Solution />");
    }

    async Task Because() => _result = await ScreenplayCompilationLoader.Load(_targetPath, CancellationToken.None);

    [Fact] void should_not_return_compilations() => _result.Compilations.ShouldBeEmpty();
    [Fact] void should_not_return_project_names() => _result.ProjectNames.ShouldBeEmpty();
    [Fact] void should_report_no_project() => _result.Diagnostics[^1].Code.ShouldEqual(ScreenplayDiagnosticCodes.NoProject);
    [Fact] void should_preserve_the_failure_message() => _result.Diagnostics[^1].Message.ShouldEqual("No C# project to generate from was found in 'Empty.slnx'");
    [Fact] void should_not_disclose_the_physical_location() => _result.Diagnostics[^1].Location.ShouldEqual("Empty.slnx");

    void Destroy() => Directory.Delete(_root, true);
}
