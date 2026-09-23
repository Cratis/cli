// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.when_resolving;

public class with_a_file_that_is_not_a_screenplay : given.a_temporary_folder
{
    string _path;

    void Establish()
    {
        _path = Path.Combine(_folder, "invoicing.play.txt");
        File.WriteAllText(_path, "not a screenplay");
        File.WriteAllText(Path.Combine(_folder, "sibling.play"), "domain Sibling\n");
    }

    void Because() => _result = RunInput.Resolve(_path);

    [Fact] void should_not_admit_anything() => _result.Target.ShouldBeNull();
    [Fact] void should_say_the_file_is_not_a_screenplay() => _result.Error!.ShouldContain("is not a Screenplay (.play) file");
}
