// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable IDE0051 // Fact methods are invoked by the test runner via reflection
#pragma warning disable RCS1213 // Fact methods are invoked by the test runner via reflection

using Cratis.Templating.Conformance.given;

namespace Cratis.Templating.Conformance;

/// <summary>
/// Parses every manifest in the vendored upstream corpus: each one must either parse cleanly or
/// fail with one of the engine's named error types — never with any other exception, and never
/// silently. This is the phase-one conformance gate: zero silent drops across the corpus.
/// </summary>
public class when_parsing_the_upstream_corpus : a_conformance_spec
{
    static string CorpusRoot
    {
        get
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current is not null && !Directory.Exists(Path.Combine(current.FullName, "UpstreamCorpus")))
            {
                current = current.Parent;
            }
            return current is null
                ? throw new InvalidOperationException("Upstream corpus not found next to the test assembly.")
                : Path.Combine(current.FullName, "UpstreamCorpus");
        }
    }

    static List<string> FindManifests() =>
        [.. Directory.EnumerateFiles(CorpusRoot, "template.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal)];

    [Fact]
    void should_locate_the_corpus_with_a_non_empty_manifest_set()
    {
        var manifests = FindManifests();
        manifests.Count.ShouldBeGreaterThan(50);
    }

    [Fact]
    void should_parse_every_manifest_or_fail_with_a_named_error()
    {
        var parsed = 0;
        var namedErrors = 0;
        var unexpected = new List<string>();

        foreach (var manifestPath in FindManifests())
        {
            try
            {
                var manifest = TemplateConfigParser.ParseFile(manifestPath);
                manifest.Name.Length.ShouldNotEqual(0);
                parsed++;
            }
            catch (Exception error) when (error is InvalidTemplateManifest or UnsupportedTemplateConstruct)
            {
                namedErrors++;
            }
            catch (Exception error)
            {
                unexpected.Add($"{Path.GetRelativePath(CorpusRoot, manifestPath)}: {error.GetType().Name}: {error.Message.Split('\n')[0]}");
            }
        }

        (parsed + namedErrors).ShouldEqual(FindManifests().Count);
        unexpected.ShouldBeEmpty();
    }

    [Fact]
    void should_fail_the_intentionally_invalid_group_with_named_errors()
    {
        var invalidRoot = Path.Combine(CorpusRoot, "Invalid");
        if (!Directory.Exists(invalidRoot))
        {
            return;
        }

        var named = 0;
        var manifests = 0;
        foreach (var manifestPath in Directory.EnumerateFiles(invalidRoot, "template.json", SearchOption.AllDirectories))
        {
            manifests++;
            try
            {
                TemplateConfigParser.ParseFile(manifestPath);
            }
            catch (Exception error) when (error is InvalidTemplateManifest or UnsupportedTemplateConstruct)
            {
                named++;
            }
        }
        manifests.ShouldBeGreaterThan(0);
        named.ShouldBeGreaterThan(0);
    }
}
