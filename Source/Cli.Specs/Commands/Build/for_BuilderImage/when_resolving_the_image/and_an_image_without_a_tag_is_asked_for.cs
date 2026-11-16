// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderImage.when_resolving_the_image;

public class and_an_image_without_a_tag_is_asked_for : given.a_resolution
{
    BuilderImageChoice _result;

    void Establish() => _published["cratis/studio-eventmodelers-builder"] = "2.1.0";

    async Task Because() => _result = await Resolve("cratis/studio-eventmodelers-builder", "cratis");

    [Fact] void should_resolve_the_version_of_that_image() => _result.Reference.ShouldEqual("cratis/studio-eventmodelers-builder:2.1.0");
}
