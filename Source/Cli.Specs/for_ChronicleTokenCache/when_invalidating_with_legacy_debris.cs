// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Cli.for_ChronicleTokenCache;

[Collection(CliSpecsCollection.Name)]
public class when_invalidating_with_legacy_debris : given.a_token_cache
{
    string _selected;
    string _sibling;
    string _legacy;

    void Establish()
    {
        _selected = CachePath(Connection());
        _sibling = CachePath(Connection(disableTls: true));
        _legacy = CliConfiguration.GetTokenCachePath($"{Context}_client");
        Seed(_selected);
        Seed(_sibling);
        Seed(_legacy);
    }

    void Because() => ChronicleTokenCache.Clear(Connection(), Context);

    [Fact] void should_delete_the_selected_destination() => File.Exists(_selected).ShouldBeFalse();
    [Fact] void should_keep_the_other_destination() => File.Exists(_sibling).ShouldBeTrue();
    [Fact] void should_not_touch_legacy_debris_during_invalidation() => File.Exists(_legacy).ShouldBeTrue();
}
