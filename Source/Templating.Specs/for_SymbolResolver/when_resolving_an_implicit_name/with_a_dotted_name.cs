// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Establish/Because are invoked by the Specification framework via reflection
#pragma warning disable RCS1213 // Establish/Because are invoked by the Specification framework via reflection

using System.Text.Json;
using Cratis.Templating.Configuration;
using Cratis.Templating.Specs.for_SymbolResolver.given;
using Cratis.Templating.Symbols;

namespace Cratis.Templating.Specs.for_SymbolResolver.when_resolving_an_implicit_name;

public class with_a_dotted_name : a_resolver
{
    ResolvedSymbols? _result;

    void Because() => _result = Resolve(new Dictionary<string, SymbolConfig>
    {
        ["Enabled"] = new() { Name = "Enabled", Type = SymbolType.Parameter, DefaultValue = "available", IsEnabled = "name != \"\"" },
        ["SafeName"] = new() { Name = "SafeName", Type = SymbolType.Derived, ValueSource = "name", ValueTransform = ["safe_name"] },
        ["HasName"] = new() { Name = "HasName", Type = SymbolType.Computed, DataType = "bool", Value = "name != \"\"" },
        ["Joined"] = new()
        {
            Name = "Joined",
            Type = SymbolType.Generated,
            Generator = "join",
            GeneratorParameters = new Dictionary<string, JsonElement>
            {
                ["symbols"] = JsonSerializer.SerializeToElement(new[] { "SafeName", "name" }),
                ["separator"] = JsonSerializer.SerializeToElement("+")
            }
        }
    },
    name: "S1.S2.S3",
    sourceName: "Template");

    [Fact] void should_resolve_the_enabled_parameter() => _result!.Values["Enabled"].ShouldEqual("available");
    [Fact] void should_not_disable_the_parameter() => _result!.DisabledSymbols.Contains("Enabled").ShouldBeFalse();
    [Fact] void should_derive_the_safe_name() => _result!.Values["SafeName"].ShouldEqual("S1_S2_S3");
    [Fact] void should_compute_that_the_name_is_present() => _result!.Values["HasName"].ShouldEqual("true");
    [Fact] void should_generate_from_the_derived_and_implicit_names() => _result!.Values["Joined"].ShouldEqual("S1_S2_S3+S1.S2.S3");
    [Fact] void should_return_the_implicit_name() => _result!.Values["name"].ShouldEqual("S1.S2.S3");
}
