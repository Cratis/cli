// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationFiles.when_resolving_output_path;

public class without_a_file : Specification
{
    string _result;

    void Because() => _result = PrologueConfigurationFiles.ResolveOutputPath(null, "/work");

    [Fact] void should_default_to_the_well_known_file_in_the_current_directory() => _result.ShouldEqual(Path.Combine("/work", "cratis-prologue.json"));
}
