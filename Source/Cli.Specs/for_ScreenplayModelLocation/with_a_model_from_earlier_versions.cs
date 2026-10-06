// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_a_model_from_earlier_versions : given.a_project
{
    void Establish()
    {
        Directory.CreateDirectory(PathOf("Source"));
        Play(".cratis/screenplay/application.play");
    }

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_keep_serving_it() => _located.ShouldEqual(PathOf(".cratis/screenplay"));
}
