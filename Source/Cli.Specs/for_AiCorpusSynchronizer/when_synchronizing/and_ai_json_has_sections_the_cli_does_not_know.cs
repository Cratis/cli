// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_synchronizing;

/// <summary>
/// Corpus extensions own top-level sections of .cratis/ai.json that the CLI knows nothing about, such as the
/// update policy of Cratis/AI. Installing and updating must hand them back as they were found.
/// </summary>
public class and_ai_json_has_sections_the_cli_does_not_know : Specification
{
    const string ExtensionSections =
        "{\"updatePolicy\":{\"channel\":\"stable\",\"intervals\":[1,7],\"nested\":{\"enabled\":true,\"note\":null}}," +
        "\"systemOne\":[{\"name\":\"first\",\"weight\":1.5},\"second\",3]," +
        "\"revision\":\"pinned\",\"retries\":3,\"enabled\":true}";

    string _project = null!;
    string _corpus = null!;
    JsonObject _afterInstall = null!;
    JsonObject _afterUpdate = null!;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_corpus, "distribution"));
        Directory.CreateDirectory(Path.Combine(_corpus, ".cratis", "ai", "rules"));
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/example\"],\"languages\":[\"csharp\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/example\",\"availableTargets\":[\"example\"]}],\"engineeringProfiles\":[]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "rules", "general.md"), "# Rule");

        var configuration = JsonNode.Parse(ExtensionSections)!.AsObject();
        configuration["schemaVersion"] = 1;
        configuration["harnesses"] = new JsonArray("pi");
        configuration["profiles"] = new JsonArray("cratis/example");
        Directory.CreateDirectory(Path.Combine(_project, ".cratis"));
        File.WriteAllText(Path.Combine(_project, ".cratis", "ai.json"), configuration.ToJsonString());
    }

    void Because()
    {
        var selection = new AiConfiguration(["pi"], ["cratis/example"], ["csharp"]);
        AiCorpusSynchronizer.Synchronize(_project, _corpus, selection);
        _afterInstall = ReadConfiguration();

        // An update reuses the recorded selection, exactly as 'cratis ai update' does.
        AiCorpusSynchronizer.Synchronize(_project, _corpus, AiCorpusSynchronizer.Status(_project).Configuration);
        _afterUpdate = ReadConfiguration();
    }

    [Fact] void should_keep_the_object_section_on_install() => Section(_afterInstall, "updatePolicy").ShouldBeTrue();
    [Fact] void should_keep_the_array_section_on_install() => Section(_afterInstall, "systemOne").ShouldBeTrue();
    [Fact] void should_keep_the_scalar_sections_on_install() => Scalars(_afterInstall).ShouldBeTrue();
    [Fact] void should_keep_the_object_section_on_update() => Section(_afterUpdate, "updatePolicy").ShouldBeTrue();
    [Fact] void should_keep_the_array_section_on_update() => Section(_afterUpdate, "systemOne").ShouldBeTrue();
    [Fact] void should_keep_the_scalar_sections_on_update() => Scalars(_afterUpdate).ShouldBeTrue();

    static bool Section(JsonObject written, string name) => JsonNode.DeepEquals(written[name], JsonNode.Parse(ExtensionSections)![name]);

    static bool Scalars(JsonObject written) => new[] { "revision", "retries", "enabled" }.All(name => Section(written, name));

    JsonObject ReadConfiguration() => JsonNode.Parse(File.ReadAllText(Path.Combine(_project, ".cratis", "ai.json")))!.AsObject();

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
