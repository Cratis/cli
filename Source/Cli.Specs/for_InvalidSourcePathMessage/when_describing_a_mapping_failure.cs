// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_InvalidSourcePathMessage;

/// <summary>
/// CLI0017 used to say only that the source paths could not be mapped, leaving the reader to find which one and why.
/// It now names the reason and the path - portably, because diagnostics never carry a machine's physical roots.
/// </summary>
public class when_describing_a_mapping_failure : Specification
{
    string _root;
    string _target;
    string _beside;
    string _rootOnly;
    string _logical;
    string _withoutPath;
    string _closure;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), $"invalid-source-path-{Guid.NewGuid():N}");
        _target = Path.Combine(_root, "Repository", "Host", "Application.csproj");
    }

    void Because()
    {
        _beside = Describe(Path.Combine(_root, "Dependency", "Source.cs"));
        _rootOnly = Describe(Path.Combine(Path.GetPathRoot(_root)!, "Elsewhere.cs"));
        _logical = InvalidSourcePathMessage.For("Application", new InvalidScreenplayProjectSource("A logical document path is rooted, traversing, or malformed", ".."), _target);
        _withoutPath = InvalidSourcePathMessage.For("Application", new InvalidScreenplayProjectSource("An authored syntax tree was not mapped by the source context"), _target);
        _closure = InvalidSourcePathMessage.ForClosure(new InvalidScreenplayProjectSource("The direct project-reference closure has no common workspace boundary"), _target);
    }

    [Fact] void should_name_the_project() => _beside.ShouldContain("'Application'");
    [Fact] void should_state_the_reason() => _beside.ShouldContain("outside the declared display root");
    [Fact] void should_name_the_unmapped_path_relative_to_the_target() => _beside.ShouldContain("('../../Dependency/Source.cs')");
    [Fact] void should_not_leak_the_physical_root() => _beside.ShouldNotContain(_root);
    [Fact] void should_name_a_path_sharing_only_the_filesystem_root_by_its_file_name() => _rootOnly.ShouldContain("('Elsewhere.cs')");
    [Fact] void should_name_a_logical_path_as_it_is() => _logical.ShouldContain("('..')");
    [Fact] void should_state_the_reason_when_no_single_path_is_responsible() => _withoutPath.EndsWith("An authored syntax tree was not mapped by the source context", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_state_the_reason_for_a_closure() => _closure.ShouldContain("has no common workspace boundary");

    string Describe(string path) =>
        InvalidSourcePathMessage.For("Application", new InvalidScreenplayProjectSource("A workspace source path is outside the declared display root", path), _target);
}
