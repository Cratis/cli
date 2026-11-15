// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;

namespace Cratis.Cli.for_RenderProfileVersion;

public class when_resolving_without_a_destination : Specification
{
    string? _version;

    void Because() => _version = RenderProfileVersion.Resolve(null, upgrade: false).Version;

    [Fact] void should_leave_the_choice_to_stage() => _version.ShouldBeNull();
}
