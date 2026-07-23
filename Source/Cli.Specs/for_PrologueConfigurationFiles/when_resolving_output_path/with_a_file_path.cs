// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationFiles.when_resolving_output_path;

public class with_a_file_path : Specification
{
    string _result;

    void Because() => _result = PrologueConfigurationFiles.ResolveOutputPath("configs/my-prologue.json", "/work");

    [Fact] void should_resolve_the_file_against_the_current_directory() => _result.ShouldEqual(Path.Combine("/work", "configs", "my-prologue.json"));
}
