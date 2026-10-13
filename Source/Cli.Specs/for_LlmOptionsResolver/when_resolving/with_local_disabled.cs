// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmOptionsResolver.when_resolving;

public class with_local_disabled : Specification
{
    (LlmOptions Options, string Source) _result;

    void Because() => _result = LlmOptionsResolver.ResolveWithSource("""{"Llm":{"Enabled":false}}""", new() { Kind = "openai", Model = "global-model" });

    [Fact] void should_disable_refinement() => _result.Options.Enabled.ShouldBeFalse();
    [Fact] void should_name_the_local_source() => _result.Source.ShouldEqual("local file");
}
