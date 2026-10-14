// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.for_RenderCommand.when_checking;

public class with_a_persisted_model_root : for_ScreenplayPlanning.when_planning_a_model_root.given.a_persisted_model_root
{
    string _destination = null!;
    int _pending;
    int _published;
    string _pendingError = null!;
    string _publishedError = null!;
    string[] _before = [];
    string[] _after = [];

    void Establish()
    {
        _destination = $"{_root}-out";
        ReleaseBaselineCommands.Admit(_root, "v1.0").ExitCode.ShouldEqual(0);
        File.WriteAllText(_document, ModelSource.Replace("value String", "value Int", StringComparison.Ordinal));
    }

    async Task Because()
    {
        await Run("render", _root, "--name", "Model", "--destination", _destination, "--check");
        _pending = _exitCode;
        _pendingError = _error;
        Directory.Exists(_destination).ShouldBeFalse();
        await Run("render", _root, "--name", "Model", "--destination", _destination);
        _published = _exitCode;
        _publishedError = _error;
        _before = Snapshot();
        await Run("render", _root, "--name", "Model", "--destination", _destination, "--check");
        _after = Snapshot();
    }

    string[] Snapshot() => [.. Directory.EnumerateFiles(_destination, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal).Select(path => $"{Path.GetRelativePath(_destination, path)}:{Convert.ToHexString(File.ReadAllBytes(path))}")];
    void Destroy()
    {
        if (Directory.Exists(_destination)) Directory.Delete(_destination, true);
    }

    [Fact] void should_report_changes_pending_before_publication() => _pending.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_publish_successfully() => _published.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_current_after_publication() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_the_warning_in_the_pending_check() => _pendingError.ShouldContain("STAGE-BASELINE-001");
    [Fact] void should_report_the_same_warning_in_publication() => _publishedError.ShouldContain("STAGE-BASELINE-001");
    [Fact] void should_report_the_warning_in_the_current_check() => _error.ShouldContain("STAGE-BASELINE-001");
    [Fact] void should_not_write_in_check_mode() => _after.ShouldEqual(_before);
}
