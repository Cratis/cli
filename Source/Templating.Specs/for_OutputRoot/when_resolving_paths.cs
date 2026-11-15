// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.FileSystem;

namespace Cratis.Templating.Specs.for_OutputRoot;

public class when_resolving_paths : Specification
{
    string _parent;
    string _root;

    void Establish()
    {
        _parent = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-output-root", Guid.NewGuid().ToString("N"))).FullName;
        _root = Directory.CreateDirectory(Path.Combine(_parent, "app")).FullName;
        Directory.CreateDirectory(Path.Combine(_parent, "app-other"));
    }

    [Fact] void should_accept_a_relative_path_inside() => OutputRoot.TryResolve(_root, Path.Combine("src", "appsettings.json"), out _).ShouldBeTrue();
    [Fact] void should_accept_the_root_itself() => OutputRoot.TryResolve(_root, ".", out _).ShouldBeTrue();
    [Fact] void should_refuse_an_absolute_path_even_inside() => OutputRoot.TryResolve(_root, Path.Combine(_root, "a.json"), out _).ShouldBeFalse();
    [Fact] void should_contain_a_full_path_inside() => OutputRoot.Contains(_root, Path.Combine(_root, "a.json")).ShouldBeTrue();
    [Fact] void should_refuse_a_parent_traversal() => OutputRoot.TryResolve(_root, Path.Combine("..", "outside.json"), out _).ShouldBeFalse();
    [Fact] void should_refuse_a_traversal_hidden_in_the_middle() => OutputRoot.TryResolve(_root, Path.Combine("src", "..", "..", "outside.json"), out _).ShouldBeFalse();
    [Fact] void should_refuse_an_absolute_path_outside() => OutputRoot.TryResolve(_root, Path.Combine(_parent, "outside.json"), out _).ShouldBeFalse();
    [Fact] void should_refuse_a_sibling_sharing_the_root_prefix() => OutputRoot.TryResolve(_root, Path.Combine(_parent, "app-other", "a.json"), out _).ShouldBeFalse();
    [Fact] void should_throw_when_resolving_a_path_outside() => Catch.Exception(() => OutputRoot.Resolve(_root, Path.Combine("..", "x"))).ShouldBeOfExactType<InvalidTemplateManifest>();
}
