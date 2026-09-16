// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.Configuration;

namespace Cratis.Templating.Specs.for_LocalizationStore.when_resolving_the_culture_chain;

public class given_a_localize_folder : Specification
{
    protected string ManifestDirectory = null!;
    protected TemplateConfig Manifest = null!;

    void Establish()
    {
        ManifestDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-localize", Guid.NewGuid().ToString("N"))).FullName;
        Manifest = new TemplateConfig
        {
            Name = "Unlocalized",
            ShortName = "spec",
            Symbols = new Dictionary<string, SymbolConfig>
            {
                ["Framework"] = new() { Name = "Framework", Type = SymbolType.Parameter, DataType = "choice", Choices = [new() { Choice = "net10.0" }] }
            }
        };
    }

    protected void WriteStrings(string locale, string json)
    {
        Directory.CreateDirectory(Path.Combine(ManifestDirectory, "localize"));
        File.WriteAllText(Path.Combine(ManifestDirectory, "localize", $"templatestrings.{locale}.json"), json);
    }
}
