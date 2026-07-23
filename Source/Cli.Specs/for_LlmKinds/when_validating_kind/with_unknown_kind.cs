// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LlmKinds.when_validating_kind;

public class with_unknown_kind : Specification
{
    bool _result;

    void Because() => _result = LlmKinds.IsValid("gemini");

    [Fact] void should_not_be_valid() => _result.ShouldBeFalse();
}
