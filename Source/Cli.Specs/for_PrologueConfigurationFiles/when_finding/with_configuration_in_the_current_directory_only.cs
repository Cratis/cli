// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PrologueConfigurationFiles.when_finding;

public class with_configuration_in_the_current_directory_only : given.a_temporary_folder
{
    string _captures;
    string? _result;

    void Establish()
    {
        _captures = Directory.CreateDirectory(Path.Combine(_folder, "captures")).FullName;
        File.WriteAllText(Path.Combine(_folder, "cratis-prologue.json"), "{}");
    }

    void Because() => _result = PrologueConfigurationFiles.Find(_captures, _folder);

    [Fact] void should_fall_back_to_the_current_directory() => _result.ShouldEqual(Path.Combine(_folder, "cratis-prologue.json"));
}
