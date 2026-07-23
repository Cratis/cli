// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LlmKinds.when_validating_kind;

public class with_mixed_casing : Specification
{
    bool _result;

    void Because() => _result = LlmKinds.IsValid("Anthropic");

    [Fact] void should_be_valid() => _result.ShouldBeTrue();
}
