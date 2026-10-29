// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using Cratis.Templating.Configuration;
using Cratis.Templating.Specs.for_SymbolResolver.given;
using Cratis.Templating.Symbols;

namespace Cratis.Templating.Specs.for_SymbolResolver.when_resolving_an_implicit_name;

public class with_a_conflicting_parameter_value : a_resolver
{
    ResolvedSymbols? _result;

    void Because() => _result = Resolve(new Dictionary<string, SymbolConfig>
    {
        ["SafeName"] = new() { Name = "SafeName", Type = SymbolType.Derived, ValueSource = "name", ValueTransform = ["safe_name"] }
    },
    new Dictionary<string, string> { ["name"] = "Other.App" },
    name: "Actual.App",
    sourceName: "Template");

    [Fact] void should_return_the_implicit_name() => _result!.Values["name"].ShouldEqual("Actual.App");
    [Fact] void should_derive_from_the_implicit_name() => _result!.Values["SafeName"].ShouldEqual("Actual_App");
}
