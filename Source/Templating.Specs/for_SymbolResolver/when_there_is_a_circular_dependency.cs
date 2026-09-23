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

public class when_there_is_a_circular_dependency : a_resolver
{
    Exception? _error;

    void Because() => _error = Catch.Exception(() => Resolve(new Dictionary<string, SymbolConfig>
    {
        ["A"] = new() { Name = "A", Type = SymbolType.Computed, Value = "B == \"x\"" },
        ["B"] = new() { Name = "B", Type = SymbolType.Computed, Value = "A == \"x\"" }
    }));

    [Fact] void should_report_the_cycle() => _error.ShouldNotBeNull();
}
