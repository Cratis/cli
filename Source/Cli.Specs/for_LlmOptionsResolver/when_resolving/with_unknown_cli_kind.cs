// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmOptionsResolver.when_resolving;

public class with_unknown_cli_kind : Specification
{
    LlmOptions _result;

    void Because() => _result = LlmOptionsResolver.Resolve(null, new LlmConfiguration { Kind = "gemini" });

    [Fact] void should_not_be_enabled() => _result.Enabled.ShouldBeFalse();
}
