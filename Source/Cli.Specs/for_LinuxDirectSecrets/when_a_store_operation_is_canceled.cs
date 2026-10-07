// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LinuxDirectSecrets;

public class when_a_store_operation_is_canceled : given.a_delayed_secret_tool
{
    Task Because() => Interrupt(cancel: true);

    [for_DirectFileSecrets.given.unix_only.Fact] void should_report_cancellation() => (_error is OperationCanceledException).ShouldBeTrue();
    [for_DirectFileSecrets.given.unix_only.Fact] void should_terminate_and_reap_the_writer() => _exited.ShouldBeTrue();
    [for_DirectFileSecrets.given.unix_only.Fact] void should_prevent_a_late_credential_overwrite() => _remaining.ShouldEqual("original");
}
