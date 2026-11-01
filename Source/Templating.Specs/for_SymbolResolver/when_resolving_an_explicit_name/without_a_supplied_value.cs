// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Configuration;
using Cratis.Templating.Specs.for_SymbolResolver.given;
using Cratis.Templating.Symbols;

namespace Cratis.Templating.Specs.for_SymbolResolver.when_resolving_an_explicit_name;

public class without_a_supplied_value : a_resolver
{
    ResolvedSymbols? _result;

    void Because() => _result = Resolve(new Dictionary<string, SymbolConfig>
    {
        ["name"] = new() { Name = "name", Type = SymbolType.Parameter, DefaultValue = "Default.App" },
        ["SafeName"] = new() { Name = "SafeName", Type = SymbolType.Derived, ValueSource = "name", ValueTransform = ["safe_name"] }
    },
    name: "Host.App");

    [Fact] void should_use_the_default_name() => _result.Values["name"].ShouldEqual("Default.App");
    [Fact] void should_derive_from_the_default_name() => _result.Values["SafeName"].ShouldEqual("Default_App");
}
