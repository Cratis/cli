// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;
using Cratis.Templating.Configuration;

namespace Cratis.Cli.for_DatabaseSelection.given;

public abstract class a_manifest : Specification
{
    protected static TemplateConfig WithDatabaseChoices(params string[] choices) => new()
    {
        Name = "Selector",
        ShortName = "selector",
        Symbols = new Dictionary<string, SymbolConfig>
        {
            ["Database"] = new()
            {
                Name = "Database",
                Type = SymbolType.Parameter,
                DataType = "choice",
                DefaultValue = choices.FirstOrDefault(),
                Choices = [.. choices.Select(choice => new ChoiceConfig { Choice = choice })]
            }
        }
    };

    protected static TemplateConfig WithoutDatabaseParameter() => new()
    {
        Name = "Plain",
        ShortName = "plain"
    };

    protected static TemplateConfig WithFreeFormDatabase() => new()
    {
        Name = "FreeForm",
        ShortName = "freeform",
        Symbols = new Dictionary<string, SymbolConfig>
        {
            ["Database"] = new()
            {
                Name = "Database",
                Type = SymbolType.Parameter,
                DataType = "string"
            }
        }
    };
}
