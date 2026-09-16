// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Templating.Configuration;
using Cratis.Templating.Symbols;
using Cratis.Templating.ValueForms;

namespace Cratis.Templating.Specs.for_SymbolResolver.given;

public abstract class a_resolver : Specification
{
    protected static ResolvedSymbols Resolve(
        IReadOnlyDictionary<string, SymbolConfig> symbols,
        IReadOnlyDictionary<string, string>? parameters = null,
        string name = "MyApp") =>
        new SymbolResolver(ValueFormRegistry.Empty).Resolve(
            new TemplateConfig
            {
                Name = "Test",
                ShortName = "test",
                Symbols = symbols
            },
            parameters ?? new Dictionary<string, string>(),
            name,
            new Dictionary<string, string>());
}
