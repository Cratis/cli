// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Cratis.Cli.for_GeneratedResourceSources.when_adding_missing_sources;

public class and_the_project_already_compiles_them : given.a_loaded_project
{
    Project _result;

    void Establish()
    {
        var solution = _project.Solution;
        foreach (var file in new[] { _adminMessages, _commonMessages })
        {
            solution = solution.AddDocument(
                DocumentId.CreateNewId(_project.Id),
                Path.GetFileName(file),
                SourceText.From(File.ReadAllText(file)),
                filePath: file);
        }

        _project = solution.GetProject(_project.Id);
    }

    void Because() => _result = GeneratedResourceSources.AddMissingTo(_project);

    [Fact] void should_not_add_them_a_second_time() => _result.Documents.Count().ShouldEqual(2);
}
