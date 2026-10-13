// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Prologue.Configuration;

namespace Cratis.Cli.for_LlmOptionsResolver.when_resolving;

public class with_local_enabled : Specification
{
    (LlmOptions Options, string Source) _result;

    void Because() => _result = LlmOptionsResolver.ResolveWithSource("""{"llm":{"enabled":true,"kind":"Ollama","modelId":"project-model","endpoint":"http://localhost:11434"}}""", new() { Kind = "openai", Model = "global-model" });

    [Fact] void should_enable_refinement() => _result.Options.Enabled.ShouldBeTrue();
    [Fact] void should_use_the_local_provider() => _result.Options.Kind.ShouldEqual(LlmKind.Ollama);
    [Fact] void should_use_the_local_model() => _result.Options.ModelId.ShouldEqual("project-model");
    [Fact] void should_name_the_local_source() => _result.Source.ShouldEqual("local file");
}
