// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderImage.when_resolving_the_image;

public class and_none_is_on_this_machine : given.a_resolution
{
    BuilderImageChoice _result;

    void Establish() => _published["cratis/builder"] = "1.5.0";

    async Task Because() => _result = await Resolve(null, "cratis");

    [Fact] void should_use_the_newest_published_version() => _result.Reference.ShouldEqual("cratis/builder:1.5.0");
    [Fact] void should_pull_it() => _result.NeedsPull.ShouldBeTrue();
}
