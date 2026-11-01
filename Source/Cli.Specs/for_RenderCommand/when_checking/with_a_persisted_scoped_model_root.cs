// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Mcp;

namespace Cratis.Cli.for_RenderCommand.when_checking;

public class with_a_persisted_scoped_model_root : for_ScreenplayPlanning.when_planning_a_model_root.given.a_persisted_model_root
{
    string _destination = null!;
    int _pending;
    int _published;
    string _publishedError = null!;

    void Establish()
    {
        _destination = $"{_root}-out";
        ReleaseBaselineCommands.Admit(_root, "v1.0").ExitCode.ShouldEqual(0);
        File.WriteAllText(_document, ModelSource.Replace("value String", "value Int", StringComparison.Ordinal));
    }

    async Task Because()
    {
        await Run("render", _root, "--name", "Model", "--destination", _destination, "--scope", "M.F.Observe", "--check");
        _pending = _exitCode;
        _error.ShouldContain("STAGE-BASELINE-001");
        Directory.Exists(_destination).ShouldBeFalse();
        await Run("render", _root, "--name", "Model", "--destination", _destination, "--scope", "M.F.Observe");
        _published = _exitCode;
        _publishedError = _error;
        await Run("render", _root, "--name", "Model", "--destination", _destination, "--scope", "M.F.Observe", "--check");
    }

    void Destroy()
    {
        if (Directory.Exists(_destination)) Directory.Delete(_destination, true);
    }

    [Fact] void should_report_pending_scoped_changes() => _pending.ShouldEqual(ExitCodes.ChangesPending);
    [Fact] void should_publish_the_scoped_plan() => _published.ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_the_scope_current_after_publication() => _exitCode.ShouldEqual(ExitCodes.Success);
    [Fact] void should_warn_about_the_unselected_revision_during_publication() => _publishedError.ShouldContain("STAGE-BASELINE-001");
    [Fact] void should_warn_about_the_unselected_revision_during_check() => _error.ShouldContain("STAGE-BASELINE-001");
    [Fact] void should_not_publish_the_unselected_event() => Directory.EnumerateFiles(_destination, "Record.cs", SearchOption.AllDirectories).ShouldBeEmpty();
    [Fact] void should_publish_the_selected_event() => Directory.EnumerateFiles(_destination, "Observe.cs", SearchOption.AllDirectories).ShouldContainSingleItem();
}
