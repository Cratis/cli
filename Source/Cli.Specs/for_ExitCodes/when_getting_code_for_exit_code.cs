// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ExitCodes;

public class when_getting_code_for_exit_code : Specification
{
    [Fact] void should_return_success_for_zero() => ExitCodes.CodeFor(0).ShouldEqual("success");
    [Fact] void should_return_not_found_for_one() => ExitCodes.CodeFor(1).ShouldEqual("not_found");
    [Fact] void should_return_connection_error_for_two() => ExitCodes.CodeFor(2).ShouldEqual("connection_error");
    [Fact] void should_return_server_error_for_three() => ExitCodes.CodeFor(3).ShouldEqual("server_error");
    [Fact] void should_return_authentication_error_for_four() => ExitCodes.CodeFor(4).ShouldEqual("authentication_error");
    [Fact] void should_return_validation_error_for_five() => ExitCodes.CodeFor(5).ShouldEqual("validation_error");
    [Fact] void should_return_unknown_for_unrecognized() => ExitCodes.CodeFor(99).ShouldEqual("unknown");
}
