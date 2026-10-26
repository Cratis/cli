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
    [Fact] void should_return_changes_pending_for_six() => ExitCodes.CodeFor(6).ShouldEqual("changes_pending");
    [Fact] void should_reserve_nine_for_strict_unaccepted() => ExitCodes.StrictUnaccepted.ShouldEqual(9);
    [Fact] void should_return_strict_unaccepted_for_nine() => ExitCodes.CodeFor(9).ShouldEqual(ExitCodes.StrictUnacceptedCode);
    [Fact] void should_use_the_strict_unaccepted_machine_code() => ExitCodes.StrictUnacceptedCode.ShouldEqual("strict_unaccepted");
    [Fact] void should_preserve_drift_detected_at_seven() => ExitCodes.DriftDetected.ShouldEqual(7);
    [Fact] void should_preserve_inconclusive_at_eight() => ExitCodes.Inconclusive.ShouldEqual(8);
    [Fact] void should_preserve_the_drift_detected_mapping() => ExitCodes.CodeFor(7).ShouldEqual("drift_detected");
    [Fact] void should_preserve_the_inconclusive_mapping() => ExitCodes.CodeFor(8).ShouldEqual("inconclusive");
    [Fact] void should_return_unknown_for_negative_values() => ExitCodes.CodeFor(-1).ShouldEqual("unknown");
    [Fact] void should_return_unexpected_error_for_ten() => ExitCodes.CodeFor(10).ShouldEqual("unexpected_error");
    [Fact] void should_return_unknown_for_unrecognized() => ExitCodes.CodeFor(99).ShouldEqual("unknown");
}
