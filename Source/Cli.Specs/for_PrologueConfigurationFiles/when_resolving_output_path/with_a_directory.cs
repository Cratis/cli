// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationFiles.when_resolving_output_path;

public class with_a_directory : given.a_temporary_folder
{
    string _result;

    void Because() => _result = PrologueConfigurationFiles.ResolveOutputPath(_folder, "/work");

    [Fact] void should_place_the_well_known_file_within_the_directory() => _result.ShouldEqual(Path.Combine(_folder, "cratis-prologue.json"));
}
