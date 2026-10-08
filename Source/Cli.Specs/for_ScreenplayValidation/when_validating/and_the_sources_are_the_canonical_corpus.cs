// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Cli.for_ScreenplayValidation.when_validating;

public class and_the_sources_are_the_canonical_corpus : given.a_folder_with_documents
{
    readonly List<(string Folder, int Files)> _forms = [];
    readonly List<string> _errors = [];

    void Establish()
    {
        var corpus = typeof(RegisterProjectCorpus).Assembly;
        var vectors = corpus.ExportedTypes.SelectMany(type => type.GetProperties())
            .Where(property => property.PropertyType == typeof(CanonicalCorpusVector))
            .Select(property => (CanonicalCorpusVector)property.GetValue(null)!);
        foreach (var vector in vectors)
        {
            foreach (var form in vector.SourceForms)
            {
                var folder = Path.Combine(vector.Name, form.Name);
                foreach (var document in form.Documents)
                {
                    WriteDocument(Path.Combine(folder, document.DisplayPath), document.Text);
                }

                _forms.Add((Path.Combine(_folder, folder), form.Documents.Length));
            }
        }

        foreach (var resource in corpus.GetManifestResourceNames().Where(name => name.Contains(".InlineEvents.", StringComparison.Ordinal) && name.EndsWith(".play", StringComparison.Ordinal)))
        {
            using var stream = corpus.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            var folder = Path.Combine("inline", resource);
            WriteDocument(Path.Combine(folder, "application.play"), reader.ReadToEnd());
            _forms.Add((Path.Combine(_folder, folder), 1));
        }

        var rejected = RegisterProjectCorpus.UnsupportedSequence.SourceForm;
        foreach (var document in rejected.Documents)
        {
            WriteDocument(Path.Combine("unsupported-sequence", document.DisplayPath), document.Text);
        }

        _forms.Add((Path.Combine(_folder, "unsupported-sequence"), rejected.Documents.Length));
    }

    void Because()
    {
        foreach (var (folder, files) in _forms)
        {
            var result = _validation.Validate(folder);
            result.FileCount.ShouldEqual(files);
            _errors.AddRange(result.Diagnostics.Where(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Error)
                .Select(diagnostic => $"{folder}: {diagnostic.Code} {diagnostic.Location}: {diagnostic.Message}"));
        }
    }

    [Fact] void should_validate_every_corpus_source_form_without_errors() => _errors.ShouldBeEmpty();
    [Fact] void should_include_the_policy_negation_vector() => _forms.Exists(form => form.Folder.Replace('\\', '/').Contains("policy-negation/v7/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_include_the_register_project_v7_vector() => _forms.Exists(form => form.Folder.Replace('\\', '/').Contains("register-project/v7/", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_include_the_reactions_v6_vector() => _forms.Exists(form => form.Folder.Replace('\\', '/').Contains("reactions/v6/single", StringComparison.Ordinal)).ShouldBeTrue();
}
