// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayPlanning.when_planning_scopes;

#pragma warning disable MA0136 // Fixture line endings do not affect normalized rendered artifacts.

public class and_an_empty_scope_has_an_invalid_application_concept : given.a_scoped_model
{
    ScreenplayRenderPlan _plan = null!;
    string _ownedPath = null!;
    byte[] _ownedBytes = null!;
    byte[] _manifestBytes = null!;

    async Task Establish()
    {
        await Publish(await Plan());
        _ownedPath = Directory.GetFiles(Path.Combine(_destination, "Tasks", "Backlog", "Intake"), "*.cs", SearchOption.AllDirectories)[0];
        _ownedBytes = await File.ReadAllBytesAsync(_ownedPath);
        _manifestBytes = await File.ReadAllBytesAsync(Path.Combine(_destination, ".cratis-render.json"));
        await File.AppendAllTextAsync(_projectFile, "\nconcept Broken : MissingType\n");
        await File.WriteAllTextAsync(_taskFile, """
            module Tasks
              feature Backlog
                feature Intake
                  description "Tasks have been removed"
            """);
    }

    async Task Because() => _plan = await Plan("Tasks.Backlog.Intake");

    [Fact] void should_refuse_the_empty_scope() => _plan.Success.ShouldBeFalse();
    [Fact] void should_report_the_application_error() => _plan.Diagnostics.Any(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_have_no_publishable_plan() => _plan.Artifacts.ShouldBeNull();
    [Fact] void should_preserve_previously_owned_artifacts() => File.ReadAllBytes(_ownedPath).SequenceEqual(_ownedBytes).ShouldBeTrue();
    [Fact] void should_preserve_the_manifest() => File.ReadAllBytes(Path.Combine(_destination, ".cratis-render.json")).SequenceEqual(_manifestBytes).ShouldBeTrue();
}
