// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;

namespace Cratis.Cli.for_CreatedProjectAiUpdate.when_running;

public class and_the_project_has_no_ai_configuration : Specification
{
    string _outputRoot = null!;
    string _resolvedOutputRoot = null!;
    CreatedProjectAiUpdateResult? _result;
    string? _previousCurrentDirectory;

    void Establish()
    {
        _previousCurrentDirectory = Environment.CurrentDirectory;
        _outputRoot = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-aiupdate", Guid.NewGuid().ToString("N"))).FullName;

        // Probe the directory's resolved form once — on macOS the temp folder is a symlink and
        // the working directory reports the resolved path.
        Environment.CurrentDirectory = _outputRoot;
        _resolvedOutputRoot = Environment.CurrentDirectory;
        Environment.CurrentDirectory = _previousCurrentDirectory;
    }

    void Because() => _result = CreatedProjectAiUpdate.Run(_outputRoot);

    [Fact] void should_skip_gracefully() => _result!.Status.ShouldEqual("skipped");

    [Fact] void should_name_the_missing_configuration() => _result!.Detail.ShouldContain(".cratis/ai.json");

    [Fact] void should_change_into_the_created_folder() =>
        Environment.CurrentDirectory.ShouldEqual(_resolvedOutputRoot);

    void Destroy()
    {
        if (_previousCurrentDirectory is not null)
        {
            Environment.CurrentDirectory = _previousCurrentDirectory;
        }
    }
}
