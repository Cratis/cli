// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CheckReleaseCommand.when_reading;

public class a_release_with_a_malformed_descriptor_set : given.a_release_file
{
    void Because() => Read(Valid.Replace("AQID", "not base64!"));

    [Fact] void should_be_invalid() => _valid.ShouldBeFalse();
    [Fact] void should_describe_the_descriptor_set() => _problem.ShouldContain("descriptorSet");
}
