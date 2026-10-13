// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectLoginFlow.when_reusing_plaintext_consent;

public class for_another_tenant : Specification
{
    bool _allowed;

    void Because() => _allowed = DirectLoginFlow.UseInsecureFileStore(new DirectSettings(), new DirectConfiguration { Origin = "https://direct.example", Tenant = "team", InsecureFileStore = true }, DirectTarget.Create("https://direct.example", "other"));

    [Fact] void should_require_explicit_consent_again() => _allowed.ShouldBeFalse();
}
