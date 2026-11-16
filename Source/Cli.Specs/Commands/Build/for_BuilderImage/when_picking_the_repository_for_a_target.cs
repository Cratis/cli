// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Build.for_BuilderImage;

public class when_picking_the_repository_for_a_target : Specification
{
    [Fact] void should_pick_the_cratis_builder_for_cratis() => BuilderImage.RepositoryFor("cratis").ShouldEqual("cratis/builder");
    [Fact] void should_pick_the_axon_builder_for_axon() => BuilderImage.RepositoryFor("axon").ShouldEqual("cratis/studio-axon-builder");
    [Fact] void should_ignore_the_casing_of_the_target() => BuilderImage.RepositoryFor("Axon").ShouldEqual("cratis/studio-axon-builder");
}
