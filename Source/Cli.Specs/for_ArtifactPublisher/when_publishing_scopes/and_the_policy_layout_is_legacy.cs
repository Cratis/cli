// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublisher.when_publishing_scopes;

public class and_the_policy_layout_is_legacy : for_ScreenplayPlanning.when_planning_scopes.given.a_scoped_model
{
    ArtifactPublicationCheckResult _check = null!;
    Exception? _error;
    Dictionary<string, byte[]> _before = null!;

    async Task Establish()
    {
        await Publish(await Plan());
        await File.WriteAllTextAsync(Path.Combine(_destination, "GeneratedPolicies/Policies.cs"), "class StagePolicy_Legacy {}\n");
        _before = Directory.GetFiles(_destination, "*", SearchOption.AllDirectories).ToDictionary(path => path, File.ReadAllBytes, StringComparer.Ordinal);
    }

    async Task Because()
    {
        var plan = await Plan("Tasks");
        _check = await _publisher.Check(Request(plan), CancellationToken.None);
        _error = await Catch.Exception(() => Publish(plan));
    }

    [Fact] void should_refuse_the_render() => _error.ShouldBeOfExactType<UnsafeArtifactPublication>();
    [Fact] void should_report_the_policy_diagnostic() => _check.Receipt.Changes.Single().Code.ShouldEqual("CLI-RENDER-013");
    [Fact] void should_name_the_policy_path() => _check.Receipt.Changes.Single().Reason!.ShouldContain("GeneratedPolicies/Policies.cs");
    [Fact] void should_explain_the_migration() => _check.Receipt.Changes.Single().Reason!.ShouldContain("Render the whole application once");
    [Fact] void should_leave_every_byte_unchanged() => _before.All(pair => File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)).ShouldBeTrue();
    [Fact] void should_create_no_control_state() => Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
}
