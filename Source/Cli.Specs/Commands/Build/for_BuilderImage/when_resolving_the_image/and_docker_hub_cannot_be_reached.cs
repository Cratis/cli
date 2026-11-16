// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderImage.when_resolving_the_image;

public class and_docker_hub_cannot_be_reached : given.a_resolution
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => Resolve(null, "cratis"));

    [Fact] void should_fail_as_unavailable() => _error.ShouldBeOfExactType<BuilderImageUnavailable>();
    [Fact] void should_name_the_image() => _error.Message.ShouldContain("cratis/builder");
}
