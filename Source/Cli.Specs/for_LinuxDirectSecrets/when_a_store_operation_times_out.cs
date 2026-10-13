// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_LinuxDirectSecrets;

public class when_a_store_operation_times_out : given.a_delayed_secret_tool
{
    Task Because() => Interrupt(cancel: false);

    [for_DirectFileSecrets.given.unix_only.Fact] void should_report_the_timeout() => _error.ShouldBeOfExactType<DirectAuthError>();
    [for_DirectFileSecrets.given.unix_only.Fact] void should_explain_a_locked_keyring_may_be_waiting_for_a_prompt() => _error!.Message.ShouldContain("locked keyring");
    [for_DirectFileSecrets.given.unix_only.Fact] void should_terminate_and_reap_the_writer() => _exited.ShouldBeTrue();
    [for_DirectFileSecrets.given.unix_only.Fact] void should_prevent_a_late_credential_overwrite() => _remaining.ShouldEqual("original");
}
