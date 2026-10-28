// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;

namespace Cratis.Cli.for_RenderCommand.when_rendering;

[Collection(CliSpecsCollection.Name)]
public class with_unexpected_strict_failure : given.a_render_acceptance_command
{
    [Theory]
    [InlineData("planning")]
    [InlineData("publication")]
    [InlineData("acceptance")]
    public async Task should_escape_unchanged_and_stop_later_effects(string failingSeam)
    {
        _settings.Strict = true;
        var failure = new InvalidOperationException($"Unexpected {failingSeam} failure");
        switch (failingSeam)
        {
            case "planning":
                _planning.Configure().Plan(Arg.Any<ScreenplayRenderRequest>(), Arg.Any<CancellationToken>())
                    .Throws(failure);
                break;
            case "publication":
                _publication.Configure().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>())
                    .Throws(failure);
                break;
            case "acceptance":
                _acceptance.Configure()(Arg.Any<ScreenplayRenderPlan>(), true).Throws(failure);
                break;
        }

        var error = await Catch.Exception(Capture);

        ReferenceEquals(error, failure).ShouldBeTrue();
        if (failingSeam == "planning")
        {
            await _publication.DidNotReceive().Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
        }
        if (failingSeam != "acceptance")
        {
            _acceptance.DidNotReceive()(Arg.Any<ScreenplayRenderPlan>(), Arg.Any<bool>());
            Directory.Exists(_destination).ShouldBeFalse();
        }
        else
        {
            Received.InOrder(() =>
            {
                _publication.Publish(Arg.Any<ArtifactPublicationRequest>(), Arg.Any<CancellationToken>());
                _acceptance(Arg.Any<ScreenplayRenderPlan>(), true);
            });
            (await File.ReadAllTextAsync(_artifactPath, TestContext.Current.CancellationToken)).ShouldEqual("allowed artifact");
            (await File.ReadAllTextAsync(_reportPath, TestContext.Current.CancellationToken)).ShouldEqual("mock degradation report");
        }
    }
}
