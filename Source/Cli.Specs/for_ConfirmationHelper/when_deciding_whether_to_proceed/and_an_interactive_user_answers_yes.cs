// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConfirmationHelper.when_deciding_whether_to_proceed;

[Collection(CliSpecsCollection.Name)]
public class and_an_interactive_user_answers_yes : given.a_confirmation_request
{
    void Establish() => _confirmationAnswer = true;

    void Because() => Decide();

    [Fact] void should_be_confirmed() => _result.ShouldEqual(ConfirmationOutcome.Confirmed);
    [Fact] void should_request_confirmation() => _confirmationWasRequested.ShouldBeTrue();
}
