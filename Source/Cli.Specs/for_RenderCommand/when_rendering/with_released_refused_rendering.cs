// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_released_refused_rendering : given.a_check_command
{
    void Establish()
    {
        File.WriteAllText(_document, string.Join('\n',
            "module Tasks",
            "  feature Unsupported",
            "    slice Automation Unsupported",
            "      reaction ReminderScheduler",
            "        every 1 minutes",
            "          produces ReminderScheduled",
            "            for \"reminders\"",
            "            scheduledAt = $context.occurred",
            "      event ReminderScheduled",
            "        scheduledAt DateTime"));
        _planning = new ScreenplayPlanning();
        _command = new RenderCommand(_planning, new ArtifactPublisher());
        _settings.Check = false;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_refuse_unsafe_generation_with_or_without_strict_acceptance(bool strict)
    {
        _settings.Strict = strict;

        var output = await Capture();

        output.ExitCode.ShouldEqual(ExitCodes.ValidationError);
        var plan = await _planning.Plan(new(_document, "MyApp", "cratis") { Strict = strict }, CancellationToken.None);
        plan.Success.ShouldBeFalse();
        plan.Artifacts!.Outcome.ShouldEqual(ArtifactRenderOutcome.Refused);
        plan.Artifacts.CanPublishArtifacts.ShouldBeFalse();
        plan.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == "STAGE-ESM-024" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Error);
        Directory.Exists(_destination).ShouldBeFalse();
    }
}
