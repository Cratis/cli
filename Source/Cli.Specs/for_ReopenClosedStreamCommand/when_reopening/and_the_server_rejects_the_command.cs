// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Validation;

namespace Cratis.Cli.for_ReopenClosedStreamCommand.when_reopening;

public class and_the_server_rejects_the_command : given.a_reopen_closed_stream_command
{
    [Theory]
    [InlineData("authorization")]
    [InlineData("validation")]
    [InlineData("exception")]
    async Task should_not_trust_the_response_of_a_failed_command(string failure)
    {
        switch (failure)
        {
            case "authorization":
                _response.IsAuthorized = false;
                _response.AuthorizationFailureReason = "Authenticated principal required";
                break;
            case "validation":
                _response.ValidationResults.Add(new ValidationResult { Message = "Authenticated principal required" });
                break;
            case "exception":
                _response.ExceptionMessages.Add("Authenticated principal required");
                break;
        }

        var result = await Execute();
        result.ShouldEqual(ExitCodes.ServerError);
        _error.ToString().ShouldContain("Authenticated principal required");
        _output.ToString().ShouldBeEmpty();
    }
}
