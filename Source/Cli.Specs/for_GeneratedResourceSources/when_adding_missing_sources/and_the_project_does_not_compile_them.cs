// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace Cratis.Cli.for_GeneratedResourceSources.when_adding_missing_sources;

public class and_the_project_does_not_compile_them : given.a_loaded_project
{
    Project _result;

    void Because() => _result = GeneratedResourceSources.AddMissingTo(_project);

    [Fact] void should_add_every_generated_resource_source() => _result.Documents.Count().ShouldEqual(2);
    [Fact] void should_add_the_admin_messages() => _result.Documents.Select(document => document.FilePath).ShouldContain(_adminMessages);
    [Fact] void should_add_the_common_messages() => _result.Documents.Select(document => document.FilePath).ShouldContain(_commonMessages);
    [Fact] async Task should_read_the_source_back_from_disk() => (await _result.Documents.First().GetTextAsync()).ToString().ShouldContain("public static class");
}
