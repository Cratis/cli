// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class without_a_source_folder : given.a_project
{
    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_fall_back_to_a_screenplay_folder() => _located.ShouldEqual(PathOf("Screenplay"));
    [Fact] void should_create_it() => Directory.Exists(PathOf("Screenplay")).ShouldBeTrue();
    [Fact] void should_not_touch_the_configuration_folder() => Directory.Exists(PathOf(".cratis")).ShouldBeFalse();
}
