// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Commands.Run.for_StageExtensions;

public class when_a_registry_host_has_a_port : Specification
{
    const string Token = "token-that-must-not-be-repeated";

    Exception _exception;

    void Because()
    {
        var plan = StageExtensions.Create(
            new([], [], []),
            new([]),
            new Dictionary<string, IReadOnlyList<string>>(),
            new Dictionary<string, string> { ["extensions.registry.localhost:5000.token"] = Token },
            "/cache",
            false);
        _exception = Catch.Exception(() => plan.RenderSecretsFile());
    }

    [Fact] void should_refuse() => _exception.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_name_the_host() => _exception.Message.ShouldContain("localhost:5000");
    [Fact] void should_never_repeat_the_credential() => _exception.Message.ShouldNotContain(Token);
}
