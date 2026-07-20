// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PlayFiles.when_checking_for_play_files;

public class and_a_play_file_is_nested : given.a_temporary_folder
{
    bool _result;

    void Establish()
    {
        var nested = Directory.CreateDirectory(Path.Combine(_folder, "features", "invoicing")).FullName;
        File.WriteAllText(Path.Combine(nested, "invoicing.play"), "module Invoicing");
    }

    void Because() => _result = PlayFiles.ExistIn(_folder);

    [Fact] void should_find_the_nested_play_file() => _result.ShouldBeTrue();
}
