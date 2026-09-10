// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.when_resolving;

public class with_a_play_named_folder_and_a_nested_file : given.a_temporary_folder
{
    string _path;
    RunInput _result;

    void Establish()
    {
        _path = Directory.CreateDirectory(Path.Combine(_folder, "models.play")).FullName;
        var nested = Directory.CreateDirectory(Path.Combine(_path, "nested")).FullName;
        File.WriteAllText(Path.Combine(nested, "model.play"), "domain Nested\n");
    }

    void Because() => _result = RunInput.Resolve(_path);

    [Fact] void should_admit_the_folder_using_recursive_discovery() => _result.Error.ShouldBeNull();
    [Fact] void should_classify_it_as_a_directory() => _result.Target.ShouldBeOfExactType<DirectoryInfo>();
    [Fact] void should_keep_the_selected_folder() => _result.Target!.FullName.ShouldEqual(_path);
}
