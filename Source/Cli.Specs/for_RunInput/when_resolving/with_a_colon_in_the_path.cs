// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_RunInput.when_resolving;

public class with_a_colon_in_the_path : given.a_temporary_folder
{
    void Because() => _result = RunInput.Resolve(Path.Combine(_folder, "model:part.play"));

    [Fact] void should_not_admit_anything() => _result.Target.ShouldBeNull();
    [Fact] void should_say_the_colon_cannot_be_mounted() => _result.Error!.ShouldContain("contains a colon");
}
