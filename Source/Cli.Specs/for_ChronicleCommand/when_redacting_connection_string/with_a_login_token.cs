// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleCommand.when_redacting_connection_string;

public class with_a_login_token : Specification
{
    string _redacted = null!;

    void Because() => _redacted = ConnectionStringRedaction.Redact("chronicle://localhost:35000/?skipTlsValidation=true&apiKey=secret-token");

    [Fact] void should_not_print_the_token() => _redacted.ShouldNotContain("secret-token");
    [Fact] void should_keep_the_connection_details() => _redacted.ShouldContain("skipTlsValidation=true&apiKey=***");
}
