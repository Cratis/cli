// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.Commands.Build.for_BuildRedaction;

public class when_redacting : Specification
{
    const string HostSecret = "q4W1v9n2Jx0a8yT6pL3sZ7bK5eR1uH9cM2dF4gN8wQ0";
    const string GitHubToken = "ghp_abcdefghijklmnopqrstuvwxyz0123456789";
    BuildRedaction _redaction;
    string _text;
    JsonObject _outcome;

    void Establish() => _redaction = new([HostSecret, null, "short"]);

    void Because()
    {
        _text = _redaction.Redact($"token {HostSecret}; Authorization: Bearer abc.def-ghi_jkl; push https://user:pa55word@github.com/acme/app.git; {GitHubToken}");
        _outcome = _redaction.Redact(new JsonObject { ["kind"] = "failed", ["error"] = $"git said {HostSecret}", ["questions"] = new JsonArray(GitHubToken) });
    }

    [Fact] void should_remove_a_credential_the_host_holds() => _text.ShouldNotContain(HostSecret);
    [Fact] void should_remove_a_bearer_token() => _text.ShouldContain("Bearer [redacted]");
    [Fact] void should_remove_a_credential_in_a_url() => _text.ShouldContain("https://user:[redacted]@github.com");
    [Fact] void should_remove_a_well_known_token() => _text.ShouldNotContain(GitHubToken);
    [Fact] void should_remove_credentials_from_an_outcome() => _outcome.ToJsonString().ShouldNotContain(HostSecret);
    [Fact] void should_remove_credentials_from_nested_outcome_members() => _outcome["questions"]![0]!.GetValue<string>().ShouldEqual(BuildRedaction.Mask);
}
