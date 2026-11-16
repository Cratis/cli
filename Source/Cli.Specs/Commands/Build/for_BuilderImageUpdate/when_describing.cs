// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderImageUpdate;

public class when_describing : Specification
{
    [Fact]
    void should_hint_at_a_newer_image() =>
        BuilderImageUpdate.HintFor(new("cratis/builder", "1.5.0")).ShouldEqual("Builder image update available: cratis/builder 1.5.0 - run 'cratis update'");

    [Fact]
    void should_report_an_update() =>
        BuilderImageUpdate.Describe(new("cratis/builder", "1.4.0", "1.5.0", true)).ShouldEqual("Updated the builder image cratis/builder from 1.4.0 to 1.5.0");

    [Fact]
    void should_report_an_image_already_at_the_latest_version() =>
        BuilderImageUpdate.Describe(new("cratis/builder", "1.5.0", "1.5.0", false)).ShouldEqual("The builder image cratis/builder is already at the latest version (1.5.0)");
}
