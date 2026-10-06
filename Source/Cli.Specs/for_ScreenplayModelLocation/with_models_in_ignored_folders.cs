// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_models_in_ignored_folders : given.a_project
{
    void Establish()
    {
        Play("node_modules/pkg/sample.play");
        Play("bin/Debug/sample.play");
    }

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_ignore_them() => _located.ShouldEqual(PathOf("Screenplay"));
}
