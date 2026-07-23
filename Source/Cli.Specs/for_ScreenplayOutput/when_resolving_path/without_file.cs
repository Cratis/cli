// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayOutput.when_resolving_path;

public class without_file : Specification
{
    string _result;

    void Because() => _result = ScreenplayOutput.ResolvePath(null, "library system", "/work");

    [Fact] void should_derive_the_file_from_the_system_name_in_the_current_directory() => _result.ShouldEqual(Path.Combine("/work", "LibrarySystem.play"));
}
