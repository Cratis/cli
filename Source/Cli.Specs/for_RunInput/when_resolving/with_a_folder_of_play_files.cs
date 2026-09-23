// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.when_resolving;

public class with_a_folder_of_play_files : given.a_temporary_folder
{
    string _path;

    void Establish()
    {
        _path = Directory.CreateDirectory(Path.Combine(_folder, "selected")).FullName;
        File.WriteAllText(Path.Combine(_path, "invoicing.PLAY"), "domain Invoicing\n");
        File.WriteAllText(Path.Combine(_folder, "parent.play"), "domain Parent\n");
    }

    void Because() => _result = RunInput.Resolve(_path);

    [Fact] void should_admit_it() => _result.Error.ShouldBeNull();
    [Fact] void should_admit_it_as_a_folder() => _result.Target.ShouldBeOfExactType<DirectoryInfo>();
    [Fact] void should_select_only_that_folder() => _result.Target!.FullName.ShouldEqual(_path);
}
