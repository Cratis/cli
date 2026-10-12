// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

public class and_the_only_slice_was_deleted : given.a_scoped_model
{
    ScreenplayRenderPlan _plan = null!;
    ArtifactPublicationResult _publication = null!;
    ArtifactPublicationCheckResult _check = null!;
    string[] _stalePaths = null!;
    string _otherPath = null!;
    byte[] _otherBytes = null!;

    async Task Establish()
    {
        await Publish(await Plan());
        _stalePaths = Directory.GetFiles(Path.Combine(_destination, "Tasks", "Backlog", "Intake"), "*", SearchOption.AllDirectories);
        _otherPath = Path.Combine(_destination, "Projects", "Registration", "RegisterProject", "RegisterProject.cs");
        _otherBytes = await File.ReadAllBytesAsync(_otherPath);
        await File.WriteAllTextAsync(_taskFile, """
            policy Staff
              require role "Staff"
            module Tasks
              feature Backlog
                feature Intake
                  description "Tasks have been removed"
            """);
        await File.WriteAllTextAsync(_lookupFile, """
            module Projects
              feature Registration
                slice StateView ProjectLookup
                  readmodel ProjectSummary
                    projectId ProjectId
                    name String
                    lastSeen DateTime?
                  query ProjectById => ProjectSummary optional
                    by projectId ProjectId
                  projection ProjectSummaryProjection => ProjectSummary
                    from ProjectRegistered key projectId
                      name = name
                      lastSeen = $eventContext.occurred
            """);
    }

    async Task Because()
    {
        _plan = await Plan("Tasks.Backlog.Intake");
        _check = await _publisher.Check(Request(_plan), CancellationToken.None);
        _publication = await Publish(_plan);
    }

    [Fact] void should_admit_the_empty_feature() => _plan.Success.ShouldBeTrue();
    [Fact] void should_report_the_unrelated_issue_as_a_warning() => _plan.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAN-UNSUPPORTEDEVENTCONTEXT" && diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_have_no_slice_artifacts() => _plan.Artifacts!.Artifacts.ShouldBeEmpty();
    [Fact] void should_not_report_an_invalid_slice_selection() => _plan.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAN-INVALIDSLICESELECTION").ShouldBeEmpty();
    [Fact] void should_have_previously_owned_files() => _stalePaths.ShouldNotBeEmpty();
    [Fact] void should_remove_the_previously_owned_files() => _stalePaths.All(path => !File.Exists(path)).ShouldBeTrue();
    [Fact] void should_record_removed_owned_files() => _publication.Removed.ShouldBeGreaterThan(0);
    [Fact] void should_agree_with_the_check() => _check.Removed.ShouldEqual(_publication.Removed);
    [Fact] void should_preserve_other_scopes() => File.ReadAllBytes(_otherPath).SequenceEqual(_otherBytes).ShouldBeTrue();
}
