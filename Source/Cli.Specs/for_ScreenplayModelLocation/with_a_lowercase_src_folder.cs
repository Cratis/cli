// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_a_lowercase_src_folder : given.a_project
{
    void Establish() => Directory.CreateDirectory(PathOf("src"));

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_build_from_the_src_folder() => _located.ShouldEqual(PathOf("src"));
}
