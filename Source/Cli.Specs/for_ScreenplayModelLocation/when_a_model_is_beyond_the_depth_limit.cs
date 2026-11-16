// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class when_a_model_is_beyond_the_depth_limit : given.a_project
{
    void Establish() => Play(Path.Combine([.. Enumerable.Repeat("Nested", 11), "application.play"]));

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_fall_back_without_descending_further() => _located.ShouldEqual(PathOf("Screenplay"));
}
