// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ConfirmationHelper.when_deciding_whether_to_proceed;

[Collection(CliSpecsCollection.Name)]
public class and_yes_flag_is_set : given.a_confirmation_request
{
    void Establish()
    {
        _settings.Yes = true;
        _isInteractiveEnvironment = false;
    }

    void Because() => Decide();

    [Fact] void should_be_confirmed() => _result.ShouldEqual(ConfirmationOutcome.Confirmed);
    [Fact] void should_not_request_confirmation() => _confirmationWasRequested.ShouldBeFalse();
}
