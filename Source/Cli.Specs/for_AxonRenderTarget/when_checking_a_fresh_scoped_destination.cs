// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render.Publication;
using Cratis.Cli.Commands.Screenplay;

namespace Cratis.Cli.for_AxonRenderTarget;

public class when_checking_a_fresh_scoped_destination : given.an_axon_target
{
    ArtifactPublicationCheckResult _result = null!;
    string _destination = null!;

    async Task Because()
    {
        _destination = Path.Combine(_folder, "fresh");
        var selection = RenderSelection.Resolve(["Projects.Registration"], SemanticAddressIndex.From(_compilation.Model.Application, true), []);
        _result = await new ArtifactPublisher().Check(
            new(_plan, _destination, false)
            {
                Target = _target,
                Selection = selection,
                ScaffoldOptions = new("Projects", "Projects", "Projects")
            },
            CancellationToken.None);
    }

    [Fact] void should_allow_the_check() => _result.Refused.ShouldEqual(0);
    [Fact] void should_not_add_cratis_files() => _result.Receipt.Changes.Count.ShouldEqual(0);
    [Fact] void should_not_write_the_destination() => Directory.Exists(_destination).ShouldBeFalse();
}
