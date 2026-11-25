// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Affected.for_DeclarationFinder;

public class when_finding_declarations : Specification
{
    IReadOnlyList<(string Class, string File)> _found;

    void Because() => _found = DeclarationFinder.Find(
        "a.cs",
        "namespace One.Two { public class Outer { public class Inner { } } } namespace Three { public partial class Split { } }");

    [Fact] void should_name_a_class_by_namespace() => _found.Select(item => item.Class).ShouldContain("One.Two.Outer");
    [Fact] void should_separate_nested_types_with_a_plus() => _found.Select(item => item.Class).ShouldContain("One.Two.Outer+Inner");
    [Fact] void should_find_partial_classes() => _found.Select(item => item.Class).ShouldContain("Three.Split");
    [Fact] void should_record_the_declaring_file() => _found.ShouldEachConformTo(item => item.File == "a.cs");
}
