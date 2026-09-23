// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating;
using Cratis.Templating.Configuration;
using Cratis.Templating.Packages;

namespace Cratis.Cli.for_ConceptTemplates.given;

public abstract class given_discovered_templates : Specification
{
    protected static DiscoveredTemplate Template(
        string shortName,
        string identity,
        string? groupIdentity,
        string language,
        double precedence = 0,
        string[]? databases = null) => new(
        new TemplateConfig
        {
            Name = $"{shortName} app",
            ShortName = shortName,
            Identity = identity,
            GroupIdentity = groupIdentity,
            Precedence = precedence,
            Tags = new Dictionary<string, string> { ["language"] = language },
            Symbols = new Dictionary<string, SymbolConfig>
            {
                ["Database"] = new()
                {
                    Name = "Database",
                    Type = SymbolType.Parameter,
                    DataType = "choice",
                    Choices = [.. (databases ?? ["MongoDB"]).Select(database => new ChoiceConfig { Choice = database })]
                }
            }
        },
        $"/templates/{shortName}");

    protected static DiscoveredTemplate Standalone(string shortName, string language = "C#", string[]? databases = null) =>
        Template(shortName, $"Cratis.Templates.{shortName}", null, language, databases: databases);
}
