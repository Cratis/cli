// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Cli.for_ScreenplayPlanning.when_mapping_source_lines;

public class with_a_shared_destination : given.an_inline_model
{
    void Because() => _result = _planning.PlanDocuments(DocumentRequest(_root, _root), CancellationToken.None);

    [Fact] void should_plan_successfully() => _result.Success.ShouldBeTrue();

    [Fact]
    void should_resolve_each_directive_relative_to_its_generated_file()
    {
        var locations = _result.Artifacts.Artifacts.SelectMany(artifact =>
            CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).GetRoot().DescendantTrivia()
                .Select(trivia => trivia.GetStructure()).OfType<LineDirectiveTriviaSyntax>()
                .Where(directive => directive.File.ValueText.Length > 0)
                .Select(directive => Path.GetFullPath(directive.File.ValueText, Path.GetDirectoryName(Path.Combine(_root, artifact.RelativePath))!))).ToArray();
        locations.ShouldNotBeEmpty();
        locations.ShouldEachConformTo(location => location == _file);
    }
}
