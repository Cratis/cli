// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PlayFiles.when_checking_for_play_files;

public class and_no_play_files_are_present : given.a_temporary_folder
{
    bool _result;

    void Establish() => File.WriteAllText(Path.Combine(_folder, "readme.md"), "not a play file");

    void Because() => _result = PlayFiles.ExistIn(_folder);

    [Fact] void should_not_find_any_play_files() => _result.ShouldBeFalse();
}
