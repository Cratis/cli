// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;

namespace Cratis.Cli.for_ArtifactPublicationCheck;

public class when_checking_strict_degraded_artifacts : given.a_degraded_publication
{
    async Task Establish() => await PlanDegraded(strict: true);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task should_check_without_writing_and_preserve_acceptance(bool published)
    {
        _plan.Success.ShouldBeFalse();
        _plan.CanPublishArtifacts.ShouldBeTrue();
        if (published) await Publish(cancellationToken: TestContext.Current.CancellationToken);
        var before = Snapshot(_destination);

        var result = await Check();

        result.Refused.ShouldEqual(0);
        result.Written.ShouldEqual(published ? 0 : _plan.Artifacts.Length);
        Snapshot(_destination).ShouldEqual(before);
        Directory.Exists(ArtifactPublicationStorage.ControlPath(_destination)).ShouldBeFalse();
        Directory.Exists(_destination).ShouldEqual(published);
    }
}
