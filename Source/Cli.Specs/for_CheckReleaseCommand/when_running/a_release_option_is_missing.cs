// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts;
using Cratis.Cli.Commands.Chronicle.CheckRelease;

namespace Cratis.Cli.for_CheckReleaseCommand.when_running;

[Collection(CliSpecsCollection.Name)]
public class a_release_option_is_missing : Specification
{
    int _result;

    async Task Because() => _result = await ((ICommand<CheckReleaseSettings>)new CheckReleaseCommand()).ExecuteAsync(
        new CommandContext([], Substitute.For<IRemainingArguments>(), "check-release", null),
        new CheckReleaseSettings { Server = "chronicle://127.0.0.1:1/?disableTls=true", Output = OutputFormats.Quiet },
        CancellationToken.None);

    [Fact] void should_exit_with_validation_error() => _result.ShouldEqual(ExitCodes.ValidationError);
}
