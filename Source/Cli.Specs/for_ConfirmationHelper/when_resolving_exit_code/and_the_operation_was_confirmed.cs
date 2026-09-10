// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConfirmationHelper.when_resolving_exit_code;

[Collection(CliSpecsCollection.Name)]
public class and_the_operation_was_confirmed : Specification
{
    int? _result;

    void Because() => _result = ConfirmationHelper.ExitCodeFor(ConfirmationOutcome.Confirmed, OutputFormats.Plain);

    [Fact] void should_not_return_an_exit_code() => _result.ShouldBeNull();
}
