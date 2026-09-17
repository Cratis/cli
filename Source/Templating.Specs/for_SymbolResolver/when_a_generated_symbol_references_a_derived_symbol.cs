// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using System.Text.Json;
using Cratis.Templating.Configuration;
using Cratis.Templating.Specs.for_SymbolResolver.given;
using Cratis.Templating.Symbols;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Specs.for_SymbolResolver;

public class when_a_generated_symbol_references_a_derived_symbol : a_resolver
{
    ResolvedSymbols? _result;

    void Because() => _result = Resolve(new Dictionary<string, SymbolConfig>
    {
        ["Base"] = new() { Name = "Base", Type = SymbolType.Parameter, DataType = "string", DefaultValue = "My Company" },
        ["Derived"] = new() { Name = "Derived", Type = SymbolType.Derived, ValueSource = "Base", ValueTransform = ["safe_name"] },
        ["Generated"] = new()
        {
            Name = "Generated",
            Type = SymbolType.Generated,
            Generator = "join",
            GeneratorParameters = new Dictionary<string, JsonElement>
            {
                ["symbols"] = JsonSerializer.SerializeToElement(new[] { "Derived", "Base" }),
                ["separator"] = JsonSerializer.SerializeToElement("+")
            }
        }
    });

    [Fact] void should_resolve_the_derived_symbol_through_its_form() => _result!.Values["Derived"].ShouldEqual("My_Company");
    [Fact] void should_resolve_the_generated_symbol_from_the_derived_value() => _result!.Values["Generated"].ShouldEqual("My_Company+My Company");
}
