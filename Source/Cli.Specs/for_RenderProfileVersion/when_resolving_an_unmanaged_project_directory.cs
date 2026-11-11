// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Render;

namespace Cratis.Cli.for_RenderProfileVersion;

public class when_resolving_an_unmanaged_project_directory : given.a_rendered_destination
{
    string? _version;

    void Establish() => File.WriteAllText(Path.Combine(_destination, "Directory.Build.props"), "<Project />");

    void Because() => _version = RenderProfileVersion.Resolve(_destination, upgrade: false).Version;

    [Fact] void should_not_treat_it_as_a_new_application() => _version.ShouldEqual("3");
}
