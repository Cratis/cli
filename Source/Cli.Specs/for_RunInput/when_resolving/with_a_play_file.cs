// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.when_resolving;

public class with_a_play_file : given.a_temporary_folder
{
    string _path;
    RunInput _result;

    void Establish()
    {
        _path = Path.Combine(_folder, "selected model.play");
        File.WriteAllText(_path, "domain Selected\n");
        File.WriteAllText(Path.Combine(_folder, "sibling.play"), "domain Sibling\n");
    }

    void Because() => _result = RunInput.Resolve(_path);

    [Fact] void should_admit_the_file() => _result.Error.ShouldBeNull();
    [Fact] void should_classify_it_as_a_file() => _result.Target.ShouldBeOfExactType<FileInfo>();
    [Fact] void should_select_only_the_exact_file() => _result.Target!.FullName.ShouldEqual(_path);
}
