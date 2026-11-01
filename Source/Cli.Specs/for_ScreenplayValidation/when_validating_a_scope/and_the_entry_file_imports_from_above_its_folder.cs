// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Cli.for_ScreenplayValidation.when_validating_a_scope;

public class and_the_entry_file_imports_from_above_its_folder : given.a_folder_with_documents
{
    string _document;
    ValidatedScreenplay _unscoped;
    ValidatedScreenplay? _scoped;
    bool _selected;

    void Establish()
    {
        _document = WriteDocument("entry/root.play", "import \"../shared/*.play\"\nmodule M\n");
        WriteDocument("shared/other.play", "module Other\n");
        WriteDocument("unrelated.play", "module Unrelated\n");
        _unscoped = _validation.Validate(_document);
    }

    void Because() => _selected = _validation.TryValidateScoped(_document, "M", CompletenessChecks.None, out _scoped, out _);

    [Fact] void should_select_the_scope() => _selected.ShouldBeTrue();
    [Fact] void should_count_the_same_files_as_unscoped_validation() => _scoped.FileCount.ShouldEqual(_unscoped.FileCount);
    [Fact] void should_include_imports_but_not_unrelated_documents() => _scoped.FileCount.ShouldEqual(2);
}
