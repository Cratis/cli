// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_a_model_root;

public class with_a_pending_operation : given.a_persisted_model_root
{
    void Establish() => File.WriteAllText(Path.Combine(_root, ".screenplay", "pending.json"), "pending");
    async Task Because() => _result = await _planning.Plan(new(_root, "Model", "cratis"), CancellationToken.None);

    [Fact] void should_refuse() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_typed_open_failure() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "CLI-RENDER-017" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error && diagnostic.Message.StartsWith("PendingOperation: ", StringComparison.Ordinal));
    [Fact] void should_not_plan_artifacts() => _result.Artifacts.ShouldBeNull();
    [Fact] void should_not_call_the_renderer() => _requests.ShouldBeEmpty();
    [Fact] void should_leave_the_recovery_journal_alone() => File.ReadAllText(Path.Combine(_root, ".screenplay", "pending.json")).ShouldEqual("pending");
}
