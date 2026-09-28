// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;
using Cratis.Cli.Commands.Direct;

namespace Cratis.Cli.for_DirectChallenge;

public class when_creating_a_challenge : Specification
{
    DirectChallenge _first = null!;
    DirectChallenge _second = null!;

    void Because()
    {
        _first = DirectChallenge.Create();
        _second = DirectChallenge.Create();
    }

    [Fact] void should_use_a_high_entropy_verifier() => _first.Verifier.Length.ShouldBeGreaterThan(40);
    [Fact] void should_use_s256_in_base64url_encoding()
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(_first.Verifier));
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_').ShouldEqual(_first.Challenge);
    }
    [Fact] void should_generate_a_distinct_verifier() => _first.Verifier.ShouldNotEqual(_second.Verifier);
    [Fact] void should_generate_a_distinct_state() => _first.State.ShouldNotEqual(_second.State);
}
