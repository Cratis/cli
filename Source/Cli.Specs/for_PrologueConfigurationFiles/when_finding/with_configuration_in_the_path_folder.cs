// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationFiles.when_finding;

public class with_configuration_in_the_path_folder : given.a_temporary_folder
{
    string _captures;
    string? _result;

    void Establish()
    {
        _captures = Directory.CreateDirectory(Path.Combine(_folder, "captures")).FullName;
        File.WriteAllText(Path.Combine(_captures, "cratis-prologue.json"), "{}");
    }

    void Because() => _result = PrologueConfigurationFiles.Find(_captures, _folder);

    [Fact] void should_find_the_configuration_in_the_path_folder() => _result.ShouldEqual(Path.Combine(_captures, "cratis-prologue.json"));
}
