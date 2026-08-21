// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConfirmationHelper.when_resolving_exit_code;

[Collection(CliSpecsCollection.Name)]
public class and_an_interactive_user_declined : Specification
{
    int? _result;

    void Because() => _result = ConfirmationHelper.ExitCodeFor(ConfirmationOutcome.Declined, OutputFormats.Plain);

    [Fact] void should_return_success() => _result.ShouldEqual(ExitCodes.Success);
}
