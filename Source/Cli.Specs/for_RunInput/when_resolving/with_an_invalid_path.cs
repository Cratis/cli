// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.when_resolving;

public class with_an_invalid_path : given.a_temporary_folder
{
    void Because() => _result = RunInput.Resolve("invalid\0.play");

    [Fact] void should_not_admit_anything() => _result.Target.ShouldBeNull();
    [Fact] void should_say_the_path_is_invalid() => _result.Error!.ShouldContain("path is invalid");
}
