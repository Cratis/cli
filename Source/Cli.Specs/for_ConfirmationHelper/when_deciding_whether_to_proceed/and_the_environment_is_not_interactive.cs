// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConfirmationHelper.when_deciding_whether_to_proceed;

[Collection(CliSpecsCollection.Name)]
public class and_the_environment_is_not_interactive : given.a_confirmation_request
{
    void Establish() => _isInteractiveEnvironment = false;

    void Because() => Decide();

    [Fact] void should_require_confirmation() => _result.ShouldEqual(ConfirmationOutcome.ConfirmationRequired);
    [Fact] void should_not_request_confirmation() => _confirmationWasRequested.ShouldBeFalse();
}
