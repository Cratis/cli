// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Extension;

namespace Cratis.Cli.Extensions.for_ExtensionCommandSupport;

public class when_mapping_a_report_to_an_exit_code : Specification
{
    static ExtensionReport Report(params ExtensionFailureKind[] kinds) =>
        new([.. kinds.Select(kind => new ExtensionOutcome("r", "failed", Failure: kind))], [], false);

    [Fact] void should_succeed_when_nothing_failed() => ExtensionCommandSupport.ExitCodeOf(new([new("r", "cached")], [], false)).ShouldEqual(ExitCodes.Success);
    [Fact] void should_report_an_unresolvable_reference_as_not_found() => ExtensionCommandSupport.ExitCodeOf(Report(ExtensionFailureKind.Unresolvable)).ShouldEqual(ExitCodes.NotFound);
    [Fact] void should_report_an_unreachable_registry_as_a_connection_error() => ExtensionCommandSupport.ExitCodeOf(Report(ExtensionFailureKind.Unreachable)).ShouldEqual(ExitCodes.ConnectionError);
    [Fact] void should_report_refused_credentials_as_an_authentication_error() => ExtensionCommandSupport.ExitCodeOf(Report(ExtensionFailureKind.Unauthorized, ExtensionFailureKind.Unresolvable)).ShouldEqual(ExitCodes.AuthenticationError);
    [Fact] void should_report_an_unallowed_publisher_as_a_validation_error() => ExtensionCommandSupport.ExitCodeOf(Report(ExtensionFailureKind.Unallowed)).ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_an_unsigned_package_as_a_validation_error() => ExtensionCommandSupport.ExitCodeOf(Report(ExtensionFailureKind.NotSigned)).ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_a_mismatched_package_as_a_validation_error() => ExtensionCommandSupport.ExitCodeOf(Report(ExtensionFailureKind.Mismatched)).ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_an_unlocked_reference_as_a_validation_error() => ExtensionCommandSupport.ExitCodeOf(Report(ExtensionFailureKind.NotLocked)).ShouldEqual(ExitCodes.ValidationError);
    [Fact] void should_report_an_unresolvable_reference_mixed_with_a_mismatch_as_a_validation_error() => ExtensionCommandSupport.ExitCodeOf(Report(ExtensionFailureKind.Unresolvable, ExtensionFailureKind.Mismatched)).ShouldEqual(ExitCodes.ValidationError);
}
