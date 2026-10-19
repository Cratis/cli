// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_a_source_folder : given.a_project
{
    void Establish() => Directory.CreateDirectory(PathOf("Source"));

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_fall_back_to_a_screenplay_folder() => _located.ShouldEqual(PathOf("Screenplay"));
    [Fact] void should_not_create_it() => Directory.Exists(PathOf("Screenplay")).ShouldBeFalse();
}
