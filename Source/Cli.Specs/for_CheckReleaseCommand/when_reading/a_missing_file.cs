// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_reading;

public class a_missing_file : given.a_release_file
{
    void Because() => _valid = ReleaseReader.TryRead(_path, out _release, out _problem);

    [Fact] void should_be_invalid() => _valid.ShouldBeFalse();
    [Fact] void should_say_it_could_not_be_read() => _problem.ShouldContain("could not be read");
}
