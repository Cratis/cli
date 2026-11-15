// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.Extensions.for_ExtensionAllowList;

public class when_checking_publishers : Specification
{
    string _root;
    ExtensionAllowList _loaded;
    ExtensionAllowList _empty;
    Exception _invalid;

    void Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), "cratis-allow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _empty = ExtensionAllowList.Load(_root);
    }

    async Task Because()
    {
        new ExtensionAllowList([]).Add(_root, new("https://github.com/cratis", "sha256:abc", ["ghcr.io/cratis/*"]));
        _loaded = ExtensionAllowList.Load(_root);
        Directory.CreateDirectory(Path.Combine(_root, "broken", ".cratis"));
        await File.WriteAllTextAsync(ExtensionAllowList.PathFor(Path.Combine(_root, "broken")), "{ not json");
        _invalid = await Catch.Exception(() => Task.FromResult(ExtensionAllowList.Load(Path.Combine(_root, "broken"))));
    }

    void Destroy() => Directory.Delete(_root, true);

    [Fact] void should_allow_nobody_when_the_project_has_no_list() => _empty.Publishers.ShouldBeEmpty();
    [Fact] void should_keep_the_publisher_and_its_key() => _loaded.Find("https://github.com/cratis")!.Key.ShouldEqual("sha256:abc");
    [Fact] void should_cover_a_reference_the_pattern_matches() => _loaded.Find("https://github.com/cratis")!.Covers("ghcr.io/cratis/extension-stripe").ShouldBeTrue();
    [Fact] void should_not_cover_another_reference() => _loaded.Find("https://github.com/cratis")!.Covers("ghcr.io/evil/extension-stripe").ShouldBeFalse();
    [Fact] void should_cover_any_reference_when_none_are_listed() => new AllowedPublisher("p").Covers("ghcr.io/any/thing").ShouldBeTrue();
    [Fact] void should_not_know_a_publisher_it_was_not_told_about() => _loaded.Find("https://github.com/other").ShouldBeNull();
    [Fact] void should_refuse_a_file_that_is_not_json() => _invalid.ShouldBeOfExactType<InvalidDataException>();
}
