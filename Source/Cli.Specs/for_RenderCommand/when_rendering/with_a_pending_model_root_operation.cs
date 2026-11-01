// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RenderCommand.when_rendering;

public class with_a_pending_model_root_operation : for_ScreenplayPlanning.when_planning_a_model_root.given.a_persisted_model_root
{
    string _destination = null!;

    void Establish()
    {
        _destination = Path.Combine(_root, "out");
        File.WriteAllText(Path.Combine(_root, ".screenplay", "pending.json"), "pending");
    }

    async Task Because() => await Run("render", _root, "--name", "Model", "--destination", _destination);

    [Fact] void should_return_a_validation_error() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_the_new_render_error() => _error.ShouldContain("CLI-RENDER-017");
    [Fact] void should_name_the_open_failure_kind() => _error.ShouldContain("PendingOperation");
    [Fact] void should_not_create_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
    [Fact] void should_leave_recovery_to_the_model_root_owner() => File.ReadAllText(Path.Combine(_root, ".screenplay", "pending.json")).ShouldEqual("pending");
}
