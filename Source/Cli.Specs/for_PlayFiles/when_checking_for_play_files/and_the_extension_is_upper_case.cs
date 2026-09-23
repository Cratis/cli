// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PlayFiles.when_checking_for_play_files;

public class and_the_extension_is_upper_case : given.a_temporary_folder
{
    bool _result;

    void Establish()
    {
        var nested = Directory.CreateDirectory(Path.Combine(_folder, "features")).FullName;
        File.WriteAllText(Path.Combine(nested, "Invoicing.PLAY"), "module Invoicing");
    }

    void Because() => _result = PlayFiles.ExistIn(_folder);

    [Fact] void should_find_the_play_file() => _result.ShouldBeTrue();
}
