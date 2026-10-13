// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmOptionsResolver.when_resolving;

public class without_a_local_section : Specification
{
    (LlmOptions Options, string Source) _result;

    void Because() => _result = LlmOptionsResolver.ResolveWithSource("{}", new() { Kind = "anthropic", Model = "global-model" });

    [Fact] void should_use_the_global_provider() => _result.Options.Kind.ShouldEqual(LlmKind.Anthropic);
    [Fact] void should_enable_refinement() => _result.Options.Enabled.ShouldBeTrue();
    [Fact] void should_name_the_global_source() => _result.Source.ShouldEqual("global config");
}
