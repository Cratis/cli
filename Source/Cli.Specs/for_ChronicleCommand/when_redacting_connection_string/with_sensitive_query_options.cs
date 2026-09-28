// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleCommand.when_redacting_connection_string;

public class with_sensitive_query_options : Specification
{
    string _redacted = null!;

    void Because() => _redacted = ConnectionStringRedaction.Redact(
        "chronicle://localhost:35000/?certificatePath=%2Ftmp%2Fserver.pfx&certificatePassword=pfx-pass&clientSecret=client-pass&accessToken=token-value&password=plain-pass&apiKey=api-key");

    [Fact] void should_hide_the_certificate_password() => _redacted.ShouldNotContain("pfx-pass");
    [Fact] void should_hide_the_client_secret() => _redacted.ShouldNotContain("client-pass");
    [Fact] void should_hide_the_access_token() => _redacted.ShouldNotContain("token-value");
    [Fact] void should_hide_the_password() => _redacted.ShouldNotContain("plain-pass");
    [Fact] void should_hide_the_api_key() => _redacted.ShouldNotContain("api-key");
    [Fact] void should_preserve_non_secret_options() => _redacted.ShouldContain("certificatePath=%2Ftmp%2Fserver.pfx&certificatePassword=***");
}
