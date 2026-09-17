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

public class when_a_computed_symbol_evaluates_an_expression : a_resolver
{
    ResolvedSymbols? _result;

    void Because() => _result = Resolve(new Dictionary<string, SymbolConfig>
    {
        ["Framework"] = new()
        {
            Name = "Framework",
            Type = SymbolType.Parameter,
            DataType = "choice",
            DefaultValue = "net10.0",
            Choices = [new() { Choice = "net10.0" }, new() { Choice = "net8.0" }]
        },
        ["IsModern"] = new() { Name = "IsModern", Type = SymbolType.Computed, DataType = "bool", Value = "Framework == \"net10.0\"" }
    });

    [Fact] void should_evaluate_the_expression() => _result!.Values["IsModern"].ShouldEqual("true");
}
