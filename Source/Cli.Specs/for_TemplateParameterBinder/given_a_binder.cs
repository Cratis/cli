// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Cli.Templates;
using Cratis.Templating;
using Cratis.Templating.Configuration;

namespace Cratis.Cli.for_TemplateParameterBinder;

public abstract class given_a_binder : Specification
{
    protected static TemplateConfig ManifestWith(params SymbolConfig[] symbols) => new()
    {
        Name = "Test",
        ShortName = "test",
        Symbols = symbols.ToDictionary(symbol => symbol.Name)
    };

    protected static SymbolConfig Choice(string name, bool multiple = false, string? defaultValue = null) => new()
    {
        Name = name,
        Type = SymbolType.Parameter,
        DataType = "choice",
        AllowMultipleValues = multiple,
        DefaultValue = defaultValue,
        Choices = [new() { Choice = "net10.0" }, new() { Choice = "net8.0" }, new() { Choice = "net6.0" }]
    };

    protected static SymbolConfig Bool(string name, string? defaultValue = null) => new()
    {
        Name = name,
        Type = SymbolType.Parameter,
        DataType = "bool",
        DefaultValue = defaultValue
    };
}
