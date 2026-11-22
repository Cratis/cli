// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_CheckReleaseCommand.when_reading;

public class a_release_with_an_invalid_minimum_kernel_version : given.a_release_file
{
    void Because() => Read(Valid.Replace("\"descriptorSet\"", "\"minimumKernelVersion\": \"latest\", \"descriptorSet\""));

    [Fact] void should_be_invalid() => _valid.ShouldBeFalse();
    [Fact] void should_describe_the_version() => _problem.ShouldContain("minimumKernelVersion");
}
