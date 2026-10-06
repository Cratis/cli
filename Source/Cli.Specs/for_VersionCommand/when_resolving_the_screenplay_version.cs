// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Version;

namespace Cratis.Cli.for_VersionCommand;

public class when_resolving_the_screenplay_version : Specification
{
    string _result;
    string _assemblyVersion;

    void Establish()
    {
        var version = typeof(Cratis.Screenplay.ScreenplayCompiler).Assembly.GetName().Version!;
        _assemblyVersion = $"{version.Major}.{version.Minor}.";
    }

    void Because() => _result = VersionCommand.GetScreenplayVersion();

    [Fact] void should_not_contain_build_metadata() => _result.ShouldNotContain("+");
    [Fact] void should_be_the_bundled_screenplay_version() => _result.StartsWith(_assemblyVersion, StringComparison.Ordinal).ShouldBeTrue();
}
