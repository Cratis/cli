// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Templating.FileSystem;

namespace Cratis.Templating.Specs.for_OutputRoot;

public class when_a_link_inside_points_outside : Specification
{
    string _root;
    string _outside;
    bool _linked;

    void Establish()
    {
        var parent = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "cratis-specs-output-root", Guid.NewGuid().ToString("N"))).FullName;
        _root = Directory.CreateDirectory(Path.Combine(parent, "app")).FullName;
        _outside = Directory.CreateDirectory(Path.Combine(parent, "elsewhere")).FullName;
        File.WriteAllText(Path.Combine(_outside, "settings.json"), "{}");
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(_root, "linked"), _outside);
            _linked = true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            _linked = false;
        }
    }

    [Fact]
    void should_refuse_a_path_through_the_link()
    {
        if (!_linked) return;
        OutputRoot.TryResolve(_root, Path.Combine("linked", "settings.json"), out _).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_the_link_itself()
    {
        if (!_linked) return;
        OutputRoot.Contains(_root, Path.Combine(_root, "linked")).ShouldBeFalse();
    }
}
