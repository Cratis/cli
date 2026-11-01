// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UnexpectedErrorHandler;

public class when_handling_a_generic_failure : given.an_error_handler
{
    void Because() => _exitCode = UnexpectedErrorHandler.Handle(_failure, ["--output=json"], _console);

    [Fact] void should_return_unexpected_error() => _exitCode.ShouldEqual(10);
    [Fact] void should_identify_the_exception() => _output.ToString().ShouldContain("System.ArgumentException");
    [Fact] void should_preserve_the_message() => _output.ToString().ShouldContain("Unexpected renderer failure.");
    [Fact] void should_report_the_machine_error_code() => _output.ToString().ShouldContain("unexpected_error");
    [Fact] void should_not_print_stack_frames() => _output.ToString().ShouldNotContain("   at ");
    [Fact] void should_not_print_the_throw_site() => _output.ToString().ShouldNotContain("ThrowRendererFailure");
}
