// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_PlayFiles.when_checking_for_play_files;

public class and_the_folder_does_not_exist : Specification
{
    string _folder;
    bool _result;

    void Establish() => _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    void Because() => _result = PlayFiles.ExistIn(_folder);

    [Fact] void should_not_find_any_play_files() => _result.ShouldBeFalse();
}
