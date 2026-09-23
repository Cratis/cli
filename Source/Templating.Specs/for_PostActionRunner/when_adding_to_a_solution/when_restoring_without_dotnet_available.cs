// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;
using Cratis.Templating.PostActions;

namespace Cratis.Templating.Specs.for_PostActionRunner.when_adding_to_a_solution;

public class when_restoring_without_dotnet_available : Specification
{
    PostActionResult? _result;
    PostActionConfig _action = new()
    {
        ActionId = "210D431B-A78B-4D2F-B762-4ED3E3EA9025",
        ManualInstructions = [new ManualInstructionConfig { Text = "Run 'dotnet restore'." }]
    };

    async Task Because() => _result = await Restore.RunForSpecs(_action, new InstantiationResult("App", Path.GetTempPath(), [], [], new Dictionary<string, string>(), []), isDotnetAvailable: () => false);

    [Fact] void should_report_not_performed() => _result!.Outcome.ShouldEqual(PostActionOutcome.NotPerformed);

    [Fact] void should_not_count_as_a_run_failure() => _result!.IsFailure.ShouldBeFalse();

    [Fact] void should_carry_the_manual_instructions() => _result!.Instructions.ShouldContain("dotnet restore");
}
