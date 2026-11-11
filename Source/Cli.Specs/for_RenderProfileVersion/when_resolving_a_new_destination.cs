// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;

namespace Cratis.Cli.for_RenderProfileVersion;

public class when_resolving_a_new_destination : given.a_rendered_destination
{
    (string? Version, Cratis.Cli.Commands.Screenplay.ScreenplayDiagnostic? Error) _result;

    void Because() => _result = RenderProfileVersion.Resolve(_destination, upgrade: false);

    [Fact] void should_select_the_net11_profile() => _result.Version.ShouldEqual("4");
    [Fact] void should_not_report_an_error() => _result.Error.ShouldBeNull();
}
