// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleCommand.when_redacting_connection_string;

public class with_both_forms_of_credentials : Specification
{
    string _redacted = null!;

    void Because() => _redacted = ConnectionStringRedaction.Redact("chronicle://client:client-secret@localhost:35000/?APIKEY=secret-token");

    [Fact] void should_not_print_the_client_secret() => _redacted.ShouldNotContain("client-secret");
    [Fact] void should_not_print_the_token() => _redacted.ShouldNotContain("secret-token");
    [Fact] void should_keep_the_identity() => _redacted.ShouldContain("client:***@localhost");
}
