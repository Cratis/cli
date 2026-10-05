// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_DesktopMcp;

public class when_parsing_unsafe_marketplaces : Specification
{
    Exception[] _errors;
    void Because() => _errors = [.. new[]
    {
        "{broken", "[]", "{\"plugins\":{}}", "{\"plugins\":[],\"plugins\":[]}",
        "{\"plugins\":[{\"name\":\"cratis-screenplay\"},{\"name\":\"cratis-screenplay\"}]}"
    }.Select(content => Catch.Exception(() => _ = new DesktopMcpMarketplace(content)))];

    [Fact] void should_reject_every_malformed_or_ambiguous_document() => _errors.All(error => error is not null).ShouldBeTrue();
}
