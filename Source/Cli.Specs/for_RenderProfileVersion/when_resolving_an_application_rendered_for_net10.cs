// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;

namespace Cratis.Cli.for_RenderProfileVersion;

public class when_resolving_an_application_rendered_for_net10 : given.a_rendered_destination
{
    string? _version;

    void Establish() => RenderedWithTargetFramework("net10.0");

    void Because() => _version = RenderProfileVersion.Resolve(_destination, upgrade: false).Version;

    [Fact] void should_keep_the_net10_profile() => _version.ShouldEqual("3");
}
