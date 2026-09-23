// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.when_resolving;

public class with_a_mixed_case_extension : given.a_temporary_folder
{
    string _path;

    void Establish()
    {
        _path = Path.Combine(_folder, "selected.PlAy");
        File.WriteAllText(_path, "domain Selected\n");
    }

    void Because() => _result = RunInput.Resolve(_path);

    [Fact] void should_admit_it_as_a_file() => _result.Target.ShouldBeOfExactType<FileInfo>();
    [Fact] void should_keep_the_casing_of_the_path() => _result.Target!.FullName.ShouldEqual(_path);
}
