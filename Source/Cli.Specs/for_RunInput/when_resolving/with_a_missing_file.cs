// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.when_resolving;

public class with_a_missing_file : given.a_temporary_folder
{
    void Establish() => File.WriteAllText(Path.Combine(_folder, "sibling.play"), "domain Sibling\n");

    void Because() => _result = RunInput.Resolve(Path.Combine(_folder, "missing.play"));

    [Fact] void should_not_admit_anything() => _result.Target.ShouldBeNull();
    [Fact] void should_say_the_path_does_not_exist() => _result.Error!.ShouldContain("does not exist");
}
