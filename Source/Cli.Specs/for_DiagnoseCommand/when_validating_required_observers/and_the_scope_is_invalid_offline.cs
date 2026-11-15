// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DiagnoseCommand.when_validating_required_observers;

[Collection(CliSpecsCollection.Name)]
public class and_the_scope_is_invalid_offline : Specification
{
    int _exitCode;

    async Task Because() => _exitCode = await new DiagnoseCommand().ExecuteAsync(
        null!,
        new DiagnoseSettings
        {
            Server = "chronicle://localhost:1",
            RequireObserver = ["required"]
        },
        CancellationToken.None);

    [Fact] void should_reject_arguments_before_connecting() => _exitCode.ShouldEqual(ExitCodes.ValidationError);
}
