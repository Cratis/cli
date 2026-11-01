// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_UnexpectedErrorHandler;

public class when_handling_cancellation : given.an_error_handler
{
    void Because() => _exitCode = UnexpectedErrorHandler.Handle(new OperationCanceledException(), ["--debug"], _console);

    [Fact] void should_preserve_spectres_cancellation_exit_code() => _exitCode.ShouldEqual(130);
    [Fact] void should_not_render_an_error() => _output.ToString().ShouldBeEmpty();
}
