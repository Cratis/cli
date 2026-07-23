// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_LlmKinds.when_validating_kind;

public class with_known_kind : Specification
{
    bool _result;

    void Because() => _result = LlmKinds.All.All(LlmKinds.IsValid);

    [Fact] void should_accept_all_known_kinds() => _result.ShouldBeTrue();
}
