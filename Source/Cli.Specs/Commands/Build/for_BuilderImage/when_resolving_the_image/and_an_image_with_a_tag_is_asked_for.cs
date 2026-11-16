// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderImage.when_resolving_the_image;

public class and_an_image_with_a_tag_is_asked_for : given.a_resolution
{
    BuilderImageChoice _result;

    void Establish()
    {
        _local["localhost:5000/builder"] = "9.9.9";
        _published["localhost:5000/builder"] = "9.9.9";
    }

    async Task Because() => _result = await Resolve("localhost:5000/builder:1.0.0", "axon");

    [Fact] void should_use_it_as_given() => _result.Reference.ShouldEqual("localhost:5000/builder:1.0.0");
    [Fact] void should_leave_pulling_to_docker() => _result.NeedsPull.ShouldBeFalse();
    [Fact] void should_not_ask_docker_hub() => _askedPublished.ShouldBeEmpty();
}
