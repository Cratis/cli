// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConfirmationHelper.when_deciding_whether_to_proceed;

[Collection(CliSpecsCollection.Name)]
public class and_the_interactive_user_does_not_answer : given.a_confirmation_request
{
    void Because() => Decide();

    [Fact] void should_be_declined_using_the_interactive_default() => _result.ShouldEqual(ConfirmationOutcome.Declined);
    [Fact] void should_request_confirmation() => _confirmationWasRequested.ShouldBeTrue();
    [Fact] void should_default_to_declining() => _defaultConfirmation.ShouldBeFalse();
}
