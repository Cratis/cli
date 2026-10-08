// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Cli.for_ScreenplayValidation.when_validating_a_scope;

public class and_the_folder_holds_several_documents : given.a_folder_with_documents
{
    ValidatedScreenplay _unscoped;
    ValidatedScreenplay? _scoped;
    bool _selected;

    void Establish()
    {
        WriteDocument("root.play", "import \"nested/*.play\"\nmodule M\n");
        WriteDocument("nested/feature.play", "module Other\n  feature F\n");
        _unscoped = _validation.Validate(_folder);
    }

    void Because() => _selected = _validation.TryValidateScoped(_folder, "M", CompletenessChecks.None, out _scoped, out _);

    [Fact] void should_select_the_scope() => _selected.ShouldBeTrue();
    [Fact] void should_count_the_same_files_as_unscoped_validation() => _scoped.FileCount.ShouldEqual(_unscoped.FileCount);
    [Fact] void should_count_each_document_once() => _scoped.FileCount.ShouldEqual(2);
}
