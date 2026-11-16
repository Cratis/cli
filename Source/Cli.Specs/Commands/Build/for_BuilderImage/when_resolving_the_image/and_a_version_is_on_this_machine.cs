// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderImage.when_resolving_the_image;

public class and_a_version_is_on_this_machine : given.a_resolution
{
    BuilderImageChoice _result;

    void Establish()
    {
        _local["cratis/studio-axon-builder"] = "1.4.0";
        _published["cratis/studio-axon-builder"] = "1.5.0";
    }

    async Task Because() => _result = await Resolve(null, "axon");

    [Fact] void should_use_the_newest_local_version() => _result.Reference.ShouldEqual("cratis/studio-axon-builder:1.4.0");
    [Fact] void should_not_pull() => _result.NeedsPull.ShouldBeFalse();
    [Fact] void should_not_ask_docker_hub() => _askedPublished.ShouldBeEmpty();
}
