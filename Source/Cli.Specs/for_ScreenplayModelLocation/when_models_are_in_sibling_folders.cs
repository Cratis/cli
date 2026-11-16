// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayModelLocation;

public class when_models_are_in_sibling_folders : given.a_project
{
    void Establish()
    {
        Play("Models/Billing/invoices.play");
        Play("Models/Shipping/shipments.PLAY");
    }

    void Because() => _located = ScreenplayModelLocation.Locate(_project);

    [Fact] void should_use_the_deepest_common_folder() => _located.ShouldEqual(PathOf("Models"));
}
