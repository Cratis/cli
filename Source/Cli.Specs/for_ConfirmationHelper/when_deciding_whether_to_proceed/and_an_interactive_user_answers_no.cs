// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConfirmationHelper.when_deciding_whether_to_proceed;

[Collection(CliSpecsCollection.Name)]
public class and_an_interactive_user_answers_no : given.a_confirmation_request
{
    void Establish() => _confirmationAnswer = false;

    void Because() => Decide();

    [Fact] void should_be_declined() => _result.ShouldEqual(ConfirmationOutcome.Declined);
    [Fact] void should_request_confirmation() => _confirmationWasRequested.ShouldBeTrue();
}
