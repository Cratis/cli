// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ScreenplayProjectSources.when_canonicalizing;

public class with_an_invalid_physical_path : Specification
{
    Exception _exception;

    async Task Because() => _exception = await Catch.Exception(
        () => Task.FromResult(ScreenplayProjectSources.CanonicalPathOf("/workspace/Invalid\0Project.csproj")));

    [Fact] void should_report_a_source_path_failure() => _exception.ShouldBeOfExactType<InvalidScreenplayProjectSource>();
    [Fact] void should_not_echo_the_malformed_path() => _exception.Message.ShouldEqual("A physical project path cannot be canonicalized safely");
    [Fact] void should_retain_the_filesystem_failure_as_the_inner_exception() => _exception.InnerException.ShouldNotBeNull();
}
