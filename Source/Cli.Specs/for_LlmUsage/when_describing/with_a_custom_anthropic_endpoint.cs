// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmUsage.when_describing;

public class with_a_custom_anthropic_endpoint : Specification
{
    LlmUsage _result;

    void Because() => _result = LlmUsage.From(new LlmOptions { Enabled = true, Kind = LlmKind.Anthropic, Endpoint = "https://user:secret@gateway.example/path?token=hidden" }, "local file");

    [Fact] void should_name_only_the_custom_host() => _result.EndpointHost.ShouldEqual("gateway.example");
    [Fact] void should_not_expose_credentials_in_the_notice() => _result.Notice.ShouldNotContain("secret");
    [Fact] void should_not_expose_paths_in_the_notice() => _result.Notice.ShouldNotContain("/path");
    [Fact] void should_not_expose_query_strings_in_the_notice() => _result.Notice.ShouldNotContain("hidden");
}
