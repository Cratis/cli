// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_refusing_unsafe_degraded_artifacts : given.a_degraded_publication
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task should_refuse_original_errors_even_with_the_degradation_code(bool strict, bool existingDestination)
    {
        if (existingDestination) await Publish(cancellationToken: TestContext.Current.CancellationToken);
        await PlanDegraded(strict, unsafeGeneration: true);
        _plan.CanPublishArtifacts.ShouldBeFalse();
        var before = Snapshot(_destination);

        var result = await Check();
        result.Refused.ShouldBeGreaterThan(0);
        Snapshot(_destination).ShouldEqual(before);
        var error = await Catch.Exception(() => Publish());

        error.ShouldBeOfExactType<UnsafeArtifactPublication>();
        Snapshot(_destination).ShouldEqual(before);
        Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_not_bypass_destination_conflicts_for_safe_plans(bool strict)
    {
        await PlanDegraded(strict);
        _plan.CanPublishArtifacts.ShouldBeTrue();
        Directory.CreateDirectory(Path.GetDirectoryName(FirstSourcePath())!);
        await File.WriteAllTextAsync(FirstSourcePath(), "user owned bytes", TestContext.Current.CancellationToken);
        var before = Snapshot(_destination);

        var result = await Check(force: true);
        result.Refused.ShouldBeGreaterThan(0);
        Snapshot(_destination).ShouldEqual(before);
        var error = await Catch.Exception(() => Publish(force: true));

        error.ShouldBeOfExactType<UnsafeArtifactPublication>();
        Snapshot(_destination).ShouldEqual(before);
    }
}
