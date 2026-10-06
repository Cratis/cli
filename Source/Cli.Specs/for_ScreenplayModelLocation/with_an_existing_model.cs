// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class with_an_existing_model : given.a_project
{
    void Establish()
    {
        Directory.CreateDirectory(PathOf("Source"));
        Play("Model/application.play");
        Play("Model/Billing/invoices.play");
    }

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_use_the_folder_holding_the_model() => _located.ShouldEqual(PathOf("Model"));
    [Fact] void should_not_create_a_folder() => Directory.Exists(PathOf("Screenplay")).ShouldBeFalse();
}
