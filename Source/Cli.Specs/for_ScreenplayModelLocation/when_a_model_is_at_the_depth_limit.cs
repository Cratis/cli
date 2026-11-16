// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class when_a_model_is_at_the_depth_limit : given.a_project
{
    string _model;

    void Establish()
    {
        _model = Path.Combine([.. Enumerable.Repeat("Nested", 10)]);
        Play(Path.Combine(_model, "application.play"));
    }

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_find_the_model() => _located.ShouldEqual(PathOf(_model));
}
