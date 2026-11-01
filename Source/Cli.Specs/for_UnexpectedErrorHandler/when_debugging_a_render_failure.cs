// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UnexpectedErrorHandler;

public class when_debugging_a_render_failure : given.an_error_handler
{
    void Because() => _exitCode = UnexpectedErrorHandler.Handle(new RenderFailedUnexpectedly("planning", _failure), ["render", "--debug", "-o", "plain"], _console);

    [Fact] void should_return_unexpected_error() => _exitCode.ShouldEqual(10);
    [Fact] void should_print_the_throw_site() => _output.ToString().ShouldContain("ThrowRendererFailure");
}
