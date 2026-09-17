// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_AiCorpusSynchronizer.when_reading_configuration;

public class and_languages_are_omitted : Specification
{
    string _project = null!;
    string _corpus = null!;
    Exception _exception = null!;
    AiStatus? _status;

    void Establish()
    {
        _project = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _corpus = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var rules = Path.Combine(_corpus, ".cratis", "ai", "rules");
        Directory.CreateDirectory(rules);
        Directory.CreateDirectory(Path.Combine(_project, ".cratis"));
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "manifest.json"), "{\"harnesses\":[\"pi\"],\"profiles\":[\"cratis/engineering/csharp\",\"cratis/documentation\"],\"languages\":[\"csharp\",\"typescript\"]}");
        File.WriteAllText(Path.Combine(_corpus, ".cratis", "ai", "profile-catalog.json"), "{\"publicProfiles\":[{\"id\":\"cratis/documentation\"}],\"engineeringProfiles\":[{\"id\":\"cratis/engineering/csharp\",\"composes\":[\"cratis/documentation\"]}]}");
        File.WriteAllText(Path.Combine(rules, "general.md"), "# General");
        File.WriteAllText(Path.Combine(rules, "csharp.md"), "---\napplyTo: \"**/*.cs\"\n---\n# CSharp");

        // A configuration written before languages existed, or by hand, omits the optional property.
        File.WriteAllText(
            Path.Combine(_project, ".cratis", "ai.json"),
            "{\"schemaVersion\":\"1.0.0\",\"profiles\":[\"cratis/documentation\",\"cratis/engineering/csharp\"],\"harnesses\":[\"pi\"]}");
    }

    void Because()
    {
        try
        {
            _status = AiCorpusSynchronizer.Status(_project);
        }
        catch (Exception exception)
        {
            _exception = exception;
        }
    }

    [Fact] void should_not_fail() => _exception.ShouldBeNull();
    [Fact] void should_read_the_configured_profiles() => _status!.Configuration.Profiles.ShouldContain("cratis/engineering/csharp");
    [Fact] void should_treat_languages_as_unconstrained() => _status!.Configuration.Languages.ShouldBeEmpty();

    void Destroy()
    {
        Directory.Delete(_project, true);
        Directory.Delete(_corpus, true);
    }
}
