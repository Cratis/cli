// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Configuration;
using Cratis.Templating.Specs.for_SymbolResolver.given;

namespace Cratis.Templating.Specs.for_SymbolResolver.when_resolving_an_implicit_name;

public class with_a_declared_name : a_resolver
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => Resolve(new Dictionary<string, SymbolConfig>
    {
        ["name"] = new() { Name = "name", Type = SymbolType.Parameter, DefaultValue = "Declared" }
    },
    sourceName: "Template"));

    [Fact] void should_reject_the_manifest() => _error.ShouldBeOfExactType<InvalidTemplateManifest>();
    [Fact] void should_explain_the_name_collision() => _error!.Message.ShouldEqual("template.json: symbol 'name' cannot be defined when 'sourceName' is set — sourceName provides the built-in 'name' symbol.");
}
