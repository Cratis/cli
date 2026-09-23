// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.when_resolving;

public class with_a_folder_without_play_files : given.a_temporary_folder
{
    string _path;

    void Establish()
    {
        _path = Directory.CreateDirectory(Path.Combine(_folder, "empty")).FullName;
        File.WriteAllText(Path.Combine(_path, "notes.txt"), "not a screenplay");
        File.WriteAllText(Path.Combine(_folder, "parent.play"), "domain Parent\n");
    }

    void Because() => _result = RunInput.Resolve(_path);

    [Fact] void should_not_admit_anything() => _result.Target.ShouldBeNull();
    [Fact] void should_say_the_folder_has_no_screenplay_files() => _result.Error!.ShouldContain("No Screenplay files (.play) found in the folder");
}
