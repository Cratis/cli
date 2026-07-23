// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayOutput.when_resolving_path;

public class with_explicit_file : Specification
{
    string _result;

    void Because() => _result = ScreenplayOutput.ResolvePath("plays/MySystem.play", "Library", "/work");

    [Fact] void should_resolve_the_file_against_the_current_directory() => _result.ShouldEqual(Path.Combine("/work", "plays", "MySystem.play"));
}
